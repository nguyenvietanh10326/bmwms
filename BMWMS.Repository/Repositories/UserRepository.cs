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
}
