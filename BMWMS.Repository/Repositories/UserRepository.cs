using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories;

public class UserRepository : IUserRepository
{
    private readonly BmwmsContext _context;

    public UserRepository(BmwmsContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        var lower = usernameOrEmail.Trim().ToLower();
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.Username.ToLower() == lower ||
                u.Email.ToLower() == lower);
    }

    public async Task<User?> GetByIdAsync(long userId)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task AddSessionAsync(UserSession session)
    {
        await _context.UserSessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task AddAuditLogAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeActiveSessionsAsync(long userId)
    {
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task RevokeSessionAsync(Guid sessionId)
    {
        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session != null && session.RevokedAt == null)
        {
            session.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetRecentPasswordsAsync(long userId, int count)
    {
        return await _context.UserPasswordHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(count)
            .Select(h => h.PasswordHash)
            .ToListAsync();
    }

    public async Task<bool> CheckEmailExistsAsync(string email, long excludeUserId)
    {
        var lowerEmail = email.Trim().ToLower();
        return await _context.Users
            .AnyAsync(u => u.UserId != excludeUserId && u.Email.ToLower() == lowerEmail);
    }

    public async Task AddPasswordResetTokenAsync(PasswordResetToken token)
    {
        await _context.PasswordResetTokens.AddAsync(token);
        await _context.SaveChangesAsync();
    }

    public async Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(long userId, byte[] tokenHash)
    {
        var now = DateTime.UtcNow;
        return await _context.PasswordResetTokens
            .Where(t => t.UserId == userId 
                     && t.TokenHash == tokenHash 
                     && t.ExpiresAt > now 
                     && t.UsedAt == null)
            .OrderByDescending(t => t.RequestedAt)
            .FirstOrDefaultAsync();
    }

    public async Task UpdatePasswordResetTokenAsync(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task AddPasswordHistoryAsync(UserPasswordHistory history)
    {
        await _context.UserPasswordHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<User> Items, int TotalCount)> GetPagedListAsync(string? keyword, string? roleCode, string? status, int pageIndex, int pageSize)
    {
        var query = _context.Users.Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(u => 
                u.Username.ToLower().Contains(kw) ||
                u.FullName.ToLower().Contains(kw) ||
                u.Email.ToLower().Contains(kw) ||
                (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(kw))
            );
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            query = query.Where(u => u.Role.RoleCode == roleCode);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

        int totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<AuditLog>> GetUserActivitiesAsync(long userId, int limit = 20)
    {
        return await _context.AuditLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}
