using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly BmwmsContext _context;

    public AuditLogRepository(BmwmsContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog, bool saveChanges = true)
    {
        await _context.AuditLogs.AddAsync(auditLog);
        if (saveChanges) await _context.SaveChangesAsync();
    }

    public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        IReadOnlyCollection<string>? moduleEntityNames,
        string? actionType,
        string? entityName,
        string? entityId,
        long? userId,
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        int pageIndex,
        int pageSize)
    {
        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(x =>
                x.ActionType.Contains(term) ||
                x.EntityName.Contains(term) ||
                (x.EntityId != null && x.EntityId.Contains(term)) ||
                (x.IpAddress != null && x.IpAddress.Contains(term)) ||
                (x.User != null && (x.User.Username.Contains(term) || x.User.FullName.Contains(term))));
        }

        if (moduleEntityNames is not null)
        {
            if (moduleEntityNames.Count == 0) return (new List<AuditLog>(), 0);
            query = query.Where(x => moduleEntityNames.Contains(x.EntityName));
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(x => x.ActionType == actionType);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(x => x.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(x => x.EntityId == entityId);
        }

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromUtc.Value);
        }

        if (toUtcExclusive.HasValue)
        {
            query = query.Where(x => x.CreatedAt < toUtcExclusive.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.AuditLogId)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<AuditLog?> GetByIdAsync(long auditLogId)
    {
        return _context.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.AuditLogId == auditLogId);
    }

    public Task<List<string>> GetActionTypesAsync()
    {
        return _context.AuditLogs
            .AsNoTracking()
            .Select(x => x.ActionType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public Task<List<string>> GetEntityNamesAsync()
    {
        return _context.AuditLogs
            .AsNoTracking()
            .Select(x => x.EntityName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public Task<List<User>> GetActorsAsync()
    {
        return _context.Users
            .AsNoTracking()
            .Where(x => x.AuditLogs.Any())
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.Username)
            .Select(x => new User
            {
                UserId = x.UserId,
                FullName = x.FullName,
                Username = x.Username
            })
            .ToListAsync();
    }
}
