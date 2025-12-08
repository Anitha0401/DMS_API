using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.Models.Circulars;

[Keyless]
public class VwCIRNotification
{
    public required long DM_ManualNotificationId { get; set; }

    public string UserID { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

    public string NotificationType { get; set; } = string.Empty;

    public string NotificationTitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    
    public DateTime? CreatedDate { get; set; }

    public required long DM_ManualID { get; set; }

    public required long DM_ManualVersionID { get; set; }

    public string ManualName { get; set; } = string.Empty;

    public bool? IsRead { get; set; } = false;

    public string Comments { get; set; } = string.Empty;
}
