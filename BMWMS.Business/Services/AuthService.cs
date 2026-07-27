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
    private readonly IEmailService _emailService;

    public AuthService(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
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
            LoginAt = DateTime.UtcNow,
            SessionId = session.SessionId
        };
    }

    public async Task LogoutAsync(Guid sessionId, long userId, string? ipAddress)
    {
        await _userRepository.RevokeSessionAsync(sessionId);

        await _userRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = userId,
            ActionType = "LOGOUT_SUCCESS",
            EntityName = "UserSession",
            EntityId = sessionId.ToString(),
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto request)
    {
        var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
        if (user == null || user.Status != "ACTIVE")
        {
            // Do not reveal if email exists or not
            return;
        }

        // Generate 6 digit OTP
        var random = new Random();
        string otp = random.Next(100000, 999999).ToString();

        // Hash OTP
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] tokenHash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(otp));

        var token = new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = tokenHash,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        await _userRepository.AddPasswordResetTokenAsync(token);

        string body = $"Chào {user.FullName},\n\nMã xác thực để đặt lại mật khẩu của bạn là: {otp}\n\nMã này sẽ hết hạn trong vòng 15 phút.\nNếu bạn không yêu cầu, vui lòng bỏ qua email này.";
        await _emailService.SendEmailAsync(user.Email, "Yêu cầu đặt lại mật khẩu BMWMS", body);

        await _userRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = user.UserId,
            ActionType = "FORGOT_PASSWORD_REQUEST",
            EntityName = "User",
            EntityId = user.UserId.ToString(),
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request)
    {
        var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
        if (user == null)
            throw new InvalidOperationException("Yêu cầu không hợp lệ.");

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] inputTokenHash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Token));

        var validToken = await _userRepository.GetValidPasswordResetTokenAsync(user.UserId, inputTokenHash);
        if (validToken == null)
            throw new InvalidOperationException("Mã xác thực không hợp lệ hoặc đã hết hạn.");

        // Rule BR-03: check past passwords
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }

        var recentHashes = await _userRepository.GetRecentPasswordsAsync(user.UserId, 5);
        foreach (var hash in recentHashes)
        {
            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, hash))
            {
                throw new InvalidOperationException("Mật khẩu mới không được trùng với 5 mật khẩu gần nhất đã sử dụng.");
            }
        }

        // Check complexity (should be done by DTO validation, but double check)
        if (request.NewPassword.Length < 12 || 
            !request.NewPassword.Any(char.IsLower) || 
            !request.NewPassword.Any(char.IsUpper) || 
            !request.NewPassword.Any(char.IsDigit) || 
            !request.NewPassword.Any(c => !char.IsLetterOrDigit(c)))
        {
            throw new InvalidOperationException("Mật khẩu không đáp ứng đủ yêu cầu bảo mật (tối thiểu 12 ký tự, gồm hoa, thường, số và ký tự đặc biệt).");
        }

        var oldHashForHistory = user.PasswordHash;

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // Mark token as used
        validToken.UsedAt = DateTime.UtcNow;
        await _userRepository.UpdatePasswordResetTokenAsync(validToken);

        // Revoke active sessions
        await _userRepository.RevokeActiveSessionsAsync(user.UserId);

        // Add to history
        await _userRepository.AddPasswordHistoryAsync(new UserPasswordHistory
        {
            UserId = user.UserId,
            PasswordHash = oldHashForHistory,
            CreatedAt = DateTime.UtcNow
        });
    }
}
