using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.Auth;
using BMWMS.Business.DTOs.User;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;

namespace BMWMS.Business.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<LoginResponseDto> GetProfileAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException("Người dùng không tồn tại hoặc đã bị khóa.");
        }

        return new LoginResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RoleCode = user.Role.RoleCode,
            RoleName = user.Role.RoleName,
            AvatarUrl = user.AvatarUrl,
            LoginAt = user.LastLoginAt ?? DateTime.UtcNow
        };
    }

    public async Task UpdateProfileAsync(long userId, UpdateProfileRequestDto dto, string? ipAddress)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(dto.FullName) || dto.FullName.Length < 2 || dto.FullName.Length > 150)
        {
            throw new ArgumentException("Họ tên phải từ 2 đến 150 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber.Length > 20)
        {
            throw new ArgumentException("Số điện thoại không được vượt quá 20 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            if (await _userRepository.CheckEmailExistsAsync(dto.Email, userId))
            {
                throw new ArgumentException("Email này đã được sử dụng bởi tài khoản khác.");
            }
        }

        // Lưu log thay đổi (audit)
        var oldValues = $"{{ \"FullName\": \"{user.FullName}\", \"PhoneNumber\": \"{user.PhoneNumber}\", \"Email\": \"{user.Email}\" }}";
        
        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            user.Email = dto.Email;
        }

        await _userRepository.UpdateAsync(user);

        var newValues = $"{{ \"FullName\": \"{user.FullName}\", \"PhoneNumber\": \"{user.PhoneNumber}\", \"Email\": \"{user.Email}\" }}";

        await _userRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = user.UserId,
            ActionType = "UPDATE_PROFILE",
            EntityName = "User",
            EntityId = user.UserId.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequestDto dto, string? ipAddress)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");
        }

        // 1. Kiểm tra mật khẩu hiện tại
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            throw new ArgumentException("Mật khẩu hiện tại không đúng.");
        }

        // 2. Validate mật khẩu mới (Rule BR-02)
        if (dto.NewPassword.Length < 12)
        {
            throw new ArgumentException("Mật khẩu mới phải có ít nhất 12 ký tự.");
        }
        if (!Regex.IsMatch(dto.NewPassword, @"[A-Z]") ||
            !Regex.IsMatch(dto.NewPassword, @"[a-z]") ||
            !Regex.IsMatch(dto.NewPassword, @"[0-9]") ||
            !Regex.IsMatch(dto.NewPassword, @"[^a-zA-Z0-9]"))
        {
            throw new ArgumentException("Mật khẩu mới phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
        }

        // 3. Kiểm tra trùng mật khẩu hiện tại
        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
        {
            throw new ArgumentException("Mật khẩu mới không được giống mật khẩu hiện tại.");
        }

        // 4. Kiểm tra lịch sử 5 mật khẩu gần nhất (Rule BR-03)
        var recentHashes = await _userRepository.GetRecentPasswordsAsync(userId, 5);
        foreach (var hash in recentHashes)
        {
            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, hash))
            {
                throw new ArgumentException("Mật khẩu mới không được trùng với 5 mật khẩu gần nhất.");
            }
        }

        // 5. Cập nhật và lưu vào DB
        var oldHashForHistory = user.PasswordHash;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        
        // Thêm vào history
        user.UserPasswordHistories.Add(new UserPasswordHistory
        {
            UserId = user.UserId,
            PasswordHash = oldHashForHistory,
            CreatedAt = DateTime.UtcNow
        });

        await _userRepository.UpdateAsync(user);

        // 6. Ghi Audit Log (Rule BR-05: without credential content)
        await _userRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = user.UserId,
            ActionType = "CHANGE_PASSWORD",
            EntityName = "User",
            EntityId = user.UserId.ToString(),
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });

        // 7. Thu hồi các session khác nếu cần thiết
        if (dto.SignOutOtherSessions)
        {
            await _userRepository.RevokeActiveSessionsAsync(userId);
        }
    }
}
