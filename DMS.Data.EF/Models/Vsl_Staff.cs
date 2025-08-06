using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class Vsl_Staff
{
    public long Vsl_StaffID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string UserID { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public byte[] UserRank { get; set; } = null!;

    public string? UserDept { get; set; }

    public int? IsDisabled { get; set; }
}
