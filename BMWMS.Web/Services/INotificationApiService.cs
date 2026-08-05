using System.Threading.Tasks;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public interface INotificationApiService
{
    Task<NotificationResponseModel> GetNotificationsAsync(NotificationFilterModel filter);
    Task MarkAsReadAsync(long notificationId);
    Task MarkAllAsReadAsync();
    Task CreateNotificationAsync(CreateNotificationModel model);
}
