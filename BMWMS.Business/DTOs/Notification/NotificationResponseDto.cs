using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Notification;

public class NotificationResponseDto
{
    public PagedResultDto<NotificationItemDto> Data { get; set; } = new();
}
