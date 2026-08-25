using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _poService;

        public PurchaseOrdersController(IPurchaseOrderService poService)
        {
            _poService = poService;
        }

        /// <summary>
        /// 2. Láº¥y chi tiáº¿t 1 Purchase Order theo ID
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
                    return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n mua hÃ ng vá»›i ID = {id}" });
                }

                return Ok(po);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i há»‡ thá»‘ng khi láº¥y chi tiáº¿t Ä‘Æ¡n mua hÃ ng.", detail = ex.Message });
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
        [Authorize(Roles = "PURCHASING_STAFF")]
        public async Task<IActionResult> Create([FromBody] BMWMS.Business.DTOs.Inventory.PurchaseOrderCreateDto request)
        {
            try
            {
                // Get userId from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                    return Unauthorized(new { message = "Token không hợp lệ." });

                var result = await _poService.CreatePurchaseOrderAsync(request, userId);
                if (result.Success)
                {
                    return Ok(new { message = "Táº¡o lá»‡nh mua hÃ ng thÃ nh cÃ´ng.", data = result.Message });
                }
                else
                {
                    return BadRequest(new { message = result.Message });
                }
            }
            catch (Exception ex)
            {
                var innerMsg = ex.ToString();
                return StatusCode(500, new { message = "Lá»—i há»‡ thá»‘ng khi táº¡o Lá»‡nh mua hÃ ng.", detail = innerMsg });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] BMWMS.Business.DTOs.Inventory.PurchaseOrderFilterDto filter)
        {
            var result = await _poService.GetPagedOrdersAsync(filter);
            return Ok(result);
        }

        [HttpPost("{id}/confirm")]
        [Authorize(Roles = "PURCHASING_STAFF")]
        public async Task<IActionResult> Confirm(long id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                    return Unauthorized(new { message = "Token không hợp lệ." });
                var result = await _poService.ConfirmOrderAsync(id, userId);
                if (result.Success) return Ok(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i há»‡ thá»‘ng.", detail = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "PURCHASING_STAFF")]
        public async Task<IActionResult> Cancel(long id, [FromQuery] string? reason)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                    return Unauthorized(new { message = "Token không hợp lệ." });
                var result = await _poService.CancelOrderAsync(id, userId, reason);
                if (result.Success) return Ok(new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i há»‡ thá»‘ng.", detail = ex.Message });
            }
        }
        [HttpGet("/AllCustomers")]
        public async Task<IActionResult> GetAllCustomer()
        {
            try
            {
                var result = await _poService.GetCustomersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khách hàng.", detail = ex.Message });
            }
        }
    }
}
