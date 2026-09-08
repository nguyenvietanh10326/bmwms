using System.Threading.Tasks;
using BMWMS.Business.DTOs.Auth;
using BMWMS.Business.DTOs.User;
using BMWMS.Business.Common;

namespace BMWMS.Business.Interfaces;

public interface IUserService
{
    Task<LoginResponseDto> GetProfileAsync(long userId);
    Task UpdateProfileAsync(long userId, UpdateProfileRequestDto dto, string? ipAddress);
    Task ChangePasswordAsync(long userId, ChangePasswordRequestDto dto, string? ipAddress);
    Task<PagedResultDto<UserListResponseDto>> GetPagedListAsync(UserFilterDto filter);
    Task<UserDetailDto?> GetUserDetailAsync(long userId);
    Task<long> CreateUserAsync(CreateUserDto dto, long actorUserId, string? ipAddress);
    Task UpdateUserAsync(long targetUserId, UpdateUserDto dto, long editorId, string? ipAddress);
    Task AssignRoleAsync(long targetUserId, AssignRoleDto dto, long editorId, string? ipAddress);
    Task ChangeUserLockStateAsync(long targetUserId, ChangeLockStateDto dto, long adminId, string? ipAddress);
}
