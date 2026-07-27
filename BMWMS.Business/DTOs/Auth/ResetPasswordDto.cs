using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.Auth;

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Token { get; set; } = null!;

    [Required]
    [MinLength(12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự.")]
    public string NewPassword { get; set; } = null!;

    [Required]
    [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp.")]
    public string ConfirmNewPassword { get; set; } = null!;
}
