using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Application.DTO.Circulars;

public class CIR_Notification
{
    [Key]
    [Column("CIR_NotificationId")]
    public long CIR_NotificationID { get; set; }

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

    public long CIR_MasterID { get; set; }

    public int? IsRead { get; set; } = 0;

    public string Comments { get; set; } = string.Empty;
}
