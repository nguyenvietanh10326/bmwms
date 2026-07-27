using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
    Task<User?> GetByIdAsync(long userId);
    Task UpdateAsync(User user);
    Task AddSessionAsync(UserSession session);
    Task AddAuditLogAsync(AuditLog log);
    Task RevokeActiveSessionsAsync(long userId);
}
