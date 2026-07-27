namespace BMWMS.Business.DTOs.Auth;

public class LoginResponseDto
{
    public long UserId { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string RoleCode { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public DateTime LoginAt { get; set; }
}
