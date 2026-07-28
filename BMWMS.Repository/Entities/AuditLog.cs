using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class AuditLog
{
    public long AuditLogId { get; set; }

    public long? UserId { get; set; }

    public string ActionType { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
