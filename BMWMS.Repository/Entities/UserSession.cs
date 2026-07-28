using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class UserSession
{
    public Guid SessionId { get; set; }

    public long UserId { get; set; }

    public byte[] RefreshTokenHash { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
