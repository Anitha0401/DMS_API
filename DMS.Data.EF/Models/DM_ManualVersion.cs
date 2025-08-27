using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_ManualVersion
{
    public long DM_ManualVersionID { get; set; }

    public long DM_ManualID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string? Version { get; set; }

    public string? TextContents { get; set; } = null!;

    public string? ReleasedBy { get; set; }

    public DateTime? ReleasedDate { get; set; }
    
    public int IsActive { get; set; }
    
    public int IsCurrent { get; set; }

    public string? FinalisedBy { get; set; }

    public DateTime? FinalisedDate { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public string? Comments { get; set; }
}
