using BMWMS.Business.DTOs.Audit;

namespace BMWMS.Business.Interfaces;

public interface IAuditLogService
{
    Task RecordAsync(AuditEventDto auditEvent);
    Task StageAsync(AuditEventDto auditEvent);
    Task<AuditLogListResponseDto> GetAuditLogsAsync(AuditLogFilterDto filter);
    Task<AuditLogDetailDto?> GetAuditLogAsync(long auditLogId);
    Task<AuditLogOptionsDto> GetOptionsAsync();
}
