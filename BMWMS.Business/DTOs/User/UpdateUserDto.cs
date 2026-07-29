namespace BMWMS.Business.DTOs.User;

public class UpdateUserDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public string Status { get; set; } = null!;
}
