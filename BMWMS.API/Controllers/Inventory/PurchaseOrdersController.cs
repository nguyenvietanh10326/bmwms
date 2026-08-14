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
    
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BMWMS.Business.DTOs.Inventory.PurchaseOrderCreateDto request)
        {
            try
            {
                // In a real app, we would get the UserId from claims.
                // For now, we hardcode 1 or get from headers if provided.
                long userId = 1;
                if (Request.Headers.TryGetValue("X-User-Id", out var userIdStr) && long.TryParse(userIdStr, out var uid))
                {
                    userId = uid;
                }

                var result = await _poService.CreatePurchaseOrderAsync(request, userId);
                if (result.Success)
                {
                    return Ok(new { message = "Tạo lệnh mua hàng thành công.", data = result.Message });
                }
                else
                {
                    return BadRequest(new { message = result.Message });
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.ToString();
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo Lệnh mua hàng.", detail = innerMsg });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] BMWMS.Business.DTOs.Inventory.PurchaseOrderFilterDto filter)
        {
            var result = await _poService.GetPagedOrdersAsync(filter);
            return Ok(result);
        }

        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> Confirm(long id)
        {
            try
            {
                long userId = 1; // Demo
                var result = await _poService.ConfirmOrderAsync(id, userId);
                if (result.Success) return Ok(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(long id, [FromQuery] string? reason)
        {
            try
            {
                long userId = 1; // Demo
                var result = await _poService.CancelOrderAsync(id, userId, reason);
                if (result.Success) return Ok(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }
    }
}
