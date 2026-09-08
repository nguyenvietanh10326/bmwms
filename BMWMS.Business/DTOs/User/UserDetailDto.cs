namespace BMWMS.Business.DTOs.User;

public class UserDetailDto
{
    public long UserId { get; set; }
    public string UserCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string RoleName { get; set; } = null!;
    public string RoleCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public List<UserActivityDto> Activities { get; set; } = new();
}

public class UserActivityDto
{
    public DateTime CreatedAt { get; set; }
    public string ActionType { get; set; } = null!;
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
}
