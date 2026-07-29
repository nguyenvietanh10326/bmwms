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

    public async Task<BMWMS.Business.Common.PagedResultDto<UserListResponseDto>> GetPagedListAsync(UserFilterDto filter)
    {
        var (items, totalCount) = await _userRepository.GetPagedListAsync(
            filter.Keyword, 
            filter.RoleCode, 
            filter.Status, 
            filter.PageIndex, 
            filter.PageSize
        );

        var dtoItems = items.Select(u => new UserListResponseDto
        {
            UserId = u.UserId,
            UserCode = "USR-" + u.UserId.ToString().PadLeft(3, '0'),
            FullName = u.FullName,
            Username = u.Username,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            RoleName = u.Role?.RoleName ?? "",
            RoleCode = u.Role?.RoleCode ?? "",
            Status = u.Status,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            LastLoginAt = u.LastLoginAt
        }).ToList();

        return new BMWMS.Business.Common.PagedResultDto<UserListResponseDto>
        {
            Items = dtoItems,
            TotalCount = totalCount,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<UserDetailDto?> GetUserDetailAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        var activities = await _userRepository.GetUserActivitiesAsync(userId, 20);

        var dto = new UserDetailDto
        {
            UserId = user.UserId,
            UserCode = $"USR-{user.UserId:D3}",
            FullName = user.FullName,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RoleName = user.Role.RoleName,
            RoleCode = user.Role.RoleCode,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt ?? user.CreatedAt,
            FailedLoginCount = user.FailedLoginCount,
            LockedUntil = user.LockedUntil,
            LastLoginAt = user.LastLoginAt,
            Activities = activities.Select(a => new UserActivityDto
            {
                CreatedAt = a.CreatedAt,
                ActionType = a.ActionType,
                IpAddress = a.IpAddress,
                Details = a.NewValuesJson ?? a.OldValuesJson ?? string.Empty
            }).ToList()
        };

        return dto;
    }

    public async Task<long> CreateUserAsync(CreateUserDto dto, string? ipAddress)
    {
        if (await _userRepository.CheckUsernameExistsAsync(dto.Username))
        {
            throw new ArgumentException("Tên đăng nhập đã tồn tại.");
        }

        if (await _userRepository.CheckEmailExistsAsync(dto.Email, 0))
        {
            throw new ArgumentException("Email này đã được sử dụng.");
        }

        if (dto.Password.Length < 12)
        {
            throw new ArgumentException("Mật khẩu phải có ít nhất 12 ký tự.");
        }
        if (!Regex.IsMatch(dto.Password, @"[A-Z]") ||
            !Regex.IsMatch(dto.Password, @"[a-z]") ||
            !Regex.IsMatch(dto.Password, @"[0-9]") ||
            !Regex.IsMatch(dto.Password, @"[^a-zA-Z0-9]"))
        {
            throw new ArgumentException("Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            RoleId = dto.RoleId,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow,
            FailedLoginCount = 0
        };

        user.UserPasswordHistories.Add(new UserPasswordHistory
        {
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        });

        await _userRepository.AddAsync(user);

        await _userRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = user.UserId, // Note: This will be the new ID if EF populates it, otherwise it's 0 for the log. For accurate logging, it might need a separate SaveChanges or EF handles it if we don't save log inside AddAsync. Wait, AddAsync in repo already calls SaveChanges! So user.UserId is populated.
            ActionType = "CREATE_USER",
            EntityName = "User",
            EntityId = user.UserId.ToString(),
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });

        return user.UserId;
    }

    public async Task UpdateUserAsync(long targetUserId, UpdateUserDto dto, long editorId, string? ipAddress)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId);
        if (user == null)
        {
            throw new ArgumentException("Người dùng không tồn tại.");
        }

        if (await _userRepository.CheckEmailExistsAsync(dto.Email, targetUserId))
        {
            throw new ArgumentException("Email này đã được sử dụng.");
        }

        // BR-04: Cannot deactivate the last SYSTEM_ADMIN or lock yourself
        if (dto.Status != "ACTIVE")
        {
            if (targetUserId == editorId)
            {
                throw new ArgumentException("Không thể tự vô hiệu hóa tài khoản của chính mình.");
            }

            if (user.Role.RoleCode == "SYSTEM_ADMIN" && user.Status == "ACTIVE")
            {
                var adminCount = await _userRepository.GetActiveSystemAdminCountAsync();
                if (adminCount <= 1)
                {
                    throw new ArgumentException("Không thể vô hiệu hóa Quản trị viên hệ thống duy nhất còn lại.");
                }
            }
        }

        var oldValues = $"{{ \"FullName\": \"{user.FullName}\", \"Email\": \"{user.Email}\", \"PhoneNumber\": \"{user.PhoneNumber}\", \"RoleId\": {user.RoleId}, \"Status\": \"{user.Status}\" }}";
        
        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;
        user.RoleId = dto.RoleId;
        user.Status = dto.Status;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        var newValues = $"{{ \"FullName\": \"{user.FullName}\", \"Email\": \"{user.Email}\", \"PhoneNumber\": \"{user.PhoneNumber}\", \"RoleId\": {user.RoleId}, \"Status\": \"{user.Status}\" }}";

        await _userRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = editorId,
            ActionType = "UPDATE_USER",
            EntityName = "User",
            EntityId = targetUserId.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task AssignRoleAsync(long targetUserId, AssignRoleDto dto, long editorId, string? ipAddress)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId);
        if (user == null)
        {
            throw new ArgumentException("Người dùng không tồn tại.");
        }

        if (user.RoleId == dto.RoleId)
        {
            throw new ArgumentException("Vai trò mới giống với vai trò hiện tại.");
        }

        // BR-04: The only usable System Administrator cannot be reassigned.
        if (user.Role.RoleCode == "SYSTEM_ADMIN" && user.Status == "ACTIVE")
        {
            var adminCount = await _userRepository.GetActiveSystemAdminCountAsync();
            if (adminCount <= 1)
            {
                throw new ArgumentException("Không thể thay đổi vai trò của Quản trị viên hệ thống duy nhất còn lại.");
            }
        }

        var oldValues = $"{{ \"RoleId\": {user.RoleId} }}";
        
        user.RoleId = dto.RoleId;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        var newValues = $"{{ \"RoleId\": {user.RoleId}, \"Reason\": \"{dto.Reason}\" }}";

        await _userRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = editorId,
            ActionType = "ASSIGN_ROLE",
            EntityName = "User",
            EntityId = targetUserId.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });

        // BR-05: Hủy tất cả các phiên đăng nhập hiện tại để quyền mới được áp dụng ngay lập tức
        await _userRepository.RevokeActiveSessionsAsync(targetUserId);
    }

    public async Task ChangeUserLockStateAsync(long targetUserId, ChangeLockStateDto dto, long adminId, string? ipAddress)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId);
        if (user == null)
        {
            throw new ArgumentException("Người dùng không tồn tại.");
        }

        var isCurrentlyLocked = user.Status == "LOCKED";
        
        if (dto.Action == "LOCK")
        {
            if (isCurrentlyLocked)
                throw new ArgumentException("Tài khoản đã ở trạng thái khóa.");

            // BR-02: Không thể khóa tài khoản System Administrator cuối cùng
            if (user.Role.RoleCode == "SYSTEM_ADMIN" && user.Status == "ACTIVE")
            {
                var adminCount = await _userRepository.GetActiveSystemAdminCountAsync();
                if (adminCount <= 1)
                {
                    throw new ArgumentException("Không thể khóa tài khoản Quản trị viên hệ thống duy nhất còn lại.");
                }
            }

            user.Status = "LOCKED";
            user.LockedUntil = null; // Khóa vô thời hạn (cho tới khi mở khóa thủ công)
            user.FailedLoginCount = 0; // Reset số lần đăng nhập sai
        }
        else if (dto.Action == "UNLOCK")
        {
            if (!isCurrentlyLocked)
                throw new ArgumentException("Tài khoản đang ở trạng thái hoạt động.");

            user.Status = "ACTIVE";
            user.LockedUntil = null;
            user.FailedLoginCount = 0;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var newValues = $"{{ \"Status\": \"{user.Status}\", \"Reason\": \"{dto.Reason}\" }}";
        var oldStatus = isCurrentlyLocked ? "LOCKED" : "ACTIVE";
        var oldValues = $"{{ \"Status\": \"{oldStatus}\" }}";

        await _userRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = adminId,
            ActionType = dto.Action == "LOCK" ? "LOCK_ACCOUNT" : "UNLOCK_ACCOUNT",
            EntityName = "User",
            EntityId = targetUserId.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });

        if (dto.Action == "LOCK")
        {
            // BR-03: Khóa tài khoản sẽ thu hồi tất cả phiên làm việc hiện hành
            await _userRepository.RevokeActiveSessionsAsync(targetUserId);
        }
    }
}
