using System.Linq;
using System.Threading.Tasks;
using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Notification;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;

namespace BMWMS.Business.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly BMWMS.Repository.Models.BmwmsContext _context;

    public NotificationService(INotificationRepository notificationRepository, BMWMS.Repository.Models.BmwmsContext context)
    {
        _notificationRepository = notificationRepository;
        _context = context;
    }

    public async Task<NotificationResponseDto> GetNotificationsAsync(long userId, NotificationFilterDto filter)
    {
        var result = await _notificationRepository.GetNotificationsAsync(
            userId,
            filter.IsRead,
            filter.PageNumber,
            filter.PageSize);

        var items = result.Items.Select(x => new NotificationItemDto
        {
            NotificationId = x.NotificationId,
            NotificationType = x.NotificationType,
            Title = x.Title,
            Message = x.Message,
            ReferenceType = x.ReferenceType,
            ReferenceId = x.ReferenceId,
            IsRead = x.IsRead,
            CreatedAt = x.CreatedAt,
            ReadAt = x.ReadAt
        }).ToList();

        return new NotificationResponseDto
        {
            Data = new PagedResultDto<NotificationItemDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                PageIndex = filter.PageNumber,
                PageSize = filter.PageSize
            }
        };
    }

    public async Task MarkAsReadAsync(long notificationId, long userId)
    {
        await _notificationRepository.MarkAsReadAsync(notificationId, userId);
    }

    public async Task MarkAllAsReadAsync(long userId)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId);
    }

    public async Task CreateNotificationAsync(CreateNotificationDto dto, long createdByUserId)
    {
        // 1. Fetch target users
        var userIdsQuery = _context.Users.Where(u => u.Status == "ACTIVE");
        
        if (dto.TargetRoleId.HasValue)
        {
            userIdsQuery = userIdsQuery.Where(u => u.RoleId == dto.TargetRoleId.Value);
        }

        var targetUserIds = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            System.Linq.Queryable.Select(userIdsQuery, u => u.UserId)
        );

        if (!targetUserIds.Any())
            return;

        // 2. Create Notification entities
        var now = System.DateTime.UtcNow;
        var notifications = targetUserIds.Select(uid => new BMWMS.Repository.Models.Notification
        {
            UserId = uid,
            Title = dto.Title,
            Message = dto.Message,
            NotificationType = dto.NotificationType,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        // 3. Save to DB
        await _notificationRepository.AddNotificationsAsync(notifications);
    }
}
