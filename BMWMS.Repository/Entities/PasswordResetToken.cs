using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class PasswordResetToken
{
    public long PasswordResetTokenId { get; set; }

    public long UserId { get; set; }

    public byte[] TokenHash { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
