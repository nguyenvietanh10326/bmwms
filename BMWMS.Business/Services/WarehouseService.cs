using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Warehouse;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;

namespace BMWMS.Business.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _warehouseRepository;

        public WarehouseService(IWarehouseRepository warehouseRepository)
        {
            _warehouseRepository = warehouseRepository;
        }

        public async Task<WarehouseResponseDto?> GetByIdAsync(long id)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id);
            if (warehouse == null) return null;

            return MapToResponseDto(warehouse);
        }

        public async Task<PagedResultDto<WarehouseResponseDto>> GetPagedListAsync(WarehouseFilterDto filter)
        {
            // Validate phân trang cơ bản
            int pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            int pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var (items, totalCount) = await _warehouseRepository.GetPagedListAsync(
                filter.Keyword,
                filter.Status,
                filter.IsPrimary,
                pageIndex,
                pageSize
            );

            var dtos = items.Select(MapToResponseDto).ToList();

            return new PagedResultDto<WarehouseResponseDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<long> CreateAsync(CreateWarehouseDto dto)
        {
            // 1. Validate mã kho trùng lặp
            string cleanCode = dto.WarehouseCode.Trim().ToUpper();
            if (await _warehouseRepository.IsCodeExistsAsync(cleanCode))
            {
                throw new InvalidOperationException($"Mã kho '{cleanCode}' đã tồn tại trong hệ thống.");
            }

            // 2. Logic kho chính
            // Nếu đây là kho đầu tiên tạo ra, tự động đặt làm kho chính
            bool isPrimary = dto.IsPrimary;
            bool hasAnyPrimary = await _warehouseRepository.HasPrimaryWarehouseAsync();
            if (!hasAnyPrimary)
            {
                isPrimary = true;
            }
            else if (isPrimary)
            {
                await UnsetExistingPrimaryWarehouseAsync();
            }

            // 3. Map & Lưu
            var warehouse = new Warehouse
            {
                WarehouseCode = cleanCode,
                WarehouseName = dto.WarehouseName.Trim(),
                Address = dto.Address.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                IsPrimary = isPrimary,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            return await _warehouseRepository.AddAsync(warehouse);
        }

        public async Task UpdateAsync(long id, UpdateWarehouseDto dto)
        {
            // 1. Kiểm tra kho có tồn tại không
            var existingWarehouse = await _warehouseRepository.GetByIdAsync(id);
            if (existingWarehouse == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy kho với ID = {id}.");
            }

            // 2. Validate trùng mã kho (loại trừ chính nó)
            string cleanCode = dto.WarehouseCode.Trim().ToUpper();
            if (await _warehouseRepository.IsCodeExistsAsync(cleanCode, id))
            {
                throw new InvalidOperationException($"Mã kho '{cleanCode}' đã được sử dụng bởi kho khác.");
            }

            // 3. Logic kho chính & Trạng thái
            if (existingWarehouse.IsPrimary && !dto.IsPrimary)
            {
                throw new InvalidOperationException("Hệ thống phải có ít nhất 1 kho chính. Vui lòng thiết lập kho khác làm kho chính trước.");
            }

            if (existingWarehouse.IsPrimary && dto.Status == "INACTIVE")
            {
                throw new InvalidOperationException("Không thể chuyển kho chính sang trạng thái ngưng hoạt động (INACTIVE).");
            }

            if (!existingWarehouse.IsPrimary && dto.IsPrimary)
            {
                await UnsetExistingPrimaryWarehouseAsync(excludeWarehouseId: id);
            }

            existingWarehouse.WarehouseCode = cleanCode;
            existingWarehouse.WarehouseName = dto.WarehouseName.Trim();
            existingWarehouse.Address = dto.Address.Trim();
            existingWarehouse.PhoneNumber = dto.PhoneNumber?.Trim();
            existingWarehouse.IsPrimary = dto.IsPrimary;
            existingWarehouse.Status = dto.Status;
            existingWarehouse.UpdatedAt = DateTime.UtcNow;

            await _warehouseRepository.UpdateAsync(existingWarehouse);
        }

        public async Task DeleteAsync(long id)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id);
            if (warehouse == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy kho với ID = {id}.");
            }

            // Validate logic xóa
            if (warehouse.IsPrimary)
            {
                throw new InvalidOperationException("Không thể xóa kho chính của hệ thống. Hãy chuyển vai trò kho chính sang kho khác trước khi xóa.");
            }

            // TODO: Bổ sung thêm validate nếu Kho đã phát sinh giao dịch nhập/xuất kho (Stock/Transactions) trong tương lai.

            await _warehouseRepository.DeleteAsync(id);
        }

        #region Helper Methods
        private async Task UnsetExistingPrimaryWarehouseAsync(long? excludeWarehouseId = null)
        {
            // Tìm kho chính hiện tại và chuyển IsPrimary về false
            var (items, _) = await _warehouseRepository.GetPagedListAsync(null, null, isPrimary: true, 1, 10);
            foreach (var primaryWh in items)
            {
                if (excludeWarehouseId.HasValue && primaryWh.WarehouseId == excludeWarehouseId.Value)
                    continue;

                primaryWh.IsPrimary = false;
                primaryWh.UpdatedAt = DateTime.UtcNow;
                await _warehouseRepository.UpdateAsync(primaryWh);
            }
        }

        private WarehouseResponseDto MapToResponseDto(Warehouse entity)
        {
            return new WarehouseResponseDto
            {
                WarehouseID = entity.WarehouseId,
                WarehouseCode = entity.WarehouseCode,
                WarehouseName = entity.WarehouseName,
                Address = entity.Address,
                PhoneNumber = entity.PhoneNumber,
                IsPrimary = entity.IsPrimary,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                TotalLocations = entity.StorageLocations?.Count ?? 0,
                ManagerName = string.IsNullOrEmpty(entity.PhoneNumber) ? "N/A" : $"WAREHOUSE MANAGER",
                StorageLocations = entity.StorageLocations?.Select(loc => new StorageLocationItemDto
                {
                    LocationID = loc.StorageLocationId,
                    LocationCode = loc.LocationCode,
                    LocationType = string.IsNullOrWhiteSpace(loc.LocationType) ? "Kệ chung" : loc.LocationType,
                    AreaSquareMeter = loc.AreaSquareMeter,
                    Status = loc.Status ?? "Empty",
                }).ToList() ?? new List<StorageLocationItemDto>()
            };
        }
            #endregion
        }
    }
