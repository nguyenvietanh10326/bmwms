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
        private bool IsManager => User.IsInRole("WAREHOUSE_MANAGER");

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "WAREHOUSE_MANAGER")]
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

        // Thay cho "reject"
        [HttpPost("{id}/cancel")]
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

        [HttpPost("{id}/confirm")]
        [Authorize(Roles = "WAREHOUSE_STAFF")]
        public async Task<IActionResult> ConfirmTransfer(long id, [FromBody] ConfirmTransferDto dto)
        {
            try
            {
                var result = await _confirmService.ConfirmAsync(CurrentUserId, id, dto);
                if (!result.Success) return BadRequest(result.Message); // Nếu bị tràn sức chứa nhưng chưa acknowledge
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Tạm thời để trống các API cũ để Web không gọi bị lỗi 404 ngay lập tức
        // Dù Web đã đổi nhưng đề phòng cache
        [HttpPost("{id}/issue")]
        public IActionResult LegacyIssue() => BadRequest("Luồng xuất đã bị loại bỏ. Vui lòng dùng tính năng Xác Nhận 1 bước.");

        [HttpPost("{id}/receive")]
        public IActionResult LegacyReceive() => BadRequest("Luồng nhập đã bị loại bỏ. Vui lòng dùng tính năng Xác Nhận 1 bước.");

        [HttpPost("{id}/reject")]
        public IActionResult LegacyReject() => BadRequest("Luồng từ chối đã được thay thế bằng Hủy (Cancel).");
    }

    public class CancelTransferDto
    {
        public string? Notes { get; set; }
    }
}
