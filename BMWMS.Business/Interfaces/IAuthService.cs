using BMWMS.Business.DTOs.Auth;

namespace BMWMS.Business.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Xác thực đăng nhập. Ném exception nếu thất bại.
    /// </summary>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, string? ipAddress, string? userAgent);
}
