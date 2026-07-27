using System;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.User;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        // Lấy UserId từ header
        if (!Request.Headers.TryGetValue("X-User-Id", out var userIdStr) || !long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("User ID is missing from headers.");
        }

        try
        {
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(profile);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto)
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var userIdStr) || !long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("User ID is missing from headers.");
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _userService.UpdateProfileAsync(userId, dto, ipAddress);
            return Ok(new { message = "Cập nhật hồ sơ thành công" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var userIdStr) || !long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("User ID is missing from headers.");
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _userService.ChangePasswordAsync(userId, dto, ipAddress);
            return Ok(new { message = "Đổi mật khẩu thành công" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
