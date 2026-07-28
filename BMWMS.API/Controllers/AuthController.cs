using System.Security.Claims;
using BMWMS.Business.DTOs.Auth;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

    /// <summary>
    /// Đăng xuất hệ thống (UC-62)
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionIdStr = User.FindFirstValue("SessionId");

        if (!long.TryParse(userIdStr, out var userId) || !Guid.TryParse(sessionIdStr, out var sessionId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.LogoutAsync(sessionId, userId, ipAddress);
            return Ok(new { message = "Đăng xuất thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi hệ thống.", detail = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _authService.ForgotPasswordAsync(dto);
            return Ok(new { message = "Nếu email hợp lệ, một mã xác thực đã được gửi đến email của bạn." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi hệ thống.", detail = ex.Message });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Đặt lại mật khẩu thành công." });
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
