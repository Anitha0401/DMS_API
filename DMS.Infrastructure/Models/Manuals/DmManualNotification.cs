using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.Manuals;

public class DM_ManualNotification
{
    [Key]
    [Column("DM_ManualNotificationId")]
    public long DM_ManualNotificationId { get; set; }


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

    public long DM_ManualID { get; set; }

    public long DM_ManualVersionID { get; set; }

    public int? IsRead { get; set; } = 0;

    public string Comments { get; set; } = string.Empty;
}
