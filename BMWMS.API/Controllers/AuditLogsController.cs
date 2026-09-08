using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "SYSTEM_ADMIN")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
    {
        try
        {
            return Ok(await _auditLogService.GetAuditLogsAsync(filter));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetAuditLog(long id)
    {
        var result = await _auditLogService.GetAuditLogAsync(id);
        return result is null
            ? NotFound(new { message = "Không tìm thấy nhật ký hoạt động." })
            : Ok(result);
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions([FromQuery] string? entityName = null)
    {
        return Ok(await _auditLogService.GetOptionsAsync(entityName));
    }
}
