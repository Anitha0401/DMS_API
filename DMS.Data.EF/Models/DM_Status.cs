using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_Status
{
    public long DM_StatusID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string? StatusCode { get; set; }

    public string StatusName { get; set; } = null!;

    public int? IsDisabled { get; set; }

    public int? SortOrder { get; set; }
}
