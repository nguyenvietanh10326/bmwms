using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.User;

public class AssignRoleDto
{
    [Required]
    public int RoleId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = null!;
}
