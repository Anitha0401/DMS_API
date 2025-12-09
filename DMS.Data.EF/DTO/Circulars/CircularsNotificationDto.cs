namespace DMS.Data.EF.DTO.Circulars;

public class CircularsNotificationDto : CircularsBaseDto
{
    public required long ManualNotificationId { get; set; }

    public string NotificationType { get; set; } = string.Empty;

    public string NotificationTitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime? CreatedDate { get; set; }

    public int? IsRead {  get; set; } = 0;
    
}
