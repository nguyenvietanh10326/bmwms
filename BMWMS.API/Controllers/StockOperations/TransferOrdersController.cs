using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;

namespace BMWMS.API.Controllers.StockOperations
{
    [ApiController]
    [Route("api/transfers")]
    [Authorize]
    public class TransferOrdersController : ControllerBase
    {
        private readonly ITransferOrderService _service;

        public TransferOrdersController(ITransferOrderService service)
        {
            _service = service;
        }

        private long CurrentUserId => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private bool IsManager => User.IsInRole("WAREHOUSE_MANAGER") || User.IsInRole("SYSTEM_ADMIN");

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? keyword,
            [FromQuery] string? status,
            [FromQuery] long? warehouseId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var staffId = IsManager ? (long?)null : CurrentUserId;
            return Ok(await _service.GetPagedOrdersAsync(keyword, status, warehouseId, (pageIndex > 0 ? pageIndex - 1 : 0), pageSize, staffId));
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetDetail(long id)
        {
            var detail = await _service.GetOrderDetailAsync(id, CurrentUserId, IsManager);
            if (detail == null) return NotFound("Phiếu không tồn tại.");
            return Ok(detail);
        }

        [HttpPost("create")]
        [Authorize(Roles = "WAREHOUSE_STAFF")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateTransferOrderDto dto)
        {
            try
            {
                var result = await _service.CreateOrderAsync(CurrentUserId, dto);
                if (!result.Success) return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "WAREHOUSE_STAFF")]
        public async Task<IActionResult> UpdateDraftOrder(long id, [FromBody] UpdateTransferOrderDto dto)
        {
            try
            {
                if (id != dto.TransferOrderId) return BadRequest("ID không khớp.");
                var result = await _service.UpdateDraftOrderAsync(CurrentUserId, dto);
                if (!result.Success) return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}


