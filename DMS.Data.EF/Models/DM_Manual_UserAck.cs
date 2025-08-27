using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_Manual_UserAck
{
    public long DM_Manual_UserAckID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long DM_ManualID { get; set; }

    public string? Version { get; set; }

    public string? UserRank { get; set; }

    public string? UserName { get; set; }

    public string? UserID { get; set; }

    public DateTime? DateRead { get; set; }

    public string? Comments { get; set; }

    public int? IsActive { get; set; }
}
