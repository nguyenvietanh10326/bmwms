using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Services.Inventory
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;

        public InventoryService(IInventoryRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<InventoryDashboardPageDto> GetInventoryPageDataAsync(InventoryFilterDto filter)
        {
            var (totalOnHand, totalAvailable, totalReserved, totalInTransit) =
                await _inventoryRepository.GetInventorySummaryMetricsAsync(
                    filter.Keyword,
                    filter.WarehouseId,
                    filter.StorageLocationId,
                    filter.Status,
                    filter.ZoneId,
                    filter.RackId);

            var (rawItems, totalCount) = await _inventoryRepository.GetPagedInventoryAsync(
                filter.Keyword,
                filter.WarehouseId,
                filter.StorageLocationId,
                filter.Status,
                filter.ZoneId, filter.RackId,
                filter.PageIndex,
                filter.PageSize);

            var items = rawItems.Select(item =>
            {
                var available = item.AvailableQuantity ?? (item.OnHandQuantity - item.ReservedQuantity);

                // Lấy ngưỡng MinimumStockQuantity từ ProductWarehousePolicy nếu có
                var warehouseId = item.StorageLocation?.WarehouseId;
                var policy = item.Product?.ProductWarehousePolicies
                    ?.FirstOrDefault(p => warehouseId.HasValue && p.WarehouseId == warehouseId.Value);
                decimal minStockQty = policy?.MinimumStockQuantity ?? 0m;

                string statusText;
                string cssClass;

                if (available <= 0)
                {
                    statusText = "Hết hàng";
                    cssClass = "status-danger";
                }
                else if (minStockQty > 0 && available < minStockQty)
                {
                    statusText = "Sắp hết";
                    cssClass = "status-warning";
                }
                else
                {
                    statusText = "Bình thường";
                    cssClass = "status-normal";
                }

                return new InventoryListItemDto
                {
                    InventoryId = item.InventoryId,
                    ProductId = item.ProductId,
                    ProductCode = item.Product?.ProductCode ?? "N/A",
                    ProductName = item.Product?.ProductName ?? "N/A",
                    UnitName = item.Product?.UnitOfMeasure?.UnitName ?? "",
                    ProductGroupName = item.Product?.ProductGroup?.GroupName ?? "",

                    WarehouseName = item.StorageLocation?.Warehouse?.WarehouseName ?? "N/A",
                    LocationCode = item.StorageLocation?.LocationCode ?? "N/A",

                    OnHandQuantity = item.OnHandQuantity,
                    ReservedQuantity = item.ReservedQuantity,
                    AvailableQuantity = available,
                    MinimumStockQuantity = minStockQty,

                    LotNumber = item.ProductLot?.LotNumber ?? "N/A",
                    ExpiryDate = item.ProductLot?.ExpiryDate,

                    Status = statusText,
                    StatusCssClass = cssClass
                };
            }).ToList();

            return new InventoryDashboardPageDto
            {
                TotalOnHand = totalOnHand,
                TotalAvailable = totalAvailable,
                TotalReserved = totalReserved,
                TotalInTransit = totalInTransit,
                Items = items,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }
    }
}


