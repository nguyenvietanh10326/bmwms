using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class Notification
{
    public long NotificationId { get; set; }

    public long UserId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public virtual User User { get; set; } = null!;
}
