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
            var warehouseDto = new WarehouseHeaderInfoDto
            {
                WarehouseId = warehouse?.WarehouseId ?? filter.WarehouseId,
                WarehouseCode = warehouse?.WarehouseCode ?? "N/A",
                WarehouseName = warehouse?.WarehouseName ?? "N/A",
                Address = warehouse?.Address ?? "Chưa cập nhật địa chỉ"
            };

            // Lấy danh sách từ Repo (Đã include StorageRack -> WarehouseZone)
            var (rawItems, totalCount) = await _locationRepository.GetPagedLocationsAsync(
                filter.WarehouseId,
                filter.Keyword,
                filter.LocationType,
                filter.Status,
                filter.PageIndex,
                filter.PageSize);

            // Map từ Entity sang DTO
            var items = rawItems.Select(l => new StorageLocationItemDto
            {
                StorageLocationId = l.StorageLocationId,
                WarehouseId = l.WarehouseId,
                RackId = l.RackId,
                LocationCode = l.LocationCode,
                LocationName = l.LocationName ?? "N/A",
                LocationType = l.LocationType,

                // Trích xuất mã Zone và Rack qua chuỗi liên kết navigation properties
                ZoneCode = l.StorageRack?.WarehouseZone?.ZoneCode ?? "N/A",
                RackCode = l.StorageRack?.RackCode ?? "N/A",

                Floor = 1, // Tuỳ chỉnh logic nếu có tầng
                AreaSquareMeter = l.AreaSquareMeter,
                MaxWeightKg = l.MaxWeightKg,
                MaxVolumeM3 = l.MaxVolumeM3,

                IsPutawayAllowed = l.IsPutawayAllowed,
                IsPickable = l.IsPickable,
                Status = l.Status
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

            return new StorageLocationItemDto
            {
                StorageLocationId = item.StorageLocationId,
                WarehouseId = item.WarehouseId,
                RackId = item.RackId,
                LocationCode = item.LocationCode,
                LocationName = item.LocationName ?? "",
                LocationType = item.LocationType,
                ZoneCode = item.StorageRack?.WarehouseZone?.ZoneCode ?? "N/A",
                RackCode = item.StorageRack?.RackCode ?? "N/A",
                Floor = 1,
                AreaSquareMeter = item.AreaSquareMeter,
                MaxWeightKg = item.MaxWeightKg,
                MaxVolumeM3 = item.MaxVolumeM3,
                IsPutawayAllowed = item.IsPutawayAllowed,
                IsPickable = item.IsPickable,
                Status = item.Status
            };
        }

        public async Task<(bool Success, string Message)> CreateLocationAsync(CreateUpdateStorageLocationDto dto)
        {
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
            var entity = await _locationRepository.GetByIdAsync(dto.StorageLocationId);
            if (entity == null)
            {
                return (false, "Không tìm thấy vị trí lưu kho.");
            }

            var isDuplicate = await _locationRepository.ExistsCodeAsync(dto.WarehouseId, dto.LocationCode, dto.StorageLocationId);
            if (isDuplicate)
            {
                return (false, $"Mã vị trí '{dto.LocationCode}' đã tồn tại trong kho này.");
            }

            entity.RackId = dto.RackId;
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
    }
}
