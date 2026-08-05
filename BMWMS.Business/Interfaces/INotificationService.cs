using System.Threading.Tasks;
using BMWMS.Business.DTOs.Notification;

namespace BMWMS.Business.Interfaces;

public interface INotificationService
{
    Task<NotificationResponseDto> GetNotificationsAsync(long userId, NotificationFilterDto filter);
    Task MarkAsReadAsync(long notificationId, long userId);
    Task MarkAllAsReadAsync(long userId);
    Task CreateNotificationAsync(CreateNotificationDto dto, long createdByUserId);
}
