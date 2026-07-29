namespace BMWMS.Business.DTOs.User;

public class CreateUserDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public string Status { get; set; } = "ACTIVE";
}
