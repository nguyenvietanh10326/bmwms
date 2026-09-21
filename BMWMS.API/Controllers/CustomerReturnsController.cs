using System.Security.Claims;
using BMWMS.Business.DTOs.Inbound;
using BMWMS.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[ApiController, Route("api/customer-returns")]
[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,WAREHOUSE_STAFF")]
public class CustomerReturnsController(CustomerReturnRequestService service, ILogger<CustomerReturnsController> logger) : ControllerBase
{
    private long CurrentUserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new UnauthorizedAccessException();
    [HttpGet] public async Task<IActionResult> List()
    {
        var rows = await service.GetListAsync();
        return Ok(User.IsInRole("WAREHOUSE_STAFF")
            ? rows.Where(r => r.Status is "APPROVED" or "PARTIALLY_RECEIVED" or "PENDING_REMAINDER_REVIEW" or "COMPLETED")
            : rows);
    }
    [HttpGet("{id:long}")] public async Task<IActionResult> Get(long id) => await service.GetAsync(id) is { } r ? Ok(r) : NotFound();
    [HttpGet("sales-orders")]
    [Authorize(Roles = "SALES_STAFF,SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<IActionResult> SalesOrders() => Ok(await service.GetSalesOrdersAsync());
    [HttpGet("sales-orders/{salesOrderId:long}/sources")]
    [Authorize(Roles = "SALES_STAFF,SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public Task<IActionResult> Sources(long salesOrderId, [FromQuery] long? editingRequestId) => Execute(async () =>
        Ok(await service.GetSalesOrderSourcesAsync(salesOrderId, editingRequestId, CurrentUserId)));
    [HttpPost, Authorize(Roles = "SALES_STAFF,SYSTEM_ADMIN")]
    public Task<IActionResult> Create(CreateCustomerReturnRequestDto dto) => Execute(async () => Ok(await service.CreateAsync(dto, CurrentUserId)));
    [HttpPut("{id:long}"), Authorize(Roles = "SALES_STAFF,SYSTEM_ADMIN")]
    public Task<IActionResult> Update(long id, CreateCustomerReturnRequestDto dto) => Execute(async () => { await service.UpdateAsync(id, dto, CurrentUserId); return Ok(new { message = "Đã sửa phiếu trả theo SO, chờ Manager duyệt." }); });
    [HttpPost("{id:long}/decision"), Authorize(Roles = "SALES_STAFF,WAREHOUSE_MANAGER,SYSTEM_ADMIN")]
    public Task<IActionResult> Decide(long id, CustomerReturnDecisionDto dto) => Execute(async () => { await service.DecideAsync(id, dto, CurrentUserId); return Ok(new { message = "Đã cập nhật yêu cầu trả hàng." }); });
    [HttpPost("{id:long}/receipts"), Authorize(Roles = "WAREHOUSE_STAFF")]
    public Task<IActionResult> Receipt(long id, [FromQuery] DateOnly receiptDate) => Execute(async () => Ok(await service.CreateReceiptAsync(id, receiptDate, CurrentUserId)));

    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
        catch (Exception e) { logger.LogError(e, "Customer return workflow failed"); return StatusCode(500, new { message = "Không thể lưu yêu cầu trả hàng. Vui lòng tải lại và kiểm tra trạng thái; quản trị viên cần kiểm tra schema nếu lỗi lặp lại." }); }
    }
}
