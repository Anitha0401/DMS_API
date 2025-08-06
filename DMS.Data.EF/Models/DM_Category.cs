using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_Category
{
    public long DM_CategoryID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string? CategoryCode { get; set; }

    public string CategoryName { get; set; } = null!;

    public int? IsDisabled { get; set; }
}
