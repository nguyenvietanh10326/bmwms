using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.Interfaces;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace BMWMS.Business.Services.Inventory
{
    public class StorageLocationService : IStorageLocationService
    {
        private readonly IStorageLocationRepository _locationRepository;
        private readonly ICapacityEvaluationService _capacityEvaluationService;
        private readonly IAuditLogService _auditLogService;

        public StorageLocationService(
            IStorageLocationRepository locationRepository,
            ICapacityEvaluationService capacityEvaluationService,
            IAuditLogService auditLogService)
        {
            _locationRepository = locationRepository;
            _capacityEvaluationService = capacityEvaluationService;
            _auditLogService = auditLogService;
        }

        // 1. Lấy dữ liệu danh sách vị trí + header kho
        public async Task<StorageLocationPageDto> GetLocationsPageAsync(StorageLocationFilterDto filter)
        {
            // Lấy Header Kho
            var warehouse = await _locationRepository.GetWarehouseByIdAsync(filter.WarehouseId);
            var zones = await _locationRepository.GetZonesByWarehouseAsync(filter.WarehouseId);
            var racks = await _locationRepository.GetRacksByWarehouseAsync(filter.WarehouseId);

            var warehouseDto = new WarehouseHeaderInfoDto
            {
                WarehouseId = warehouse?.WarehouseId ?? filter.WarehouseId,
                WarehouseCode = warehouse?.WarehouseCode ?? "N/A",
                WarehouseName = warehouse?.WarehouseName ?? "N/A",
                Address = warehouse?.Address ?? "Chưa cập nhật địa chỉ",
                TotalZones = zones.Count,
                TotalRacks = racks.Count
            };

            // Lấy danh sách từ Repo (Đã include StorageRack -> WarehouseZone & Inventories)
            var (rawItems, totalCount) = await _locationRepository.GetPagedLocationsAsync(
                filter.WarehouseId,
                filter.Keyword,
                filter.LocationType,
                filter.Status,
                filter.PageIndex,
                filter.PageSize,
                filter.ZoneId,
                filter.RackId);

            warehouseDto.TotalLocations = totalCount;

            // Map từ Entity sang DTO
            var items = rawItems.Select(l =>
            {
                var onHand = l.Inventories.Sum(inv => inv.OnHandQuantity);
                var prodCount = l.Inventories.Where(inv => inv.OnHandQuantity > 0).Select(inv => inv.ProductId).Distinct().Count();
                var lotCount = l.Inventories.Where(inv => inv.OnHandQuantity > 0).Select(inv => inv.ProductLotId).Distinct().Count();

                return new StorageLocationItemDto
                {
                    StorageLocationId = l.StorageLocationId,
                    WarehouseId = l.WarehouseId,
                    RackId = l.RackId,
                    ZoneId = l.StorageRack?.ZoneId,
                    LocationCode = l.LocationCode,
                    LocationName = l.LocationName ?? string.Empty,
                    LocationType = l.LocationType,

                    ZoneCode = l.StorageRack?.WarehouseZone?.ZoneCode ?? "N/A",
                    ZoneName = l.StorageRack?.WarehouseZone?.ZoneName ?? "N/A",
                    RackCode = l.StorageRack?.RackCode ?? "N/A",
                    RackName = l.StorageRack?.RackName ?? "N/A",

                    Floor = 1,
                    AreaSquareMeter = l.AreaSquareMeter,
                    MaxWeightKg = l.MaxWeightKg,
                    MaxVolumeM3 = l.MaxVolumeM3,

                    IsPutawayAllowed = l.IsPutawayAllowed,
                    IsPickable = l.IsPickable,
                    Status = l.Status,

                    StoredProductCount = prodCount,
                    StoredLotCount = lotCount,
                    TotalOnHandQuantity = onHand
                };
            }).ToList();

            return new StorageLocationPageDto
            {
                WarehouseInfo = warehouseDto,
                Items = items,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<StorageLocationItemDto?> GetByIdAsync(long locationId)
        {
            var item = await _locationRepository.GetByIdAsync(locationId);
            if (item == null) return null;

            var onHand = item.Inventories.Sum(inv => inv.OnHandQuantity);
            var prodCount = item.Inventories.Where(inv => inv.OnHandQuantity > 0).Select(inv => inv.ProductId).Distinct().Count();
            var lotCount = item.Inventories.Where(inv => inv.OnHandQuantity > 0).Select(inv => inv.ProductLotId).Distinct().Count();

            var result = new StorageLocationItemDto
            {
                StorageLocationId = item.StorageLocationId,
                WarehouseId = item.WarehouseId,
                RackId = item.RackId,
                ZoneId = item.StorageRack?.ZoneId,
                LocationCode = item.LocationCode,
                LocationName = item.LocationName ?? "",
                LocationType = item.LocationType,
                ZoneCode = item.StorageRack?.WarehouseZone?.ZoneCode ?? "N/A",
                ZoneName = item.StorageRack?.WarehouseZone?.ZoneName ?? "N/A",
                RackCode = item.StorageRack?.RackCode ?? "N/A",
                RackName = item.StorageRack?.RackName ?? "N/A",
                Floor = 1,
                AreaSquareMeter = item.AreaSquareMeter,
                MaxWeightKg = item.MaxWeightKg,
                MaxVolumeM3 = item.MaxVolumeM3,
                IsPutawayAllowed = item.IsPutawayAllowed,
                IsPickable = item.IsPickable,
                Status = item.Status,
                StoredProductCount = prodCount,
                StoredLotCount = lotCount,
                TotalOnHandQuantity = onHand
            };

            var evaluations = await _capacityEvaluationService.EvaluateCurrentAsync([locationId]);
            ApplyCapacity(result, evaluations.GetValueOrDefault(locationId));
            return result;
        }

        public async Task<(bool Success, string Message)> CreateLocationAsync(CreateUpdateStorageLocationDto dto, long currentUserId = 0)
        {
            var warehouseValidation = await ValidateOperationalWarehouseAsync(dto.WarehouseId);
            if (warehouseValidation != null)
                return (false, warehouseValidation);
            var identityValidation = ValidateLocationIdentity(dto);
            if (identityValidation != null)
                return (false, identityValidation);
            var capacityValidation = ValidatePhysicalLimits(dto);
            if (capacityValidation != null)
                return (false, capacityValidation);
            var hierarchyValidation = await ValidateLocationHierarchyAsync(dto);
            if (hierarchyValidation != null)
                return (false, hierarchyValidation);

            var isDuplicate = await _locationRepository.ExistsCodeAsync(dto.WarehouseId, dto.LocationCode);
            if (isDuplicate)
            {
                return (false, $"Mã vị trí '{dto.LocationCode}' đã tồn tại trong kho này.");
            }

            var entity = new StorageLocation
            {
                WarehouseId = dto.WarehouseId,
                RackId = dto.RackId,
                LocationCode = dto.LocationCode.Trim().ToUpper(),
                LocationName = dto.LocationName?.Trim(),
                LocationType = "BIN",
                AreaSquareMeter = null,
                MaxWeightKg = dto.MaxWeightKg,
                MaxVolumeM3 = dto.MaxVolumeM3,
                IsPutawayAllowed = true,
                IsPickable = true,
                Status = "ACTIVE",
                CreatedAt = DateTime.Now
            };

            await _locationRepository.AddAsync(entity);
            await RecordAuditAsync(currentUserId, AuditActions.Create, AuditEntities.StorageLocation, entity.StorageLocationId, null,
                new { entity.LocationCode, entity.LocationName, entity.RackId, entity.MaxWeightKg, entity.MaxVolumeM3, entity.Status });
            return (true, "Thêm mới vị trí thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateLocationAsync(CreateUpdateStorageLocationDto dto, long currentUserId = 0)
        {
            var warehouseValidation = await ValidateOperationalWarehouseAsync(dto.WarehouseId);
            if (warehouseValidation != null)
                return (false, warehouseValidation);
            var identityValidation = ValidateLocationIdentity(dto);
            if (identityValidation != null)
                return (false, identityValidation);
            var capacityValidation = ValidatePhysicalLimits(dto);
            if (capacityValidation != null)
                return (false, capacityValidation);

            var entity = await _locationRepository.GetForUpdateAsync(dto.StorageLocationId);
            if (entity == null || entity.WarehouseId != dto.WarehouseId)
            {
                return (false, "Không tìm thấy Bin trong kho hiện tại.");
            }
            if (!entity.LocationType.Equals("BIN", StringComparison.OrdinalIgnoreCase))
                return (false, "Đây là vị trí kỹ thuật của luồng kho và không được sửa trên màn sơ đồ Bin.");

            var capacityReductionError = await ValidateCapacityReductionAsync(
                entity.StorageLocationId,
                entity.MaxWeightKg,
                dto.MaxWeightKg,
                entity.MaxVolumeM3,
                dto.MaxVolumeM3);
            if (capacityReductionError != null)
                return (false, capacityReductionError);

            if (entity.RackId != dto.RackId)
            {
                var moveBlockers = await _locationRepository.GetLocationDeactivationBlockersAsync(entity.StorageLocationId);
                if (moveBlockers.Count > 0)
                    return (false, $"Không thể chuyển Bin sang Rack khác vì {string.Join(", ", moveBlockers)}.");
            }
            var hierarchyValidation = await ValidateLocationHierarchyAsync(dto, dto.StorageLocationId);
            if (hierarchyValidation != null)
                return (false, hierarchyValidation);

            var isDuplicate = await _locationRepository.ExistsCodeAsync(dto.WarehouseId, dto.LocationCode, dto.StorageLocationId);
            if (isDuplicate)
            {
                return (false, $"Mã vị trí '{dto.LocationCode}' đã tồn tại trong kho này.");
            }

            var oldValue = new { entity.LocationCode, entity.LocationName, entity.RackId, entity.MaxWeightKg, entity.MaxVolumeM3, entity.Status };
            entity.RackId = dto.RackId;
            entity.LocationCode = dto.LocationCode.Trim().ToUpper();
            entity.LocationName = dto.LocationName?.Trim();
            entity.LocationType = "BIN";
            entity.MaxWeightKg = dto.MaxWeightKg;
            entity.MaxVolumeM3 = dto.MaxVolumeM3;
            entity.IsPutawayAllowed = true;
            entity.IsPickable = true;
            entity.UpdatedAt = DateTime.Now;

            await _locationRepository.UpdateAsync(entity);
            await RecordAuditAsync(currentUserId, AuditActions.Update, AuditEntities.StorageLocation, entity.StorageLocationId, oldValue,
                new { entity.LocationCode, entity.LocationName, entity.RackId, entity.MaxWeightKg, entity.MaxVolumeM3, entity.Status });
            return (true, "Cập nhật vị trí thành công.");
        }

        private static string? ValidatePhysicalLimits(CreateUpdateStorageLocationDto dto)
        {
            return ValidatePhysicalLimits(
                dto.AreaSquareMeter,
                dto.MaxWeightKg,
                dto.MaxVolumeM3,
                "vị trí");
        }

        private static string? ValidateLocationIdentity(CreateUpdateStorageLocationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.LocationCode))
                return "Mã Bin không được để trống.";
            if (dto.LocationCode.Trim().Length > 80)
                return "Mã Bin không được vượt quá 80 ký tự.";
            if (dto.LocationName?.Trim().Length > 200)
                return "Tên Bin không được vượt quá 200 ký tự.";
            return null;
        }

        private async Task<string?> ValidateLocationHierarchyAsync(
            CreateUpdateStorageLocationDto dto,
            long? excludeLocationId = null)
        {
            if (!dto.RackId.HasValue || dto.RackId <= 0)
                return "Vị trí Bin phải thuộc một Rack để thể hiện đúng cấu trúc Zone → Rack → Bin.";

            var rack = (await _locationRepository.GetRacksByWarehouseAsync(dto.WarehouseId))
                .FirstOrDefault(item => item.RackId == dto.RackId.Value);
            if (rack == null || rack.WarehouseId != dto.WarehouseId)
                return "Rack đã chọn không thuộc kho hiện tại.";
            if (!IsUsableNodeStatus(rack.Status) || !IsUsableNodeStatus(rack.WarehouseZone?.Status))
                return "Chỉ được đặt Bin trong Rack và Zone đang hoạt động.";

            var siblings = rack.StorageLocations
                .Where(location => !excludeLocationId.HasValue || location.StorageLocationId != excludeLocationId.Value)
                .ToList();
            return ValidateChildBudget(
                "Tổng giới hạn các Bin",
                $"Rack {rack.RackCode}",
                null,
                dto.MaxWeightKg,
                dto.MaxVolumeM3,
                siblings.Sum(location => location.AreaSquareMeter ?? 0),
                siblings.Sum(location => location.MaxWeightKg ?? 0),
                siblings.Sum(location => location.MaxVolumeM3 ?? 0),
                rack.AreaSquareMeter,
                rack.MaxWeightKg,
                rack.MaxVolumeM3);
        }

        // --- HIERARCHICAL STRUCTURE METHODS ---
        public async Task<List<WarehouseZoneDto>> GetZonesAsync(long warehouseId)
        {
            var zones = await _locationRepository.GetZonesByWarehouseAsync(warehouseId);
            return zones.Where(z => !IsTechnicalZone(z.ZoneCode)).Select(z => new WarehouseZoneDto
            {
                ZoneId = z.ZoneId,
                WarehouseId = z.WarehouseId,
                ZoneCode = z.ZoneCode,
                ZoneName = z.ZoneName,
                Description = z.Description,
                AreaSquareMeter = z.AreaSquareMeter,
                MaxWeightKg = z.MaxWeightKg,
                MaxVolumeM3 = z.MaxVolumeM3,
                AllocatedRackAreaSquareMeter = z.StorageRacks.Sum(r => r.AreaSquareMeter ?? 0),
                AllocatedRackWeightKg = z.StorageRacks.Sum(r => r.MaxWeightKg ?? 0),
                AllocatedRackVolumeM3 = z.StorageRacks.Sum(r => r.MaxVolumeM3 ?? 0),
                Status = z.Status,
                RackCount = z.StorageRacks.Count,
                LocationCount = z.StorageRacks.SelectMany(r => r.StorageLocations).Count()
            }).ToList();
        }

        public async Task<List<StorageRackDto>> GetRacksAsync(long warehouseId, long? zoneId = null)
        {
            var racks = await _locationRepository.GetRacksByWarehouseAsync(warehouseId, zoneId);
            return racks.Where(r => !IsTechnicalZone(r.WarehouseZone?.ZoneCode)).Select(r => new StorageRackDto
            {
                RackId = r.RackId,
                WarehouseId = r.WarehouseId,
                ZoneId = r.ZoneId,
                ZoneCode = r.WarehouseZone?.ZoneCode ?? "N/A",
                ZoneName = r.WarehouseZone?.ZoneName ?? "N/A",
                RackCode = r.RackCode,
                RackName = r.RackName,
                AreaSquareMeter = r.AreaSquareMeter,
                MaxWeightKg = r.MaxWeightKg,
                MaxVolumeM3 = r.MaxVolumeM3,
                AllocatedLocationAreaSquareMeter = r.StorageLocations.Sum(l => l.AreaSquareMeter ?? 0),
                AllocatedLocationWeightKg = r.StorageLocations.Sum(l => l.MaxWeightKg ?? 0),
                AllocatedLocationVolumeM3 = r.StorageLocations.Sum(l => l.MaxVolumeM3 ?? 0),
                Status = r.Status,
                LocationCount = r.StorageLocations.Count
            }).ToList();
        }

        public async Task<WarehouseStructureDto> GetWarehouseStructureAsync(long warehouseId)
        {
            var warehouse = await _locationRepository.GetWarehouseByIdAsync(warehouseId);
            var zones = (await _locationRepository.GetZonesByWarehouseAsync(warehouseId))
                .Where(zone => !IsTechnicalZone(zone.ZoneCode)).ToList();
            var allLocations = await _locationRepository.GetAllLocationsWithHierarchyAndInventoryAsync(warehouseId);
            var hierarchyIssueCount = allLocations.Count(location =>
                location.LocationType.Equals("BIN", StringComparison.OrdinalIgnoreCase) && !location.RackId.HasValue);

            var mappedLocations = allLocations
                .Where(l => l.RackId.HasValue && l.LocationType.Equals("BIN", StringComparison.OrdinalIgnoreCase))
                .Select(l =>
            {
                var onHand = l.Inventories.Sum(inv => inv.OnHandQuantity);
                var prodCount = l.Inventories.Where(inv => inv.OnHandQuantity > 0).Select(inv => inv.ProductId).Distinct().Count();
                var lotCount = l.Inventories.Where(inv => inv.OnHandQuantity > 0).Select(inv => inv.ProductLotId).Distinct().Count();

                return new StorageLocationItemDto
                {
                    StorageLocationId = l.StorageLocationId,
                    WarehouseId = l.WarehouseId,
                    RackId = l.RackId,
                    ZoneId = l.StorageRack?.ZoneId,
                    LocationCode = l.LocationCode,
                    LocationName = l.LocationName ?? string.Empty,
                    LocationType = l.LocationType,
                    ZoneCode = l.StorageRack?.WarehouseZone?.ZoneCode ?? "N/A",
                    ZoneName = l.StorageRack?.WarehouseZone?.ZoneName ?? "N/A",
                    RackCode = l.StorageRack?.RackCode ?? "N/A",
                    RackName = l.StorageRack?.RackName ?? "N/A",
                    Floor = 1,
                    AreaSquareMeter = l.AreaSquareMeter,
                    MaxWeightKg = l.MaxWeightKg,
                    MaxVolumeM3 = l.MaxVolumeM3,
                    IsPutawayAllowed = l.IsPutawayAllowed,
                    IsPickable = l.IsPickable,
                    Status = l.Status,
                    StoredProductCount = prodCount,
                    StoredLotCount = lotCount,
                    TotalOnHandQuantity = onHand
                };
            }).ToList();

            var capacityEvaluations = await _capacityEvaluationService.EvaluateCurrentAsync(
                mappedLocations.Select(location => location.StorageLocationId).ToList());
            foreach (var location in mappedLocations)
                ApplyCapacity(location, capacityEvaluations.GetValueOrDefault(location.StorageLocationId));

            var locationByRack = mappedLocations
                .Where(l => l.RackId.HasValue)
                .GroupBy(l => l.RackId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var zoneStructures = zones.OrderBy(z => NaturalSortKey(z.ZoneCode), StringComparer.OrdinalIgnoreCase)
            .Select(z => new WarehouseZoneStructureDto
            {
                ZoneId = z.ZoneId,
                ZoneCode = z.ZoneCode,
                ZoneName = z.ZoneName,
                Description = z.Description,
                AreaSquareMeter = z.AreaSquareMeter,
                MaxWeightKg = z.MaxWeightKg,
                MaxVolumeM3 = z.MaxVolumeM3,
                AllocatedRackAreaSquareMeter = z.StorageRacks.Sum(r => r.AreaSquareMeter ?? 0),
                AllocatedRackWeightKg = z.StorageRacks.Sum(r => r.MaxWeightKg ?? 0),
                AllocatedRackVolumeM3 = z.StorageRacks.Sum(r => r.MaxVolumeM3 ?? 0),
                Status = z.Status,
                Racks = z.StorageRacks.OrderBy(r => NaturalSortKey(r.RackCode), StringComparer.OrdinalIgnoreCase)
                .Select(r => new StorageRackStructureDto
                {
                    RackId = r.RackId,
                    ZoneId = r.ZoneId,
                    RackCode = r.RackCode,
                    RackName = r.RackName,
                    AreaSquareMeter = r.AreaSquareMeter,
                    MaxWeightKg = r.MaxWeightKg,
                    MaxVolumeM3 = r.MaxVolumeM3,
                    AllocatedLocationAreaSquareMeter = r.StorageLocations.Sum(l => l.AreaSquareMeter ?? 0),
                    AllocatedLocationWeightKg = r.StorageLocations.Sum(l => l.MaxWeightKg ?? 0),
                    AllocatedLocationVolumeM3 = r.StorageLocations.Sum(l => l.MaxVolumeM3 ?? 0),
                    Status = r.Status,
                    Locations = locationByRack.TryGetValue(r.RackId, out var locs)
                        ? locs.OrderBy(location => NaturalSortKey(location.LocationCode), StringComparer.OrdinalIgnoreCase).ToList()
                        : new List<StorageLocationItemDto>()
                }).ToList()
            }).ToList();

            foreach (var zone in zoneStructures)
            {
                var zoneScope = capacityEvaluations.Values.SelectMany(item => item.Scopes)
                    .FirstOrDefault(scope => scope.ScopeType == "ZONE" && scope.ScopeId == zone.ZoneId);
                ApplyScopeCapacity(zone, zoneScope);
                foreach (var rack in zone.Racks)
                {
                    var rackScope = capacityEvaluations.Values.SelectMany(item => item.Scopes)
                        .FirstOrDefault(scope => scope.ScopeType == "RACK" && scope.ScopeId == rack.RackId);
                    ApplyScopeCapacity(rack, rackScope);
                }
            }

            int available = mappedLocations.Count(l => l.OccupancyStatus == "Available");
            int occupied = mappedLocations.Count(l => l.OccupancyStatus == "Occupied");
            int full = mappedLocations.Count(l => l.OccupancyStatus == "Full");
            int unknown = mappedLocations.Count(l => l.OccupancyStatus == "Unknown");
            int blocked = mappedLocations.Count(l => l.OccupancyStatus == "Blocked");

            return new WarehouseStructureDto
            {
                WarehouseInfo = new WarehouseHeaderInfoDto
                {
                    WarehouseId = warehouse?.WarehouseId ?? warehouseId,
                    WarehouseCode = warehouse?.WarehouseCode ?? "N/A",
                    WarehouseName = warehouse?.WarehouseName ?? "N/A",
                    Address = warehouse?.Address ?? "Chưa cập nhật địa chỉ",
                    TotalZones = zones.Count,
                    TotalRacks = zones.Sum(z => z.StorageRacks.Count),
                    TotalLocations = mappedLocations.Count
                },
                Zones = zoneStructures,
                TotalAvailableBins = available,
                TotalOccupiedBins = occupied,
                TotalFullBins = full,
                TotalUnknownBins = unknown,
                TotalBlockedBins = blocked,
                HierarchyIssueCount = hierarchyIssueCount
            };
        }

        private async Task<string?> ValidateCapacityReductionAsync(
            long locationId,
            decimal? oldMaxWeightKg,
            decimal? newMaxWeightKg,
            decimal? oldMaxVolumeM3,
            decimal? newMaxVolumeM3)
        {
            var weightRequiresValidation = newMaxWeightKg.HasValue
                ? !oldMaxWeightKg.HasValue || newMaxWeightKg.Value < oldMaxWeightKg.Value
                : oldMaxWeightKg.HasValue;
            var volumeRequiresValidation = newMaxVolumeM3.HasValue
                ? !oldMaxVolumeM3.HasValue || newMaxVolumeM3.Value < oldMaxVolumeM3.Value
                : oldMaxVolumeM3.HasValue;
            if (!weightRequiresValidation && !volumeRequiresValidation)
                return null;

            var evaluations = await _capacityEvaluationService.EvaluateCurrentAsync([locationId]);
            var evaluation = evaluations.GetValueOrDefault(locationId);
            if (evaluation == null)
                return "Không thể xác minh mức sử dụng hiện tại của Bin.";

            if (weightRequiresValidation)
            {
                if (evaluation.CurrentWeightKg == null && evaluation.MissingWeightProductCodes.Count > 0)
                    return "Không thể giảm tải trọng vì còn sản phẩm chưa cấu hình quy đổi khối lượng lưu kho.";
                if (!newMaxWeightKg.HasValue && evaluation.CurrentWeightKg > 0)
                    return "Không thể xóa giới hạn tải trọng khi Bin vẫn còn hàng.";
                if (newMaxWeightKg.HasValue && evaluation.CurrentWeightKg > newMaxWeightKg.Value)
                    return $"Tải trọng mới nhỏ hơn mức đang sử dụng ({evaluation.CurrentWeightKg:0.###} kg).";
            }
            if (volumeRequiresValidation)
            {
                if (evaluation.CurrentVolumeM3 == null && evaluation.MissingVolumeProductCodes.Count > 0)
                    return "Không thể giảm thể tích vì còn sản phẩm chưa có hồ sơ thể tích lưu kho.";
                if (!newMaxVolumeM3.HasValue && evaluation.CurrentVolumeM3 > 0)
                    return "Không thể xóa giới hạn thể tích khi Bin vẫn còn hàng.";
                if (newMaxVolumeM3.HasValue && evaluation.CurrentVolumeM3 > newMaxVolumeM3.Value)
                    return $"Thể tích mới nhỏ hơn mức đang sử dụng ({evaluation.CurrentVolumeM3:0.###} m³).";
            }
            return null;
        }

        private static void ApplyCapacity(
            StorageLocationItemDto location,
            LocationCapacityEvaluationDto? evaluation)
        {
            if (evaluation == null)
                return;

            location.CurrentWeightKg = evaluation.CurrentWeightKg;
            location.CurrentVolumeM3 = evaluation.CurrentVolumeM3;
            location.WeightCapacityStatus = evaluation.WeightStatus;
            location.VolumeCapacityStatus = evaluation.VolumeStatus;
            location.CapacityStatus = evaluation.OverallStatus;
            location.HasMissingCapacityData = evaluation.OverallStatus.Equals(
                CapacityEvaluationStatuses.Unknown,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyScopeCapacity(WarehouseZoneStructureDto zone, CapacityScopeEvaluationDto? scope)
        {
            if (scope == null) return;
            zone.CurrentWeightKg = scope.CurrentWeightKg;
            zone.CurrentVolumeM3 = scope.CurrentVolumeM3;
            zone.CapacityStatus = scope.OverallStatus;
            zone.HasMissingCapacityData = scope.OverallStatus.Equals(
                CapacityEvaluationStatuses.Unknown,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyScopeCapacity(StorageRackStructureDto rack, CapacityScopeEvaluationDto? scope)
        {
            if (scope == null) return;
            rack.CurrentWeightKg = scope.CurrentWeightKg;
            rack.CurrentVolumeM3 = scope.CurrentVolumeM3;
            rack.CapacityStatus = scope.OverallStatus;
            rack.HasMissingCapacityData = scope.OverallStatus.Equals(
                CapacityEvaluationStatuses.Unknown,
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<LocationDetailWithInventoryDto?> GetLocationInventoryAsync(long locationId)
        {
            var location = await GetByIdAsync(locationId);
            if (location == null) return null;

            var rawInventories = await _locationRepository.GetLocationInventoryAsync(locationId);
            var inventories = rawInventories.Select(inv => new LocationInventoryItemDto
            {
                InventoryId = inv.InventoryId,
                ProductId = inv.ProductId,
                ProductCode = inv.Product.ProductCode,
                ProductName = inv.Product.ProductName,
                UnitOfMeasure = inv.Product.UnitOfMeasure?.UnitName ?? inv.Product.UnitOfMeasure?.UnitCode ?? "Chưa cấu hình",
                QuantityScale = inv.Product.UnitOfMeasure?.QuantityScale ?? 0,
                ProductGroupName = inv.Product.ProductGroup?.GroupName ?? "Chưa cấu hình",
                ProductLotId = inv.ProductLotId,
                LotNumber = inv.ProductLot?.LotNumber ?? "N/A",
                ManufactureDate = inv.ProductLot?.ManufactureDate,
                ExpiryDate = inv.ProductLot?.ExpiryDate,
                LotStatus = inv.ProductLot?.Status ?? "Active",
                OnHandQuantity = inv.OnHandQuantity,
                ReservedQuantity = inv.ReservedQuantity,
                AvailableQuantity = inv.AvailableQuantity ?? (inv.OnHandQuantity - inv.ReservedQuantity),
                LastUpdatedAt = inv.LastUpdatedAt
            }).ToList();

            return new LocationDetailWithInventoryDto
            {
                Location = location,
                Inventories = inventories
            };
        }

        public async Task<List<ProductLocationSearchItemDto>> SearchProductLocationsAsync(long warehouseId, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<ProductLocationSearchItemDto>();

            var rawLookup = await _locationRepository.SearchProductLocationsAsync(warehouseId, keyword);
            return rawLookup.Select(v => new ProductLocationSearchItemDto
            {
                ProductId = v.ProductId,
                ProductCode = v.ProductCode,
                ProductName = v.ProductName,
                UnitOfMeasure = v.UnitCode,
                ProductLotId = v.ProductLotId,
                LotNumber = v.LotNumber,
                ExpiryDate = v.ExpiryDate,
                WarehouseId = v.WarehouseId,
                WarehouseName = v.WarehouseName,
                StorageLocationId = v.StorageLocationId,
                LocationCode = v.LocationCode,
                LocationType = "Bin",
                OnHandQuantity = v.OnHandQuantity,
                AvailableQuantity = v.AvailableQuantity ?? (v.OnHandQuantity - v.ReservedQuantity)
            }).ToList();
        }

        public async Task<(bool Success, string Message)> CreateZoneAsync(CreateUpdateZoneDto dto, long currentUserId = 0)
        {
            var warehouseValidation = await ValidateOperationalWarehouseAsync(dto.WarehouseId);
            if (warehouseValidation != null)
                return (false, warehouseValidation);
            var identityValidation = ValidateZoneIdentity(dto);
            if (identityValidation != null)
                return (false, identityValidation);
            var capacityValidation = ValidatePhysicalLimits(
                dto.AreaSquareMeter, dto.MaxWeightKg, dto.MaxVolumeM3, "khu vực");
            if (capacityValidation != null)
                return (false, capacityValidation);

            var isDuplicate = await _locationRepository.ExistsZoneCodeAsync(dto.WarehouseId, dto.ZoneCode);
            if (isDuplicate)
            {
                return (false, $"Mã khu vực '{dto.ZoneCode}' đã tồn tại trong kho này.");
            }

            var zone = new WarehouseZone
            {
                WarehouseId = dto.WarehouseId,
                ZoneCode = dto.ZoneCode.Trim().ToUpper(),
                ZoneName = dto.ZoneName.Trim(),
                Description = dto.Description?.Trim(),
                AreaSquareMeter = null,
                MaxWeightKg = dto.MaxWeightKg,
                MaxVolumeM3 = dto.MaxVolumeM3,
                Status = "ACTIVE"
            };

            await _locationRepository.AddZoneAsync(zone);
            await RecordAuditAsync(currentUserId, AuditActions.Create, AuditEntities.WarehouseZone, zone.ZoneId, null,
                new { zone.ZoneCode, zone.ZoneName, zone.Description, zone.MaxWeightKg, zone.MaxVolumeM3, zone.Status });
            return (true, "Thêm mới khu vực (Zone) thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateZoneAsync(CreateUpdateZoneDto dto, long currentUserId = 0)
        {
            var warehouseValidation = await ValidateOperationalWarehouseAsync(dto.WarehouseId);
            if (warehouseValidation != null)
                return (false, warehouseValidation);
            var identityValidation = ValidateZoneIdentity(dto);
            if (identityValidation != null)
                return (false, identityValidation);
            var capacityValidation = ValidatePhysicalLimits(
                dto.AreaSquareMeter, dto.MaxWeightKg, dto.MaxVolumeM3, "khu vực");
            if (capacityValidation != null)
                return (false, capacityValidation);

            var zone = await _locationRepository.GetZoneByIdAsync(dto.ZoneId);
            if (zone == null || zone.WarehouseId != dto.WarehouseId)
                return (false, "Không tìm thấy khu vực trong kho hiện tại.");
            if (await _locationRepository.ExistsZoneCodeAsync(dto.WarehouseId, dto.ZoneCode, dto.ZoneId))
                return (false, $"Mã khu vực '{dto.ZoneCode}' đã tồn tại trong kho này.");

            var childValidation = ValidateParentAgainstExistingChildren(
                "Khu vực",
                dto.AreaSquareMeter,
                dto.MaxWeightKg,
                dto.MaxVolumeM3,
                zone.StorageRacks.Sum(rack => rack.AreaSquareMeter ?? 0),
                zone.StorageRacks.Sum(rack => rack.MaxWeightKg ?? 0),
                zone.StorageRacks.Sum(rack => rack.MaxVolumeM3 ?? 0));
            if (childValidation != null)
                return (false, childValidation);

            var actualCapacityValidation = await ValidateScopeCapacityReductionAsync(
                zone.StorageRacks.SelectMany(rack => rack.StorageLocations).Select(location => location.StorageLocationId),
                "ZONE", zone.ZoneId,
                zone.MaxWeightKg, dto.MaxWeightKg,
                zone.MaxVolumeM3, dto.MaxVolumeM3,
                "Zone");
            if (actualCapacityValidation != null)
                return (false, actualCapacityValidation);

            var oldValue = new { zone.ZoneCode, zone.ZoneName, zone.Description, zone.MaxWeightKg, zone.MaxVolumeM3, zone.Status };
            zone.ZoneCode = dto.ZoneCode.Trim().ToUpperInvariant();
            zone.ZoneName = dto.ZoneName.Trim();
            zone.Description = dto.Description?.Trim();
            zone.MaxWeightKg = dto.MaxWeightKg;
            zone.MaxVolumeM3 = dto.MaxVolumeM3;
            await _locationRepository.UpdateZoneAsync(zone);
            await RecordAuditAsync(currentUserId, AuditActions.Update, AuditEntities.WarehouseZone, zone.ZoneId, oldValue,
                new { zone.ZoneCode, zone.ZoneName, zone.Description, zone.MaxWeightKg, zone.MaxVolumeM3, zone.Status });
            return (true, "Cập nhật khu vực và sức chứa thành công.");
        }

        public async Task<(bool Success, string Message)> CreateRackAsync(CreateUpdateRackDto dto, long currentUserId = 0)
        {
            var warehouseValidation = await ValidateOperationalWarehouseAsync(dto.WarehouseId);
            if (warehouseValidation != null)
                return (false, warehouseValidation);
            var identityValidation = ValidateRackIdentity(dto);
            if (identityValidation != null)
                return (false, identityValidation);

            if (dto.ZoneId <= 0)
            {
                return (false, "Vui lòng chọn Khu vực (Zone) cho kệ này.");
            }
            var capacityValidation = ValidatePhysicalLimits(
                dto.AreaSquareMeter, dto.MaxWeightKg, dto.MaxVolumeM3, "kệ");
            if (capacityValidation != null)
                return (false, capacityValidation);

            var zone = await _locationRepository.GetZoneByIdAsync(dto.ZoneId);
            if (zone == null || zone.WarehouseId != dto.WarehouseId)
                return (false, "Khu vực đã chọn không thuộc kho hiện tại.");
            if (!IsUsableNodeStatus(zone.Status))
                return (false, "Chỉ được tạo Rack trong Zone đang hoạt động.");
            var hierarchyValidation = ValidateChildBudget(
                "Tổng giới hạn các Rack",
                $"Zone {zone.ZoneCode}",
                dto.AreaSquareMeter,
                dto.MaxWeightKg,
                dto.MaxVolumeM3,
                zone.StorageRacks.Sum(rack => rack.AreaSquareMeter ?? 0),
                zone.StorageRacks.Sum(rack => rack.MaxWeightKg ?? 0),
                zone.StorageRacks.Sum(rack => rack.MaxVolumeM3 ?? 0),
                zone.AreaSquareMeter,
                zone.MaxWeightKg,
                zone.MaxVolumeM3);
            if (hierarchyValidation != null)
                return (false, hierarchyValidation);

            var isDuplicate = await _locationRepository.ExistsRackCodeAsync(dto.WarehouseId, dto.RackCode);
            if (isDuplicate)
            {
                return (false, $"Mã kệ '{dto.RackCode}' đã tồn tại trong kho này.");
            }

            var rack = new StorageRack
            {
                WarehouseId = dto.WarehouseId,
                ZoneId = dto.ZoneId,
                RackCode = dto.RackCode.Trim().ToUpper(),
                RackName = dto.RackName.Trim(),
                AreaSquareMeter = null,
                MaxWeightKg = dto.MaxWeightKg,
                MaxVolumeM3 = dto.MaxVolumeM3,
                Status = "ACTIVE"
            };

            await _locationRepository.AddRackAsync(rack);
            await RecordAuditAsync(currentUserId, AuditActions.Create, AuditEntities.StorageRack, rack.RackId, null,
                new { rack.RackCode, rack.RackName, rack.ZoneId, rack.MaxWeightKg, rack.MaxVolumeM3, rack.Status });
            return (true, "Thêm mới kệ chứa (Rack) thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateRackAsync(CreateUpdateRackDto dto, long currentUserId = 0)
        {
            var warehouseValidation = await ValidateOperationalWarehouseAsync(dto.WarehouseId);
            if (warehouseValidation != null)
                return (false, warehouseValidation);
            var identityValidation = ValidateRackIdentity(dto);
            if (identityValidation != null)
                return (false, identityValidation);
            var capacityValidation = ValidatePhysicalLimits(
                dto.AreaSquareMeter, dto.MaxWeightKg, dto.MaxVolumeM3, "kệ");
            if (capacityValidation != null)
                return (false, capacityValidation);

            var rack = await _locationRepository.GetRackByIdAsync(dto.RackId);
            if (rack == null || rack.WarehouseId != dto.WarehouseId)
                return (false, "Không tìm thấy Rack trong kho hiện tại.");
            if (await _locationRepository.ExistsRackCodeAsync(dto.WarehouseId, dto.RackCode, dto.RackId))
                return (false, $"Mã kệ '{dto.RackCode}' đã tồn tại trong kho này.");

            var childValidation = ValidateParentAgainstExistingChildren(
                "Rack",
                dto.AreaSquareMeter,
                dto.MaxWeightKg,
                dto.MaxVolumeM3,
                rack.StorageLocations.Sum(location => location.AreaSquareMeter ?? 0),
                rack.StorageLocations.Sum(location => location.MaxWeightKg ?? 0),
                rack.StorageLocations.Sum(location => location.MaxVolumeM3 ?? 0));
            if (childValidation != null)
                return (false, childValidation);

            var actualCapacityValidation = await ValidateScopeCapacityReductionAsync(
                rack.StorageLocations.Select(location => location.StorageLocationId),
                "RACK", rack.RackId,
                rack.MaxWeightKg, dto.MaxWeightKg,
                rack.MaxVolumeM3, dto.MaxVolumeM3,
                "Rack");
            if (actualCapacityValidation != null)
                return (false, actualCapacityValidation);

            var zone = await _locationRepository.GetZoneByIdAsync(dto.ZoneId);
            if (zone == null || zone.WarehouseId != dto.WarehouseId)
                return (false, "Khu vực đã chọn không thuộc kho hiện tại.");
            if (rack.ZoneId != dto.ZoneId && !IsUsableNodeStatus(zone.Status))
                return (false, "Không thể chuyển Rack vào Zone đang ngừng hoạt động hoặc bị khóa.");
            if (rack.ZoneId != dto.ZoneId)
            {
                foreach (var location in rack.StorageLocations)
                {
                    var blockers = await _locationRepository.GetLocationDeactivationBlockersAsync(location.StorageLocationId);
                    if (blockers.Count > 0)
                        return (false, $"Không thể chuyển Rack vì Bin {location.LocationCode} {string.Join(", ", blockers)}.");
                }
            }
            var siblingRacks = zone.StorageRacks.Where(item => item.RackId != dto.RackId).ToList();
            var hierarchyValidation = ValidateChildBudget(
                "Tổng giới hạn các Rack",
                $"Zone {zone.ZoneCode}",
                dto.AreaSquareMeter,
                dto.MaxWeightKg,
                dto.MaxVolumeM3,
                siblingRacks.Sum(item => item.AreaSquareMeter ?? 0),
                siblingRacks.Sum(item => item.MaxWeightKg ?? 0),
                siblingRacks.Sum(item => item.MaxVolumeM3 ?? 0),
                zone.AreaSquareMeter,
                zone.MaxWeightKg,
                zone.MaxVolumeM3);
            if (hierarchyValidation != null)
                return (false, hierarchyValidation);

            var oldValue = new { rack.RackCode, rack.RackName, rack.ZoneId, rack.MaxWeightKg, rack.MaxVolumeM3, rack.Status };
            rack.ZoneId = dto.ZoneId;
            rack.RackCode = dto.RackCode.Trim().ToUpperInvariant();
            rack.RackName = dto.RackName.Trim();
            rack.MaxWeightKg = dto.MaxWeightKg;
            rack.MaxVolumeM3 = dto.MaxVolumeM3;
            await _locationRepository.UpdateRackAsync(rack);
            await RecordAuditAsync(currentUserId, AuditActions.Update, AuditEntities.StorageRack, rack.RackId, oldValue,
                new { rack.RackCode, rack.RackName, rack.ZoneId, rack.MaxWeightKg, rack.MaxVolumeM3, rack.Status });
            return (true, "Cập nhật Rack và sức chứa thành công.");
        }

        public async Task<(bool Success, string Message)> ChangeLocationStatusAsync(long locationId, bool active, long currentUserId = 0)
        {
            var location = await _locationRepository.GetForUpdateAsync(locationId);
            if (location == null)
                return (false, "Không tìm thấy Bin.");
            if (!location.LocationType.Equals("BIN", StringComparison.OrdinalIgnoreCase))
                return (false, "Vị trí kỹ thuật không được thay đổi trên màn quản lý Bin.");
            var warehouseValidation = await ValidateOperationalWarehouseAsync(location.WarehouseId);
            if (warehouseValidation != null)
                return (false, warehouseValidation);
            if (location.Status.Equals("BLOCKED", StringComparison.OrdinalIgnoreCase))
                return (false, "Bin đang bị khóa bởi nghiệp vụ kỹ thuật; không được đổi trạng thái tại màn cấu hình.");
            if (active == !IsInactive(location.Status))
                return (true, active ? "Bin đã ở trạng thái hoạt động." : "Bin đã ngừng sử dụng.");

            var oldStatus = location.Status;
            if (!active)
            {
                var blockers = await _locationRepository.GetLocationDeactivationBlockersAsync(locationId);
                if (blockers.Count > 0)
                    return (false, $"Không thể ngừng sử dụng Bin vì {string.Join(", ", blockers)}.");
                location.Status = "INACTIVE";
            }
            else
            {
                if (!location.RackId.HasValue)
                    return (false, "Bin chưa thuộc Rack nên không thể kích hoạt.");
                var rack = (await _locationRepository.GetRacksByWarehouseAsync(location.WarehouseId))
                    .FirstOrDefault(item => item.RackId == location.RackId.Value);
                if (rack == null || !IsUsableNodeStatus(rack.Status) || !IsUsableNodeStatus(rack.WarehouseZone?.Status))
                    return (false, "Hãy kích hoạt Zone và Rack cha trước khi kích hoạt Bin.");
                location.Status = "ACTIVE";
            }

            location.UpdatedAt = DateTime.Now;
            await _locationRepository.UpdateAsync(location);
            await RecordAuditAsync(currentUserId, AuditActions.ChangeStatus, AuditEntities.StorageLocation, location.StorageLocationId,
                new { Status = oldStatus }, new { location.Status });
            return (true, active ? "Đã kích hoạt Bin." : "Đã ngừng sử dụng Bin.");
        }

        public async Task<(bool Success, string Message)> ChangeRackStatusAsync(long rackId, bool active, long currentUserId = 0)
        {
            var rack = await _locationRepository.GetRackByIdAsync(rackId);
            if (rack == null)
                return (false, "Không tìm thấy Rack.");
            var warehouseValidation = await ValidateOperationalWarehouseAsync(rack.WarehouseId);
            if (warehouseValidation != null)
                return (false, warehouseValidation);
            if (rack.Status.Equals("BLOCKED", StringComparison.OrdinalIgnoreCase))
                return (false, "Rack đang bị khóa bởi nghiệp vụ kỹ thuật; không được đổi trạng thái tại màn cấu hình.");
            if (active == !IsInactive(rack.Status))
                return (true, active ? "Rack đã ở trạng thái hoạt động." : "Rack đã ngừng sử dụng.");

            var oldStatus = rack.Status;
            if (!active)
            {
                if (rack.StorageLocations.Any(location => !IsInactive(location.Status)))
                    return (false, "Chỉ được ngừng sử dụng Rack sau khi tất cả Bin con đã ngừng sử dụng.");
                rack.Status = "INACTIVE";
            }
            else
            {
                if (rack.WarehouseZone == null || !IsUsableNodeStatus(rack.WarehouseZone.Status))
                    return (false, "Hãy kích hoạt Zone cha trước khi kích hoạt Rack.");
                rack.Status = "ACTIVE";
            }

            await _locationRepository.UpdateRackAsync(rack);
            await RecordAuditAsync(currentUserId, AuditActions.ChangeStatus, AuditEntities.StorageRack, rack.RackId,
                new { Status = oldStatus }, new { rack.Status });
            return (true, active ? "Đã kích hoạt Rack." : "Đã ngừng sử dụng Rack.");
        }

        public async Task<(bool Success, string Message)> ChangeZoneStatusAsync(long zoneId, bool active, long currentUserId = 0)
        {
            var zone = await _locationRepository.GetZoneByIdAsync(zoneId);
            if (zone == null)
                return (false, "Không tìm thấy Zone.");
            var warehouseValidation = await ValidateOperationalWarehouseAsync(zone.WarehouseId);
            if (warehouseValidation != null)
                return (false, warehouseValidation);
            if (zone.Status.Equals("BLOCKED", StringComparison.OrdinalIgnoreCase))
                return (false, "Zone đang bị khóa bởi nghiệp vụ kỹ thuật; không được đổi trạng thái tại màn cấu hình.");
            if (active == !IsInactive(zone.Status))
                return (true, active ? "Zone đã ở trạng thái hoạt động." : "Zone đã ngừng sử dụng.");

            var oldStatus = zone.Status;
            if (!active)
            {
                if (zone.StorageRacks.Any(rack => !IsInactive(rack.Status)))
                    return (false, "Chỉ được ngừng sử dụng Zone sau khi tất cả Rack con đã ngừng sử dụng.");
                zone.Status = "INACTIVE";
            }
            else
            {
                zone.Status = "ACTIVE";
            }

            await _locationRepository.UpdateZoneAsync(zone);
            await RecordAuditAsync(currentUserId, AuditActions.ChangeStatus, AuditEntities.WarehouseZone, zone.ZoneId,
                new { Status = oldStatus }, new { zone.Status });
            return (true, active ? "Đã kích hoạt Zone." : "Đã ngừng sử dụng Zone.");
        }

        private static bool IsInactive(string? status) =>
            status?.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase) == true;

        private static bool IsUsableNodeStatus(string? status) =>
            !string.IsNullOrWhiteSpace(status) &&
            !status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase) &&
            !status.Equals("BLOCKED", StringComparison.OrdinalIgnoreCase);

        private static bool IsTechnicalZone(string? zoneCode) =>
            zoneCode?.Equals("ZONE-RECV", StringComparison.OrdinalIgnoreCase) == true ||
            zoneCode?.Equals("ZONE-DISP", StringComparison.OrdinalIgnoreCase) == true;

        private static string NaturalSortKey(string value) =>
            Regex.Replace(value ?? string.Empty, @"\d+", match => match.Value.PadLeft(12, '0'));

        private async Task<string?> ValidateOperationalWarehouseAsync(long warehouseId)
        {
            var warehouse = await _locationRepository.GetWarehouseByIdAsync(warehouseId);
            if (warehouse == null)
                return "Không tìm thấy kho hiện tại.";
            if (!warehouse.IsPrimary || !IsUsableNodeStatus(warehouse.Status))
                return "Chỉ được cấu hình vị trí cho kho chính đang hoạt động của hệ thống một kho.";
            return null;
        }

        private Task RecordAuditAsync(long userId, string action, string entity, long entityId, object? oldValue, object? newValue) =>
            _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = userId > 0 ? userId : null,
                ActionType = action,
                EntityName = entity,
                EntityId = entityId.ToString(),
                OldValues = oldValue,
                NewValues = newValue
            });

        private async Task<string?> ValidateScopeCapacityReductionAsync(
            IEnumerable<long> descendantLocationIds,
            string scopeType,
            long scopeId,
            decimal? oldMaxWeightKg,
            decimal? newMaxWeightKg,
            decimal? oldMaxVolumeM3,
            decimal? newMaxVolumeM3,
            string displayName)
        {
            var weightRequiresValidation = newMaxWeightKg.HasValue
                ? !oldMaxWeightKg.HasValue || newMaxWeightKg.Value < oldMaxWeightKg.Value
                : oldMaxWeightKg.HasValue;
            var volumeRequiresValidation = newMaxVolumeM3.HasValue
                ? !oldMaxVolumeM3.HasValue || newMaxVolumeM3.Value < oldMaxVolumeM3.Value
                : oldMaxVolumeM3.HasValue;
            if (!weightRequiresValidation && !volumeRequiresValidation)
                return null;

            var ids = descendantLocationIds.Distinct().ToList();
            if (ids.Count == 0)
                return null;

            var evaluations = await _capacityEvaluationService.EvaluateCurrentAsync(ids);
            var scope = evaluations.Values.SelectMany(item => item.Scopes)
                .FirstOrDefault(item => item.ScopeType == scopeType && item.ScopeId == scopeId);
            if (scope == null)
                return null;

            if (weightRequiresValidation)
            {
                if (scope.MissingWeightProductCodes.Count > 0)
                    return $"Không thể giảm tải trọng {displayName}: còn sản phẩm chưa cấu hình quy đổi khối lượng.";
                if (!newMaxWeightKg.HasValue && scope.CurrentWeightKg > 0)
                    return $"Không thể xóa giới hạn tải trọng {displayName} khi vẫn còn hàng.";
                if (newMaxWeightKg.HasValue && scope.CurrentWeightKg > newMaxWeightKg.Value)
                    return $"Tải trọng {displayName} mới nhỏ hơn mức đang sử dụng ({scope.CurrentWeightKg:0.###} kg).";
            }
            if (volumeRequiresValidation)
            {
                if (scope.MissingVolumeProductCodes.Count > 0)
                    return $"Không thể giảm thể tích {displayName}: còn sản phẩm chưa có hồ sơ thể tích lưu kho.";
                if (!newMaxVolumeM3.HasValue && scope.CurrentVolumeM3 > 0)
                    return $"Không thể xóa giới hạn thể tích {displayName} khi vẫn còn hàng.";
                if (newMaxVolumeM3.HasValue && scope.CurrentVolumeM3 > newMaxVolumeM3.Value)
                    return $"Thể tích {displayName} mới nhỏ hơn mức đang sử dụng ({scope.CurrentVolumeM3:0.###} m³).";
            }
            return null;
        }

        private static string? ValidatePhysicalLimits(
            decimal? areaSquareMeter,
            decimal? maxWeightKg,
            decimal? maxVolumeM3,
            string subject)
        {
            if (areaSquareMeter.HasValue && areaSquareMeter <= 0)
                return $"Diện tích {subject} phải lớn hơn 0 hoặc để trống.";
            if (maxWeightKg.HasValue && maxWeightKg <= 0)
                return $"Tải trọng tối đa của {subject} phải lớn hơn 0 hoặc để trống.";
            if (maxVolumeM3.HasValue && maxVolumeM3 <= 0)
                return $"Thể tích tối đa của {subject} phải lớn hơn 0 hoặc để trống.";
            return null;
        }

        private static string? ValidateZoneIdentity(CreateUpdateZoneDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ZoneCode) || string.IsNullOrWhiteSpace(dto.ZoneName))
                return "Mã Zone và tên Zone không được để trống.";
            if (dto.ZoneCode.Trim().Length > 50)
                return "Mã Zone không được vượt quá 50 ký tự.";
            if (dto.ZoneName.Trim().Length > 200)
                return "Tên Zone không được vượt quá 200 ký tự.";
            if (dto.Description?.Trim().Length > 500)
                return "Mô tả Zone không được vượt quá 500 ký tự.";
            return null;
        }

        private static string? ValidateRackIdentity(CreateUpdateRackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RackCode) || string.IsNullOrWhiteSpace(dto.RackName))
                return "Mã Rack và tên Rack không được để trống.";
            if (dto.RackCode.Trim().Length > 50)
                return "Mã Rack không được vượt quá 50 ký tự.";
            if (dto.RackName.Trim().Length > 200)
                return "Tên Rack không được vượt quá 200 ký tự.";
            return null;
        }

        private static string? ValidateParentAgainstExistingChildren(
            string parentName,
            decimal? parentArea,
            decimal? parentWeight,
            decimal? parentVolume,
            decimal allocatedArea,
            decimal allocatedWeight,
            decimal allocatedVolume)
        {
            if (parentArea.HasValue && allocatedArea > parentArea.Value)
                return $"Diện tích {parentName} không được nhỏ hơn tổng diện tích cấp con đang cấu hình ({allocatedArea} m²).";
            if (parentWeight.HasValue && allocatedWeight > parentWeight.Value)
                return $"Tải trọng {parentName} không được nhỏ hơn tổng tải trọng cấp con đang cấu hình ({allocatedWeight} kg).";
            if (parentVolume.HasValue && allocatedVolume > parentVolume.Value)
                return $"Thể tích {parentName} không được nhỏ hơn tổng thể tích cấp con đang cấu hình ({allocatedVolume} m³).";
            return null;
        }

        private static string? ValidateChildBudget(
            string childTotalName,
            string parentName,
            decimal? childArea,
            decimal? childWeight,
            decimal? childVolume,
            decimal siblingArea,
            decimal siblingWeight,
            decimal siblingVolume,
            decimal? parentArea,
            decimal? parentWeight,
            decimal? parentVolume)
        {
            if (parentArea.HasValue && siblingArea + (childArea ?? 0) > parentArea.Value)
                return $"{childTotalName} vượt diện tích tối đa của {parentName} ({parentArea.Value} m²).";
            if (parentWeight.HasValue && siblingWeight + (childWeight ?? 0) > parentWeight.Value)
                return $"{childTotalName} vượt tải trọng tối đa của {parentName} ({parentWeight.Value} kg).";
            if (parentVolume.HasValue && siblingVolume + (childVolume ?? 0) > parentVolume.Value)
                return $"{childTotalName} vượt thể tích tối đa của {parentName} ({parentVolume.Value} m³).";
            return null;
        }
    }
}
