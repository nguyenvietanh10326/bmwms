using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.Notification;

public class CreateNotificationDto
{
    [Required(ErrorMessage = "Tiêu đề không được để trống.")]
    [StringLength(200)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung không được để trống.")]
    [StringLength(2000)]
    public string Message { get; set; } = null!;

    [Required]
    public string NotificationType { get; set; } = "SYSTEM";

    public int? TargetRoleId { get; set; }

    public long? TargetUserId { get; set; }

    public string? ReferenceType { get; set; }

    public string? ReferenceId { get; set; }
}
