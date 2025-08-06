using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_Manual_ApprovalSettings
{
    public long DM_Manual_ApprovalSettingsID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string Hqdept { get; set; } = null!;

    public string? UserRole { get; set; }

    public string? UserID { get; set; }

    public long DM_StatusID { get; set; }

    public long Next_DM_StatusID { get; set; }

    public long? SendBack_DM_StatusID { get; set; }

    public int? CanAddEmailApprovalLink { get; set; }

    public int? IsDisabled { get; set; }
}
