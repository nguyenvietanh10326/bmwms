using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BMWMS.Repository.Context;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly BmwmsContext _context;

    public NotificationRepository(BmwmsContext context)
    {
        _context = context;
    }

    public async Task<(int TotalCount, List<Notification> Items)> GetNotificationsAsync(long userId, bool? isRead, int pageNumber, int pageSize)
    {
        var query = _context.Notifications.Where(x => x.UserId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(x => x.IsRead == isRead.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, items);
    }

    public async Task<Notification?> GetByIdAsync(long notificationId)
    {
        return await _context.Notifications.FindAsync(notificationId);
    }

    public async Task MarkAsReadAsync(long notificationId, long userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(long userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync();

        if (unreadNotifications.Any())
        {
            var now = DateTime.UtcNow;
            foreach (var item in unreadNotifications)
            {
                item.IsRead = true;
                item.ReadAt = now;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddNotificationsAsync(IEnumerable<Notification> notifications)
    {
        await _context.Notifications.AddRangeAsync(notifications);
        await _context.SaveChangesAsync();
    }
}
