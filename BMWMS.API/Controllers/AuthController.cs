using BMWMS.Business.DTOs.Auth;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng nhập hệ thống (UC-01)
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            var result = await _authService.LoginAsync(dto, ipAddress, userAgent);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Mật khẩu sai hoặc user không tồn tại (thông báo chung - BR-01)
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("LOCKED:"))
        {
            // Tài khoản bị khóa (NAC-01-02)
            var minutes = ex.Message.Replace("LOCKED:", "");
            return StatusCode(StatusCodes.Status423Locked, new
            {
                message = $"Tài khoản bị khóa tạm thời. Vui lòng thử lại sau {minutes} phút.",
                remainingMinutes = int.Parse(minutes)
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "INACTIVE")
        {
            // Tài khoản bị deactivate (AF-03)
            return Unauthorized(new
            {
                message = "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi hệ thống.", detail = ex.Message });
        }
    }
}
