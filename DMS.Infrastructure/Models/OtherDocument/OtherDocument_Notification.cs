using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.OtherDocument;

public class OtherDocument_Notification
{
    [Key]
    [Column("OtherDocument_NotificationId")]
    public long OtherDocument_NotificationID { get; set; }

    [StringLength(3)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    public string UserID { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

    public string NotificationType { get; set; } = string.Empty;

    public string NotificationTitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    
    public DateTime? CreatedDate { get; set; }

    public long OtherDocumentMasterID { get; set; }

    public int? IsRead { get; set; } = 0;

    public string Comments { get; set; } = string.Empty;
}
