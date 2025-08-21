using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class V_Vessel
{
    public long V_VesselID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string VslCode { get; set; } = null!;

    public string VslName { get; set; } = null!;

    public string? VslType { get; set; } = null!;
}
