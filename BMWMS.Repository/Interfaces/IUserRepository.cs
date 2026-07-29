using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail); // select * form user where username = @usernameOrEmail or email = @usernameOrEmail
    Task<User?> GetByIdAsync(long userId);
    Task UpdateAsync(User user);
    Task AddSessionAsync(UserSession session);
    Task AddAuditLogAsync(AuditLog log);
    Task RevokeActiveSessionsAsync(long userId);
    Task RevokeSessionAsync(Guid sessionId);
    Task<List<string>> GetRecentPasswordsAsync(long userId, int count);
    Task<bool> CheckEmailExistsAsync(string email, long excludeUserId);
    Task AddPasswordResetTokenAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(long userId, byte[] tokenHash);
    Task UpdatePasswordResetTokenAsync(PasswordResetToken token);
    Task AddPasswordHistoryAsync(UserPasswordHistory history);
    Task<(List<User> Items, int TotalCount)> GetPagedListAsync(string? keyword, string? roleCode, string? status, int pageIndex, int pageSize);
    Task<List<AuditLog>> GetUserActivitiesAsync(long userId, int limit = 20);
    Task AddAsync(User user);
    Task<bool> CheckUsernameExistsAsync(string username);
}
