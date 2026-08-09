using System.Security.Claims;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.Notification;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] NotificationFilterDto filter)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }

        var result = await _notificationService.GetNotificationsAsync(userId, filter);
        return Ok(result);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }

        await _notificationService.MarkAsReadAsync(id, userId);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }
        await _notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }

    [HttpPost]
    [Authorize(Roles = "SYSTEM_ADMIN")]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Token không hợp lệ." });
        }
        await _notificationService.CreateNotificationAsync(dto, userId);
        return Ok(new { message = "Notifications created successfully." });
    }
}
