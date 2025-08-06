using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_Manual_Log
{
    public long DM_Manual_LogID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long DM_ManualID { get; set; }

    public long DM_ManualVersionID { get; set; }

    public string Activity { get; set; } = null!;

    public string Remarks { get; set; } = null!;

    public DateTime? Logdate { get; set; }

    public string? UserRank { get; set; }

    public string? UserName { get; set; }
}
