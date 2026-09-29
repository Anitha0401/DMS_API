using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Circulars;

[Keyless]
public class VwCIRNotification
{
    public required long CIR_NotificationID { get; set; }

    public string UserID { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

    public string NotificationType { get; set; } = string.Empty;

    public string NotificationTitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    
    public DateTime? CreatedDate { get; set; }

    public required long CIR_MasterID { get; set; }

    public string Reference { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string CIR_Number { get; set; } = string.Empty;

    public bool? IsRead { get; set; } = false;

    public string Comments { get; set; } = string.Empty;
}
