using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface INotificationRepository
{
    Task<(int TotalCount, List<Notification> Items)> GetNotificationsAsync(long userId, bool? isRead, int pageNumber, int pageSize);
    Task<Notification?> GetByIdAsync(long notificationId);
    Task MarkAsReadAsync(long notificationId, long userId);
    Task MarkAllAsReadAsync(long userId);
    Task AddNotificationsAsync(IEnumerable<Notification> notifications);
}
