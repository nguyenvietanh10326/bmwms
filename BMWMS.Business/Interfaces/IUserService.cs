using System.Threading.Tasks;
using BMWMS.Business.DTOs.Auth;
using BMWMS.Business.DTOs.User;

namespace BMWMS.Business.Interfaces;

public interface IUserService
{
    Task<LoginResponseDto> GetProfileAsync(long userId);
    Task UpdateProfileAsync(long userId, UpdateProfileRequestDto dto, string? ipAddress);
    Task ChangePasswordAsync(long userId, ChangePasswordRequestDto dto, string? ipAddress);
}
