using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email.")]
    [StringLength(254, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 254 ký tự.")]
    public string UsernameOrEmail { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = null!;

    public bool RememberUsername { get; set; }
}
