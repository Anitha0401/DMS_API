using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_Manual_RolesToAck
{
    public long DM_Manual_RolesToAckID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long DM_ManualID { get; set; }

    public string UserRank { get; set; } = null!;

    public int? IsActive { get; set; }
}
