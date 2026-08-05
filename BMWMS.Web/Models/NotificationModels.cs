using System;

namespace BMWMS.Web.Models;

public class NotificationFilterModel
{
    public bool? IsRead { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class NotificationItemModel
{
    public long NotificationId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class NotificationResponseModel
{
    public PagedResultModel<NotificationItemModel> Data { get; set; } = new();
}

public class CreateNotificationModel
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = "SYSTEM";
    public int? TargetRoleId { get; set; }
}
