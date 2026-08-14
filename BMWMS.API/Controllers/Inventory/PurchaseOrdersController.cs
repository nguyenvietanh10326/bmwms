using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _poService;

        public PurchaseOrdersController(IPurchaseOrderService poService)
        {
            _poService = poService;
        }

        /// <summary>
        /// 2. Lấy chi tiết 1 Purchase Order theo ID
        /// GET: api/PurchaseOrders/13
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var po = await _poService.GetOrderDetailAsync(id);
                if (po == null)
                {
                    return NotFound(new { message = $"Không tìm thấy đơn mua hàng với ID = {id}" });
                }

                return Ok(po);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy chi tiết đơn mua hàng.", detail = ex.Message });
            }
        }

        [HttpGet("AllSupplier")]
        public async Task<IActionResult> GetAllSupplier()
        {
            var users = await _poService.GetLookupListAsync();
            return Ok(users);
        }

        [HttpGet("AllWarehouse")]
        public async Task<IActionResult> GetAllWarehouse()
        {
            var wh = await _poService.GetLookListAsync();
            return Ok(wh);
        }

        [HttpGet("AllProduct")]
        public async Task<IActionResult> GetAllProduct()
        {
            var p = await _poService.GetUpListAsync();
            return Ok(p);
        }
    }
}
