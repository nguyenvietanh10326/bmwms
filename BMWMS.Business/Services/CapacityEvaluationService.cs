using System.Globalization;
using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Business.Services;

public sealed class CapacityEvaluationService : ICapacityEvaluationService
{
    public const string StorageWeightAttributeCode = "STORAGE_WEIGHT_KG_PER_BASE_UOM";
    public const string StorageVolumeAttributeCode = "STORAGE_VOLUME_M3_PER_BASE_UOM";

    private readonly BmwmsContext _context;

    public CapacityEvaluationService(BmwmsContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<long, LocationCapacityEvaluationDto>> EvaluateAsync(
        IReadOnlyCollection<CapacityAllocationDto> allocations,
        bool acquireLocationLocks = false)
    {
        if (allocations == null)
            throw new ArgumentNullException(nameof(allocations));
        if (allocations.Any(item => item.StorageLocationId <= 0 || item.ProductId <= 0))
            throw new ArgumentException("Dữ liệu dùng để tính sức chứa không hợp lệ.", nameof(allocations));

        var locationIds = allocations.Select(item => item.StorageLocationId).Distinct().OrderBy(id => id).ToList();
        if (locationIds.Count == 0)
            return new Dictionary<long, LocationCapacityEvaluationDto>();

        List<StorageLocation> locations;
        if (acquireLocationLocks)
        {
            locations = new List<StorageLocation>(locationIds.Count);
            foreach (var locationId in locationIds)
            {
                var location = await _context.StorageLocations
                    .FromSqlInterpolated($"SELECT * FROM dbo.StorageLocations WITH (UPDLOCK, HOLDLOCK) WHERE StorageLocationID = {locationId}")
                    .SingleOrDefaultAsync()
                    ?? throw new ArgumentException($"Không tìm thấy vị trí kho ID {locationId}.");
                locations.Add(location);
            }
        }
        else
        {
            locations = await _context.StorageLocations
                .AsNoTracking()
                .Where(location => locationIds.Contains(location.StorageLocationId))
                .ToListAsync();
            if (locations.Count != locationIds.Count)
                throw new ArgumentException("Một hoặc nhiều vị trí dùng để tính sức chứa không còn tồn tại.");
        }

        var stockRows = await _context.Inventories
            .AsNoTracking()
            .Where(inventory => locationIds.Contains(inventory.StorageLocationId) && inventory.OnHandQuantity > 0)
            .GroupBy(inventory => new { inventory.StorageLocationId, inventory.ProductId })
            .Select(group => new StockRow
            {
                StorageLocationId = group.Key.StorageLocationId,
                ProductId = group.Key.ProductId,
                Quantity = group.Sum(inventory => inventory.OnHandQuantity)
            })
            .ToListAsync();

        var productIds = stockRows.Select(row => row.ProductId)
            .Concat(allocations.Select(item => item.ProductId))
            .Distinct()
            .ToList();
        var productCodes = await _context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.ProductId))
            .ToDictionaryAsync(product => product.ProductId, product => product.ProductCode);
        var attributeRows = await _context.ProductAttributeValues
            .AsNoTracking()
            .Where(value => productIds.Contains(value.ProductId) &&
                            (value.ProductAttribute.AttributeCode == StorageWeightAttributeCode ||
                             value.ProductAttribute.AttributeCode == StorageVolumeAttributeCode))
            .Select(value => new
            {
                value.ProductId,
                value.ProductAttribute.AttributeCode,
                value.AttributeValue
            })
            .ToListAsync();

        var profiles = productIds.ToDictionary(productId => productId, _ => new ProductStorageProfile());
        foreach (var row in attributeRows)
        {
            if (!TryParsePositiveDecimal(row.AttributeValue, out var value))
                continue;
            if (row.AttributeCode == StorageWeightAttributeCode)
                profiles[row.ProductId].UnitWeightKg = value;
            else if (row.AttributeCode == StorageVolumeAttributeCode)
                profiles[row.ProductId].UnitVolumeM3 = value;
        }

        var currentByLocation = stockRows
            .GroupBy(row => row.StorageLocationId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var incomingByLocation = allocations
            .GroupBy(item => new { item.StorageLocationId, item.ProductId })
            .Select(group => new StockRow
            {
                StorageLocationId = group.Key.StorageLocationId,
                ProductId = group.Key.ProductId,
                Quantity = group.Sum(item => item.Quantity)
            })
            .GroupBy(row => row.StorageLocationId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var result = new Dictionary<long, LocationCapacityEvaluationDto>();
        foreach (var location in locations)
        {
            var currentRows = currentByLocation.GetValueOrDefault(location.StorageLocationId) ?? new List<StockRow>();
            var incomingRows = incomingByLocation.GetValueOrDefault(location.StorageLocationId) ?? new List<StockRow>();
            var evaluation = new LocationCapacityEvaluationDto
            {
                StorageLocationId = location.StorageLocationId,
                LocationCode = location.LocationCode,
                MaxWeightKg = location.MaxWeightKg,
                MaxVolumeM3 = location.MaxVolumeM3
            };

            EvaluateWeight(evaluation, currentRows, incomingRows, profiles, productCodes);
            EvaluateVolume(evaluation, currentRows, incomingRows, profiles, productCodes);
            evaluation.OverallStatus = ResolveOverallStatus(evaluation.WeightStatus, evaluation.VolumeStatus);
            result[location.StorageLocationId] = evaluation;
        }

        return result;
    }

    private static void EvaluateWeight(
        LocationCapacityEvaluationDto evaluation,
        IReadOnlyCollection<StockRow> currentRows,
        IReadOnlyCollection<StockRow> incomingRows,
        IReadOnlyDictionary<long, ProductStorageProfile> profiles,
        IReadOnlyDictionary<long, string> productCodes)
    {
        if (!evaluation.MaxWeightKg.HasValue)
        {
            evaluation.WeightStatus = CapacityEvaluationStatuses.NotConfigured;
            return;
        }

        evaluation.MissingWeightProductCodes = currentRows.Concat(incomingRows)
            .Where(row => !profiles.GetValueOrDefault(row.ProductId)?.UnitWeightKg.HasValue ?? true)
            .Select(row => productCodes.GetValueOrDefault(row.ProductId) ?? $"ID {row.ProductId}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToList();
        if (evaluation.MissingWeightProductCodes.Count > 0)
        {
            evaluation.WeightStatus = CapacityEvaluationStatuses.Unknown;
            return;
        }

        evaluation.CurrentWeightKg = currentRows.Sum(row => row.Quantity * profiles[row.ProductId].UnitWeightKg!.Value);
        evaluation.AddedWeightKg = incomingRows.Sum(row => row.Quantity * profiles[row.ProductId].UnitWeightKg!.Value);
        evaluation.ProjectedWeightKg = evaluation.CurrentWeightKg + evaluation.AddedWeightKg;
        evaluation.WeightStatus = evaluation.ProjectedWeightKg > evaluation.MaxWeightKg
            ? CapacityEvaluationStatuses.Exceeded
            : CapacityEvaluationStatuses.Available;
    }

    private static void EvaluateVolume(
        LocationCapacityEvaluationDto evaluation,
        IReadOnlyCollection<StockRow> currentRows,
        IReadOnlyCollection<StockRow> incomingRows,
        IReadOnlyDictionary<long, ProductStorageProfile> profiles,
        IReadOnlyDictionary<long, string> productCodes)
    {
        if (!evaluation.MaxVolumeM3.HasValue)
        {
            evaluation.VolumeStatus = CapacityEvaluationStatuses.NotConfigured;
            return;
        }

        evaluation.MissingVolumeProductCodes = currentRows.Concat(incomingRows)
            .Where(row => !profiles.GetValueOrDefault(row.ProductId)?.UnitVolumeM3.HasValue ?? true)
            .Select(row => productCodes.GetValueOrDefault(row.ProductId) ?? $"ID {row.ProductId}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToList();
        if (evaluation.MissingVolumeProductCodes.Count > 0)
        {
            evaluation.VolumeStatus = CapacityEvaluationStatuses.Unknown;
            return;
        }

        evaluation.CurrentVolumeM3 = currentRows.Sum(row => row.Quantity * profiles[row.ProductId].UnitVolumeM3!.Value);
        evaluation.AddedVolumeM3 = incomingRows.Sum(row => row.Quantity * profiles[row.ProductId].UnitVolumeM3!.Value);
        evaluation.ProjectedVolumeM3 = evaluation.CurrentVolumeM3 + evaluation.AddedVolumeM3;
        evaluation.VolumeStatus = evaluation.ProjectedVolumeM3 > evaluation.MaxVolumeM3
            ? CapacityEvaluationStatuses.Exceeded
            : CapacityEvaluationStatuses.Available;
    }

    private static string ResolveOverallStatus(string weightStatus, string volumeStatus)
    {
        if (weightStatus == CapacityEvaluationStatuses.Exceeded || volumeStatus == CapacityEvaluationStatuses.Exceeded)
            return CapacityEvaluationStatuses.Exceeded;
        if (weightStatus == CapacityEvaluationStatuses.Unknown || volumeStatus == CapacityEvaluationStatuses.Unknown)
            return CapacityEvaluationStatuses.Unknown;
        if (weightStatus == CapacityEvaluationStatuses.NotConfigured && volumeStatus == CapacityEvaluationStatuses.NotConfigured)
            return CapacityEvaluationStatuses.NotConfigured;
        return CapacityEvaluationStatuses.Available;
    }

    private static bool TryParsePositiveDecimal(string? rawValue, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        var normalized = rawValue.Trim().Replace(',', '.');
        return decimal.TryParse(
                   normalized,
                   NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                   CultureInfo.InvariantCulture,
                   out result) && result > 0;
    }

    private sealed class StockRow
    {
        public long StorageLocationId { get; set; }
        public long ProductId { get; set; }
        public decimal Quantity { get; set; }
    }

    private sealed class ProductStorageProfile
    {
        public decimal? UnitWeightKg { get; set; }
        public decimal? UnitVolumeM3 { get; set; }
    }
}
