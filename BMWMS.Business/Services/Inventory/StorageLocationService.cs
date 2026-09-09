using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Services.Inventory
{
    public class StorageLocationService : IStorageLocationService
    {
        private readonly IStorageLocationRepository _locationRepository;

        public StorageLocationService(IStorageLocationRepository locationRepository)
        {
            _locationRepository = locationRepository;
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
                    LocationName = l.LocationName ?? "N/A",
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

            return new StorageLocationItemDto
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
        }

        public async Task<(bool Success, string Message)> CreateLocationAsync(CreateUpdateStorageLocationDto dto)
        {
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
                RackId = dto.RackId > 0 ? dto.RackId : null,
                LocationCode = dto.LocationCode.Trim().ToUpper(),
                LocationName = dto.LocationName?.Trim(),
                LocationType = dto.LocationType,
                AreaSquareMeter = dto.AreaSquareMeter,
                MaxWeightKg = dto.MaxWeightKg,
                MaxVolumeM3 = dto.MaxVolumeM3,
                IsPutawayAllowed = dto.IsPutawayAllowed,
                IsPickable = dto.IsPickable,
                Status = dto.Status ?? "Active",
                CreatedAt = DateTime.Now
            };

            await _locationRepository.AddAsync(entity);
            return (true, "Thêm mới vị trí thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateLocationAsync(CreateUpdateStorageLocationDto dto)
        {
            var capacityValidation = ValidatePhysicalLimits(dto);
            if (capacityValidation != null)
                return (false, capacityValidation);

            var entity = await _locationRepository.GetByIdAsync(dto.StorageLocationId);
            if (entity == null)
            {
                return (false, "Không tìm thấy vị trí lưu kho.");
            }
            var hierarchyValidation = await ValidateLocationHierarchyAsync(dto, dto.StorageLocationId);
            if (hierarchyValidation != null)
                return (false, hierarchyValidation);

            var isDuplicate = await _locationRepository.ExistsCodeAsync(dto.WarehouseId, dto.LocationCode, dto.StorageLocationId);
            if (isDuplicate)
            {
                return (false, $"Mã vị trí '{dto.LocationCode}' đã tồn tại trong kho này.");
            }

            entity.RackId = dto.RackId > 0 ? dto.RackId : null;
            entity.LocationCode = dto.LocationCode.Trim().ToUpper();
            entity.LocationName = dto.LocationName?.Trim();
            entity.LocationType = dto.LocationType;
            entity.AreaSquareMeter = dto.AreaSquareMeter;
            entity.MaxWeightKg = dto.MaxWeightKg;
            entity.MaxVolumeM3 = dto.MaxVolumeM3;
            entity.IsPutawayAllowed = dto.IsPutawayAllowed;
            entity.IsPickable = dto.IsPickable;
            entity.Status = dto.Status;
            entity.UpdatedAt = DateTime.Now;

            await _locationRepository.UpdateAsync(entity);
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

        private async Task<string?> ValidateLocationHierarchyAsync(
            CreateUpdateStorageLocationDto dto,
            long? excludeLocationId = null)
        {
            if (!dto.RackId.HasValue || dto.RackId <= 0)
                return dto.LocationType.Equals("BIN", StringComparison.OrdinalIgnoreCase)
                    ? "Vị trí loại Bin phải thuộc một Rack để kiểm soát sức chứa phân cấp."
                    : null;

            var rack = await _locationRepository.GetRackByIdAsync(dto.RackId.Value);
            if (rack == null || rack.WarehouseId != dto.WarehouseId)
                return "Rack đã chọn không thuộc kho hiện tại.";

            var siblings = rack.StorageLocations
                .Where(location => !excludeLocationId.HasValue || location.StorageLocationId != excludeLocationId.Value)
                .ToList();
            return ValidateChildBudget(
                "Tổng giới hạn các Bin",
                $"Rack {rack.RackCode}",
                dto.AreaSquareMeter,
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
            return zones.Select(z => new WarehouseZoneDto
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
            return racks.Select(r => new StorageRackDto
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
            var zones = await _locationRepository.GetZonesByWarehouseAsync(warehouseId);
            var allLocations = await _locationRepository.GetAllLocationsWithHierarchyAndInventoryAsync(warehouseId);

            var mappedLocations = allLocations.Select(l =>
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
                    LocationName = l.LocationName ?? "N/A",
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

            var locationByRack = mappedLocations
                .Where(l => l.RackId.HasValue)
                .GroupBy(l => l.RackId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var unassignedLocations = mappedLocations
                .Where(l => !l.RackId.HasValue)
                .ToList();

            var zoneStructures = zones.Select(z => new WarehouseZoneStructureDto
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
                Racks = z.StorageRacks.Select(r => new StorageRackStructureDto
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
                    Locations = locationByRack.TryGetValue(r.RackId, out var locs) ? locs : new List<StorageLocationItemDto>()
                }).ToList()
            }).ToList();

            int available = mappedLocations.Count(l => l.OccupancyStatus == "Available");
            int occupied = mappedLocations.Count(l => l.OccupancyStatus == "Occupied");
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
                UnassignedLocations = unassignedLocations,
                TotalAvailableBins = available,
                TotalOccupiedBins = occupied,
                TotalBlockedBins = blocked
            };
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
                UnitOfMeasure = inv.Product.UnitOfMeasure?.UnitName ?? inv.Product.UnitOfMeasure?.UnitCode ?? "N/A",
                ProductGroupName = inv.Product.ProductGroup?.GroupName ?? "N/A",
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

        public async Task<(bool Success, string Message)> CreateZoneAsync(CreateUpdateZoneDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ZoneCode) || string.IsNullOrWhiteSpace(dto.ZoneName))
            {
                return (false, "Mã khu vực và tên khu vực không được để trống.");
            }
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
                AreaSquareMeter = dto.AreaSquareMeter,
                MaxWeightKg = dto.MaxWeightKg,
                MaxVolumeM3 = dto.MaxVolumeM3,
                Status = dto.Status ?? "Active"
            };

            await _locationRepository.AddZoneAsync(zone);
            return (true, "Thêm mới khu vực (Zone) thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateZoneAsync(CreateUpdateZoneDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ZoneCode) || string.IsNullOrWhiteSpace(dto.ZoneName))
                return (false, "Mã khu vực và tên khu vực không được để trống.");
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

            zone.ZoneCode = dto.ZoneCode.Trim().ToUpperInvariant();
            zone.ZoneName = dto.ZoneName.Trim();
            zone.Description = dto.Description?.Trim();
            zone.AreaSquareMeter = dto.AreaSquareMeter;
            zone.MaxWeightKg = dto.MaxWeightKg;
            zone.MaxVolumeM3 = dto.MaxVolumeM3;
            zone.Status = dto.Status ?? "Active";
            await _locationRepository.UpdateZoneAsync(zone);
            return (true, "Cập nhật khu vực và sức chứa thành công.");
        }

        public async Task<(bool Success, string Message)> CreateRackAsync(CreateUpdateRackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RackCode) || string.IsNullOrWhiteSpace(dto.RackName))
            {
                return (false, "Mã kệ và tên kệ không được để trống.");
            }

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
                AreaSquareMeter = dto.AreaSquareMeter,
                MaxWeightKg = dto.MaxWeightKg,
                MaxVolumeM3 = dto.MaxVolumeM3,
                Status = dto.Status ?? "Active"
            };

            await _locationRepository.AddRackAsync(rack);
            return (true, "Thêm mới kệ chứa (Rack) thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateRackAsync(CreateUpdateRackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RackCode) || string.IsNullOrWhiteSpace(dto.RackName))
                return (false, "Mã kệ và tên kệ không được để trống.");
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

            var zone = await _locationRepository.GetZoneByIdAsync(dto.ZoneId);
            if (zone == null || zone.WarehouseId != dto.WarehouseId)
                return (false, "Khu vực đã chọn không thuộc kho hiện tại.");
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

            rack.ZoneId = dto.ZoneId;
            rack.RackCode = dto.RackCode.Trim().ToUpperInvariant();
            rack.RackName = dto.RackName.Trim();
            rack.AreaSquareMeter = dto.AreaSquareMeter;
            rack.MaxWeightKg = dto.MaxWeightKg;
            rack.MaxVolumeM3 = dto.MaxVolumeM3;
            rack.Status = dto.Status ?? "Active";
            await _locationRepository.UpdateRackAsync(rack);
            return (true, "Cập nhật Rack và sức chứa thành công.");
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
