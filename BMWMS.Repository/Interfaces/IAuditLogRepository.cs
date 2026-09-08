using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, bool saveChanges = true);

    Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        IReadOnlyCollection<string>? moduleEntityNames,
        string? actionType,
        string? entityName,
        string? entityId,
        long? userId,
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        int pageIndex,
        int pageSize);

    Task<AuditLog?> GetByIdAsync(long auditLogId);
    Task<List<string>> GetActionTypesAsync(string? entityName = null);
    Task<List<string>> GetEntityNamesAsync();
    Task<List<User>> GetActorsAsync();
}
