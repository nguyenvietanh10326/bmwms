using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;

namespace BMWMS.API.Controllers.StockOperations
{
    [ApiController]
    [Route("api/transfers")]
    [Authorize(Roles = "WAREHOUSE_MANAGER,SYSTEM_ADMIN,WAREHOUSE_STAFF")]
    public class TransferActionsController : ControllerBase
    {
        private readonly ITransferApprovalService _approvalService;
        private readonly ITransferConfirmService _confirmService;

        public TransferActionsController(ITransferApprovalService approvalService, ITransferConfirmService confirmService)
        {
            _approvalService = approvalService;
            _confirmService = confirmService;
        }

        private long CurrentUserId => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private bool IsManager => User.IsInRole("WAREHOUSE_MANAGER") || User.IsInRole("SYSTEM_ADMIN");

        [HttpPost("{id:long}/approve")]
        [Authorize(Roles = "WAREHOUSE_MANAGER,SYSTEM_ADMIN")]
        public async Task<IActionResult> ApproveTransfer(long id, [FromBody] ApproveTransferDto dto)
        {
            try
            {
                if (id != dto.TransferOrderId) return BadRequest("ID không khớp.");
                var result = await _approvalService.ApproveTransferAsync(CurrentUserId, dto);
                if (!result.Success) return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> CancelTransfer(long id, [FromBody] CancelTransferDto dto)
        {
            try
            {
                var result = await _approvalService.CancelTransferAsync(CurrentUserId, id, dto.Notes, IsManager);
                if (!result.Success) return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:long}/confirm")]
        [Authorize(Roles = "WAREHOUSE_STAFF")]
        public async Task<IActionResult> ConfirmTransfer(long id, [FromBody] ConfirmTransferDto dto)
        {
            try
            {
                var result = await _confirmService.ConfirmAsync(CurrentUserId, id, dto);
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

