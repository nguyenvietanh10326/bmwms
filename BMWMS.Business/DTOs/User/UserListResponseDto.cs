using System;

namespace BMWMS.Business.DTOs.User;

public class UserListResponseDto
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
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
