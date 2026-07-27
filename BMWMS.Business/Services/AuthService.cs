using BCrypt.Net;
using BMWMS.Business.DTOs.Auth;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;

namespace BMWMS.Business.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, string? ipAddress, string? userAgent)
    {
        // 1. Tìm user theo username hoặc email
        var user = await _userRepository.GetByUsernameOrEmailAsync(dto.UsernameOrEmail);

        // 2. User không tồn tại → thông báo chung (không tiết lộ trường nào sai - BR-01)
        if (user == null)
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        // 3. Kiểm tra tài khoản có đang bị khóa tạm thời không (NAC-01-02)
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            var remaining = (int)Math.Ceiling((user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
            throw new InvalidOperationException($"LOCKED:{remaining}");
        }

        // 4. Kiểm tra tài khoản có ACTIVE không (AF-03)
        if (user.Status != "ACTIVE")
        {
            throw new InvalidOperationException("INACTIVE");
        }

        // 5. Kiểm tra role hợp lệ (BR-03)
        if (user.Role == null || !user.Role.IsActive)
        {
            throw new InvalidOperationException("Tài khoản chưa được gán vai trò hợp lệ. Vui lòng liên hệ quản trị viên.");
        }

        // 6. Xác thực mật khẩu BCrypt
        bool passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!passwordValid)
        {
            // Tăng số lần sai
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                // Khóa tài khoản 15 phút (NAC-01-02)
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                user.FailedLoginCount = 0;
                await _userRepository.UpdateAsync(user);

                // Ghi audit log
                await _userRepository.AddAuditLogAsync(new AuditLog
                {
                    UserId = user.UserId,
                    ActionType = "LOGIN_LOCKED",
                    EntityName = "User",
                    EntityId = user.UserId.ToString(),
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                });

                throw new InvalidOperationException($"LOCKED:{LockoutMinutes}");
            }

            await _userRepository.UpdateAsync(user);

            // Ghi audit log thất bại
            await _userRepository.AddAuditLogAsync(new AuditLog
            {
                UserId = user.UserId,
                ActionType = "LOGIN_FAILED",
                EntityName = "User",
                EntityId = user.UserId.ToString(),
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        // 7. Đăng nhập thành công → reset failed count, tạo session
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // 8. Tạo session trong DB
        var session = new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = user.UserId,
            RefreshTokenHash = System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8) // AC-01-05: 8 giờ không hoạt động
        };
        await _userRepository.AddSessionAsync(session);

        // 9. Ghi audit log thành công (BR-04)
        await _userRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = user.UserId,
            ActionType = "LOGIN_SUCCESS",
            EntityName = "User",
            EntityId = user.UserId.ToString(),
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });

        return new LoginResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            RoleCode = user.Role.RoleCode,
            RoleName = user.Role.RoleName,
            AvatarUrl = user.AvatarUrl,
            LoginAt = DateTime.UtcNow
        };
    }
}
