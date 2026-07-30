using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.User;

public class ChangeLockStateDto
{
    [Required]
    [RegularExpression("^(LOCK|UNLOCK)$", ErrorMessage = "Action chỉ được là LOCK hoặc UNLOCK")]
    public string Action { get; set; } = null!;

    [Required]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Lý do phải từ 5 đến 500 ký tự")]
    public string Reason { get; set; } = null!;
}
