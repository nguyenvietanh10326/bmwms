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
        return await EvaluateCoreAsync(locationIds, allocations, acquireLocationLocks);
    }

    public async Task<IReadOnlyDictionary<long, LocationCapacityEvaluationDto>> EvaluateCurrentAsync(
        IReadOnlyCollection<long> storageLocationIds)
    {
        if (storageLocationIds == null)
            throw new ArgumentNullException(nameof(storageLocationIds));
        if (storageLocationIds.Any(id => id <= 0))
            throw new ArgumentException("Danh sách vị trí dùng để tính sức chứa không hợp lệ.", nameof(storageLocationIds));

        var locationIds = storageLocationIds.Distinct().OrderBy(id => id).ToList();
        return await EvaluateCoreAsync(locationIds, Array.Empty<CapacityAllocationDto>(), false);
    }

    private async Task<IReadOnlyDictionary<long, LocationCapacityEvaluationDto>> EvaluateCoreAsync(
        IReadOnlyCollection<long> locationIds,
        IReadOnlyCollection<CapacityAllocationDto> allocations,
        bool acquireLocationLocks)
    {
        if (locationIds.Count == 0)
            return new Dictionary<long, LocationCapacityEvaluationDto>();

        var destinations = await _context.StorageLocations
            .AsNoTracking()
            .Where(location => locationIds.Contains(location.StorageLocationId))
            .Select(location => new LocationHierarchyRow
            {
                StorageLocationId = location.StorageLocationId,
                LocationCode = location.LocationCode,
                MaxWeightKg = location.MaxWeightKg,
                MaxVolumeM3 = location.MaxVolumeM3,
                RackId = location.RackId,
                RackCode = location.StorageRack != null ? location.StorageRack.RackCode : null,
                RackMaxWeightKg = location.StorageRack != null ? location.StorageRack.MaxWeightKg : null,
                RackMaxVolumeM3 = location.StorageRack != null ? location.StorageRack.MaxVolumeM3 : null,
                ZoneId = location.StorageRack != null ? location.StorageRack.ZoneId : null,
                ZoneCode = location.StorageRack != null ? location.StorageRack.WarehouseZone.ZoneCode : null,
                ZoneMaxWeightKg = location.StorageRack != null ? location.StorageRack.WarehouseZone.MaxWeightKg : null,
                ZoneMaxVolumeM3 = location.StorageRack != null ? location.StorageRack.WarehouseZone.MaxVolumeM3 : null
            })
            .ToListAsync();
        if (destinations.Count != locationIds.Count)
            throw new ArgumentException("Một hoặc nhiều vị trí dùng để tính sức chứa không còn tồn tại.");

        var affectedRackIds = destinations.Where(row => row.RackId.HasValue)
            .Select(row => row.RackId!.Value).Distinct().OrderBy(id => id).ToList();
        var affectedZoneIds = destinations.Where(row => row.ZoneId.HasValue)
            .Select(row => row.ZoneId!.Value).Distinct().OrderBy(id => id).ToList();

        if (acquireLocationLocks)
        {
            // Luôn khóa từ cấp cha xuống cấp con để hai thao tác vào các bin khác nhau
            // trong cùng rack/zone không cùng lúc vượt ngân sách sức chứa cấp cha.
            foreach (var zoneId in affectedZoneIds)
                _ = await _context.WarehouseZones
                    .FromSqlInterpolated($"SELECT * FROM dbo.WarehouseZones WITH (UPDLOCK, HOLDLOCK) WHERE ZoneID = {zoneId}")
                    .SingleAsync();
            foreach (var rackId in affectedRackIds)
                _ = await _context.StorageRacks
                    .FromSqlInterpolated($"SELECT * FROM dbo.StorageRacks WITH (UPDLOCK, HOLDLOCK) WHERE RackID = {rackId}")
                    .SingleAsync();
            foreach (var locationId in locationIds)
                _ = await _context.StorageLocations
                    .FromSqlInterpolated($"SELECT * FROM dbo.StorageLocations WITH (UPDLOCK, HOLDLOCK) WHERE StorageLocationID = {locationId}")
                    .SingleAsync();
        }

        var hierarchyLocations = await _context.StorageLocations
            .AsNoTracking()
            .Where(location => locationIds.Contains(location.StorageLocationId) ||
                (location.RackId.HasValue &&
                 (affectedRackIds.Contains(location.RackId.Value) ||
                  affectedZoneIds.Contains(location.StorageRack!.ZoneId))))
            .Select(location => new LocationMembershipRow
            {
                StorageLocationId = location.StorageLocationId,
                RackId = location.RackId,
                ZoneId = location.StorageRack != null ? location.StorageRack.ZoneId : null
            })
            .ToListAsync();
        var hierarchyLocationIds = hierarchyLocations.Select(row => row.StorageLocationId).Distinct().ToList();

        var stockRows = await _context.Inventories
            .AsNoTracking()
            .Where(inventory => hierarchyLocationIds.Contains(inventory.StorageLocationId) && inventory.OnHandQuantity > 0)
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
        foreach (var location in destinations)
        {
            var currentRows = currentByLocation.GetValueOrDefault(location.StorageLocationId) ?? new List<StockRow>();
            var incomingRows = incomingByLocation.GetValueOrDefault(location.StorageLocationId) ?? new List<StockRow>();
            var binScope = EvaluateScope("BIN", location.StorageLocationId, location.LocationCode,
                location.MaxWeightKg, location.MaxVolumeM3, currentRows, incomingRows, profiles, productCodes);
            var evaluation = new LocationCapacityEvaluationDto
            {
                StorageLocationId = location.StorageLocationId,
                LocationCode = location.LocationCode,
                MaxWeightKg = binScope.MaxWeightKg,
                CurrentWeightKg = binScope.CurrentWeightKg,
                AddedWeightKg = binScope.AddedWeightKg,
                ProjectedWeightKg = binScope.ProjectedWeightKg,
                WeightStatus = binScope.WeightStatus,
                MaxVolumeM3 = binScope.MaxVolumeM3,
                CurrentVolumeM3 = binScope.CurrentVolumeM3,
                AddedVolumeM3 = binScope.AddedVolumeM3,
                ProjectedVolumeM3 = binScope.ProjectedVolumeM3,
                VolumeStatus = binScope.VolumeStatus,
                Scopes = [binScope]
            };

            if (location.RackId.HasValue)
            {
                var rackLocationIds = hierarchyLocations
                    .Where(row => row.RackId == location.RackId)
                    .Select(row => row.StorageLocationId).ToHashSet();
                var rackScope = EvaluateScope("RACK", location.RackId.Value, location.RackCode ?? location.RackId.Value.ToString(),
                    location.RackMaxWeightKg, location.RackMaxVolumeM3,
                    stockRows.Where(row => rackLocationIds.Contains(row.StorageLocationId)).ToList(),
                    incomingByLocation.Where(pair => rackLocationIds.Contains(pair.Key)).SelectMany(pair => pair.Value).ToList(),
                    profiles, productCodes);
                evaluation.Scopes.Add(rackScope);
            }

            if (location.ZoneId.HasValue)
            {
                var zoneLocationIds = hierarchyLocations
                    .Where(row => row.ZoneId == location.ZoneId)
                    .Select(row => row.StorageLocationId).ToHashSet();
                var zoneScope = EvaluateScope("ZONE", location.ZoneId.Value, location.ZoneCode ?? location.ZoneId.Value.ToString(),
                    location.ZoneMaxWeightKg, location.ZoneMaxVolumeM3,
                    stockRows.Where(row => zoneLocationIds.Contains(row.StorageLocationId)).ToList(),
                    incomingByLocation.Where(pair => zoneLocationIds.Contains(pair.Key)).SelectMany(pair => pair.Value).ToList(),
                    profiles, productCodes);
                evaluation.Scopes.Add(zoneScope);
            }

            evaluation.OverallStatus = ResolveOverallStatus(evaluation.Scopes.Select(scope => scope.OverallStatus));
            evaluation.MissingWeightProductCodes = evaluation.Scopes
                .SelectMany(scope => scope.MissingWeightProductCodes).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            evaluation.MissingVolumeProductCodes = evaluation.Scopes
                .SelectMany(scope => scope.MissingVolumeProductCodes).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            result[location.StorageLocationId] = evaluation;
        }

        return result;
    }

    private static CapacityScopeEvaluationDto EvaluateScope(
        string scopeType,
        long scopeId,
        string scopeCode,
        decimal? maxWeightKg,
        decimal? maxVolumeM3,
        IReadOnlyCollection<StockRow> currentRows,
        IReadOnlyCollection<StockRow> incomingRows,
        IReadOnlyDictionary<long, ProductStorageProfile> profiles,
        IReadOnlyDictionary<long, string> productCodes)
    {
        var scope = new CapacityScopeEvaluationDto
        {
            ScopeType = scopeType,
            ScopeId = scopeId,
            ScopeCode = scopeCode,
            MaxWeightKg = maxWeightKg,
            MaxVolumeM3 = maxVolumeM3
        };

        if (maxWeightKg.HasValue)
        {
            scope.MissingWeightProductCodes = currentRows.Concat(incomingRows)
                .Where(row => !profiles.GetValueOrDefault(row.ProductId)?.UnitWeightKg.HasValue ?? true)
                .Select(row => productCodes.GetValueOrDefault(row.ProductId) ?? $"ID {row.ProductId}")
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(code => code).ToList();
            if (scope.MissingWeightProductCodes.Count > 0)
                scope.WeightStatus = CapacityEvaluationStatuses.Unknown;
            else
            {
                scope.CurrentWeightKg = currentRows.Sum(row => row.Quantity * profiles[row.ProductId].UnitWeightKg!.Value);
                scope.AddedWeightKg = incomingRows.Sum(row => row.Quantity * profiles[row.ProductId].UnitWeightKg!.Value);
                scope.ProjectedWeightKg = scope.CurrentWeightKg + scope.AddedWeightKg;
                scope.WeightStatus = scope.ProjectedWeightKg > maxWeightKg.Value
                    ? CapacityEvaluationStatuses.Exceeded
                    : CapacityEvaluationStatuses.Available;
            }
        }

        if (maxVolumeM3.HasValue)
        {
            scope.MissingVolumeProductCodes = currentRows.Concat(incomingRows)
                .Where(row => !profiles.GetValueOrDefault(row.ProductId)?.UnitVolumeM3.HasValue ?? true)
                .Select(row => productCodes.GetValueOrDefault(row.ProductId) ?? $"ID {row.ProductId}")
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(code => code).ToList();
            if (scope.MissingVolumeProductCodes.Count > 0)
                scope.VolumeStatus = CapacityEvaluationStatuses.Unknown;
            else
            {
                scope.CurrentVolumeM3 = currentRows.Sum(row => row.Quantity * profiles[row.ProductId].UnitVolumeM3!.Value);
                scope.AddedVolumeM3 = incomingRows.Sum(row => row.Quantity * profiles[row.ProductId].UnitVolumeM3!.Value);
                scope.ProjectedVolumeM3 = scope.CurrentVolumeM3 + scope.AddedVolumeM3;
                scope.VolumeStatus = scope.ProjectedVolumeM3 > maxVolumeM3.Value
                    ? CapacityEvaluationStatuses.Exceeded
                    : CapacityEvaluationStatuses.Available;
            }
        }

        scope.OverallStatus = ResolveOverallStatus(scope.WeightStatus, scope.VolumeStatus);
        return scope;
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

    private static string ResolveOverallStatus(IEnumerable<string> statuses)
    {
        var values = statuses.ToList();
        if (values.Any(status => status == CapacityEvaluationStatuses.Exceeded))
            return CapacityEvaluationStatuses.Exceeded;
        if (values.Any(status => status == CapacityEvaluationStatuses.Unknown))
            return CapacityEvaluationStatuses.Unknown;
        // Một mắt xích chưa cấu hình thì toàn bộ chuỗi Zone -> Rack -> Bin chưa hoàn chỉnh.
        // ADVISORY sẽ yêu cầu xác nhận; STRICT sẽ chặn thao tác.
        if (values.Any(status => status == CapacityEvaluationStatuses.NotConfigured))
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

    private sealed class LocationHierarchyRow
    {
        public long StorageLocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }
        public long? RackId { get; set; }
        public string? RackCode { get; set; }
        public decimal? RackMaxWeightKg { get; set; }
        public decimal? RackMaxVolumeM3 { get; set; }
        public long? ZoneId { get; set; }
        public string? ZoneCode { get; set; }
        public decimal? ZoneMaxWeightKg { get; set; }
        public decimal? ZoneMaxVolumeM3 { get; set; }
    }

    private sealed class LocationMembershipRow
    {
        public long StorageLocationId { get; set; }
        public long? RackId { get; set; }
        public long? ZoneId { get; set; }
    }
}
