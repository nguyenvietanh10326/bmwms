namespace BMWMS.Business.DTOs.User;

public class UpdateProfileRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}
