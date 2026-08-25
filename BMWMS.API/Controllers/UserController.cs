using System.Security.Claims;
using BMWMS.Business.DTOs.User;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[Authorize]
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
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
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
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
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
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
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

    [Authorize(Roles = "SYSTEM_ADMIN")]
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter)
    {
        try
        {
            var result = await _userService.GetPagedListAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserDetail(long id)
    {
        try
        {
            var result = await _userService.GetUserDetailAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN")]
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var actorIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(actorIdText, out var actorId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var newUserId = await _userService.CreateUserAsync(dto, actorId, ipAddress);
            return Ok(new { message = "Tạo người dùng thành công", userId = newUserId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserDto dto)
    {
        var editorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(editorIdStr, out var editorId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _userService.UpdateUserAsync(id, dto, editorId, ipAddress);
            return Ok(new { message = "Cập nhật người dùng thành công" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN")]
    [HttpPut("{id}/role")]
    public async Task<IActionResult> AssignRole(long id, [FromBody] AssignRoleDto dto)
    {
        var editorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(editorIdStr, out var editorId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _userService.AssignRoleAsync(id, dto, editorId, ipAddress);
            return Ok(new { message = "Gán vai trò thành công" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN")]
    [HttpPut("{id}/lock-state")]
    public async Task<IActionResult> ChangeLockState(long id, [FromBody] ChangeLockStateDto dto)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(adminIdStr, out var adminId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _userService.ChangeUserLockStateAsync(id, dto, adminId, ipAddress);
            var actionMsg = dto.Action == "LOCK" ? "Khóa tài khoản thành công" : "Mở khóa tài khoản thành công";
            return Ok(new { message = actionMsg });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
