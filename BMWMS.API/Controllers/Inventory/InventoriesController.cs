using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers.Inventory
{
    public class InventoriesController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoriesController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Lấy dữ liệu danh sách tồn kho + 4 card tổng quan + bộ lọc + phân trang
        /// GET: api/inventories?keyword=XM001&warehouseId=1&pageIndex=1&pageSize=10
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetInventoryPage([FromQuery] InventoryFilterDto filter)
        {
            var result = await _inventoryService.GetInventoryPageDataAsync(filter);
            return Ok(result);
        }
    }
}
