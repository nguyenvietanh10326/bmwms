namespace BMWMS.Business.DTOs.Notification;

public class NotificationFilterDto
{
    public bool? IsRead { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
