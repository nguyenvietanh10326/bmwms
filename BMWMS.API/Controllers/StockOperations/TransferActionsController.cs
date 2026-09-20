using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using BMWMS.Business.Common;

namespace BMWMS.API.Controllers.StockOperations
{
    [ApiController]
    [Route("api/transfers")]
    [Authorize(Roles = "WAREHOUSE_MANAGER,SYSTEM_ADMIN,WAREHOUSE_STAFF")]
    public class TransferActionsController : ControllerBase
    {
        private readonly ITransferCancellationService _cancellationService;
        private readonly ITransferConfirmService _confirmService;

        public TransferActionsController(ITransferCancellationService cancellationService, ITransferConfirmService confirmService)
        {
            _cancellationService = cancellationService;
            _confirmService = confirmService;
        }

        private long CurrentUserId => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private bool IsManager => User.IsInRole("WAREHOUSE_MANAGER") || User.IsInRole("SYSTEM_ADMIN");

        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> CancelTransfer(long id, [FromBody] CancelTransferDto dto)
        {
            try
            {
                var result = await _cancellationService.CancelTransferAsync(CurrentUserId, id, dto.Notes, IsManager);
                if (!result.Success) return BadRequest(result.Message);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(StocktakeLocationLockError.Is(ex)
                    ? new { code = StocktakeLocationLockError.Code, message = StocktakeLocationLockError.Message }
                    : new { code = "TRANSFER_ERROR", message = ex.Message });
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

