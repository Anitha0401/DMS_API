using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.OtherDocument;

[Keyless]
public class VwOtherDocumentNotification
{
    public required long OtherDocument_NotificationID { get; set; }

    public string UserID { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

    public string NotificationType { get; set; } = string.Empty;

    public string NotificationTitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    
    public DateTime? CreatedDate { get; set; }

    public required long OtherDocumentMasterID { get; set; }

    public string Reference { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string DocumentNumber { get; set; } = string.Empty;

    public bool? IsRead { get; set; } = false;

    public string Comments { get; set; } = string.Empty;
}
