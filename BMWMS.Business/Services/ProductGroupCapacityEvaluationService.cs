using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Business.Services;

/// <summary>
/// Evaluates physical stock in one Product Group base UOM across Bin → Rack → Zone.
/// Does not use the retired per-product kg/m³ conversion factors.
/// </summary>
public sealed class ProductGroupCapacityEvaluationService : ICapacityEvaluationService
{
    private readonly BmwmsContext _context;

    public ProductGroupCapacityEvaluationService(BmwmsContext context) => _context = context;

    public Task<IReadOnlyDictionary<long, LocationCapacityEvaluationDto>> EvaluateCurrentAsync(
        IReadOnlyCollection<long> storageLocationIds) =>
        EvaluateCoreAsync(storageLocationIds, Array.Empty<CapacityAllocationDto>(), false);

    public Task<IReadOnlyDictionary<long, LocationCapacityEvaluationDto>> EvaluateAsync(
        IReadOnlyCollection<CapacityAllocationDto> allocations,
        bool acquireLocationLocks = false) =>
        EvaluateCoreAsync(allocations.Select(item => item.StorageLocationId).Distinct().ToList(),
                          allocations, acquireLocationLocks);

    private async Task<IReadOnlyDictionary<long, LocationCapacityEvaluationDto>> EvaluateCoreAsync(
        IReadOnlyCollection<long> requestedIds,
        IReadOnlyCollection<CapacityAllocationDto> allocations,
        bool acquireLocks)
    {
        if (requestedIds.Any(id => id <= 0) ||
            allocations.Any(item => item.StorageLocationId <= 0 || item.ProductId <= 0))
            throw new ArgumentException("Vị trí hoặc sản phẩm dùng để kiểm tra sức chứa không hợp lệ.");
        if (requestedIds.Count == 0)
            return new Dictionary<long, LocationCapacityEvaluationDto>();

        var requested = await _context.StorageLocations.AsNoTracking()
            .Include(bin => bin.StorageRack)!
                .ThenInclude(rack => rack!.WarehouseZone)
            .Where(bin => requestedIds.Contains(bin.StorageLocationId))
            .ToListAsync();
        if (requested.Count != requestedIds.Distinct().Count())
            throw new ArgumentException("Một hoặc nhiều Bin không còn tồn tại.");

        var rackIds = requested.Where(bin => bin.RackId.HasValue)
            .Select(bin => bin.RackId!.Value).Distinct().OrderBy(id => id).ToList();
        var zoneIds = requested.Where(bin => bin.StorageRack != null)
            .Select(bin => bin.StorageRack!.ZoneId).Distinct().OrderBy(id => id).ToList();

        if (acquireLocks)
        {
            // Parent-to-child SQL locks protect the shared Rack/Zone budget.
            foreach (var id in zoneIds)
                _ = await _context.WarehouseZones
                    .FromSqlInterpolated($"SELECT * FROM dbo.WarehouseZones WITH (UPDLOCK, HOLDLOCK) WHERE ZoneID = {id}")
                    .SingleAsync();
            foreach (var id in rackIds)
                _ = await _context.StorageRacks
                    .FromSqlInterpolated($"SELECT * FROM dbo.StorageRacks WITH (UPDLOCK, HOLDLOCK) WHERE RackID = {id}")
                    .SingleAsync();
            foreach (var id in requestedIds.OrderBy(id => id))
                _ = await _context.StorageLocations
                    .FromSqlInterpolated($"SELECT * FROM dbo.StorageLocations WITH (UPDLOCK, HOLDLOCK) WHERE StorageLocationID = {id}")
                    .SingleAsync();
        }

        var hierarchyBins = await _context.StorageLocations.AsNoTracking()
            .Include(bin => bin.StorageRack)!
                .ThenInclude(rack => rack!.WarehouseZone)
            .Where(bin => bin.RackId.HasValue &&
                (rackIds.Contains(bin.RackId.Value) ||
                 zoneIds.Contains(bin.StorageRack!.ZoneId)))
            .ToListAsync();
        var allBins = hierarchyBins.Concat(requested)
            .DistinctBy(bin => bin.StorageLocationId).ToDictionary(bin => bin.StorageLocationId);
        var allBinIds = allBins.Keys.ToList();

        var stock = await _context.Inventories.AsNoTracking()
            .Include(row => row.Product)
            .Where(row => allBinIds.Contains(row.StorageLocationId) && row.OnHandQuantity > 0)
            .ToListAsync();
        var allocationProductIds = allocations.Select(item => item.ProductId).Distinct().ToList();
        var allocationProducts = await _context.Products.AsNoTracking()
            .Where(product => allocationProductIds.Contains(product.ProductId))
            .ToDictionaryAsync(product => product.ProductId);
        if (allocationProducts.Count != allocationProductIds.Count)
            throw new ArgumentException("Sản phẩm dùng để kiểm tra sức chứa không còn tồn tại.");

        var groupIds = requested.Where(bin => bin.StorageRack?.WarehouseZone.ProductGroupId.HasValue == true)
            .Select(bin => bin.StorageRack!.WarehouseZone.ProductGroupId!.Value).Distinct().ToList();
        var groups = await _context.ProductGroups.AsNoTracking()
            .Where(group => groupIds.Contains(group.ProductGroupId))
            .ToDictionaryAsync(group => group.ProductGroupId);
        var unitIds = groups.Values.Where(group => group.BaseUnitOfMeasureId.HasValue)
            .Select(group => group.BaseUnitOfMeasureId!.Value).Distinct().ToList();
        var units = await _context.UnitsOfMeasures.AsNoTracking()
            .Where(unit => unitIds.Contains(unit.UnitOfMeasureId))
            .ToDictionaryAsync(unit => unit.UnitOfMeasureId);

        foreach (var allocation in allocations.Where(item => item.Quantity > 0))
        {
            var bin = allBins[allocation.StorageLocationId];
            var zone = bin.StorageRack?.WarehouseZone;
            if (zone?.ProductGroupId is not long groupId ||
                !groups.TryGetValue(groupId, out var group) ||
                !group.BaseUnitOfMeasureId.HasValue)
                throw new InvalidOperationException(
                    $"Zone của Bin {bin.LocationCode} chưa cấu hình Product Group và ĐVT cơ sở; không thể đưa hàng vào.");
            var product = allocationProducts[allocation.ProductId];
            if (product.ProductGroupId != groupId ||
                product.UnitOfMeasureId != group.BaseUnitOfMeasureId.Value)
                throw new InvalidOperationException(
                    $"Sản phẩm {product.ProductCode} không thuộc Product Group/ĐVT cơ sở của Zone {zone.ZoneCode}.");
        }

        var stockByBin = stock.GroupBy(row => row.StorageLocationId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var addedByBin = allocations.GroupBy(item => item.StorageLocationId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var result = new Dictionary<long, LocationCapacityEvaluationDto>();

        foreach (var bin in requested)
        {
            var rack = bin.StorageRack;
            var zone = rack?.WarehouseZone;
            var group = zone?.ProductGroupId is long groupId && groups.TryGetValue(groupId, out var found)
                ? found : null;
            var unitName = group?.BaseUnitOfMeasureId is int unitId && units.TryGetValue(unitId, out var unit)
                ? unit.UnitName : null;
            var binIds = new[] { bin.StorageLocationId };
            var rackBinIds = rack is null ? binIds : allBins.Values
                .Where(item => item.RackId == rack.RackId)
                .Select(item => item.StorageLocationId).ToArray();
            var zoneBinIds = zone is null ? binIds : allBins.Values
                .Where(item => item.StorageRack?.ZoneId == zone.ZoneId)
                .Select(item => item.StorageLocationId).ToArray();

            var binScope = Calculate(binIds, bin.MaxCapacityQuantity, group, stockByBin, addedByBin);
            var rackScope = Calculate(rackBinIds, rack?.MaxCapacityQuantity, group, stockByBin, addedByBin);
            var zoneScope = Calculate(zoneBinIds, zone?.MaxCapacityQuantity, group, stockByBin, addedByBin);
            var scopes = new List<CapacityScopeEvaluationDto>();
            if (rack is not null)
                scopes.Add(ToScope("RACK", rack.RackId, rack.RackCode, rack.MaxCapacityQuantity, rackScope));
            if (zone is not null)
                scopes.Add(ToScope("ZONE", zone.ZoneId, zone.ZoneCode, zone.MaxCapacityQuantity, zoneScope));

            result[bin.StorageLocationId] = new LocationCapacityEvaluationDto
            {
                StorageLocationId = bin.StorageLocationId,
                LocationCode = bin.LocationCode,
                ProductGroupName = group?.GroupName,
                UnitName = unitName,
                MaxCapacityQuantity = bin.MaxCapacityQuantity,
                CurrentQuantity = binScope.Current,
                AddedQuantity = binScope.Added,
                ProjectedQuantity = binScope.Projected,
                OverallStatus = MergeStatus(binScope.Status, rackScope.Status, zoneScope.Status),
                Scopes = scopes
            };
        }

        return result;
    }

    private static (decimal? Current, decimal Added, decimal? Projected, string Status) Calculate(
        IReadOnlyCollection<long> binIds,
        decimal? max,
        ProductGroup? group,
        IReadOnlyDictionary<long, List<BMWMS.Repository.Models.Inventory>> stock,
        IReadOnlyDictionary<long, decimal> added)
    {
        if (group?.BaseUnitOfMeasureId is not int unitId)
            return (null, binIds.Sum(id => added.GetValueOrDefault(id)), null,
                    CapacityEvaluationStatuses.NotConfigured);
        var rows = binIds.SelectMany(id => stock.GetValueOrDefault(id) ??
            new List<BMWMS.Repository.Models.Inventory>()).ToList();
        if (rows.Any(row => row.Product.ProductGroupId != group.ProductGroupId ||
                            row.Product.UnitOfMeasureId != unitId))
            return (null, binIds.Sum(id => added.GetValueOrDefault(id)), null,
                    CapacityEvaluationStatuses.Unknown);
        var current = rows.Sum(row => row.OnHandQuantity);
        var delta = binIds.Sum(id => added.GetValueOrDefault(id));
        var projected = current + delta;
        var status = max is null ? CapacityEvaluationStatuses.NotConfigured
            : projected > max.Value ? CapacityEvaluationStatuses.Exceeded
            : CapacityEvaluationStatuses.Available;
        return (current, delta, projected, status);
    }

    private static CapacityScopeEvaluationDto ToScope(
        string type, long id, string code, decimal? max,
        (decimal? Current, decimal Added, decimal? Projected, string Status) value) => new()
    {
        ScopeType = type, ScopeId = id, ScopeCode = code, MaxCapacityQuantity = max,
        CurrentQuantity = value.Current, AddedQuantity = value.Added,
        ProjectedQuantity = value.Projected, OverallStatus = value.Status
    };

    private static string MergeStatus(params string[] statuses) =>
        statuses.Contains(CapacityEvaluationStatuses.Exceeded) ? CapacityEvaluationStatuses.Exceeded :
        statuses.Contains(CapacityEvaluationStatuses.Unknown) ? CapacityEvaluationStatuses.Unknown :
        statuses.Contains(CapacityEvaluationStatuses.NotConfigured) ? CapacityEvaluationStatuses.NotConfigured :
        CapacityEvaluationStatuses.Available;
}
