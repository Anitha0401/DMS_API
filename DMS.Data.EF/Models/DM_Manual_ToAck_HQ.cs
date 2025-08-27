using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_Manual_ToAck_HQ
{
    public long DM_Manual_ToAck_HQID { get; set; }

    public string Origin { get; set; } = null!;
   
    public int VesselID { get; set; }

    public long DM_ManualID { get; set; }

    public long HQ_UsersID { get; set; }

    public string UserRole { get; set; } = null!;

    public int? IsActive { get; set; }
}
