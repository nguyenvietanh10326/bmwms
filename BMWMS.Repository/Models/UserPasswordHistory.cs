using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class UserPasswordHistory
{
    public long HistoryId { get; set; }
    
    public long UserId { get; set; }
    
    public string PasswordHash { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    
    public virtual User User { get; set; } = null!;
}
