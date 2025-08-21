using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class Dm_ManualDetails
{
    public long DM_ManualID { get; set; }

    public long DM_ManualVersionID { get; set; }

    public int VesselID { get; set; }

    public long DM_CategoryID { get; set; }

    public string? ManualNo { get; set; }

    public string? ManualCode { get; set; }

    public string ManualName { get; set; } = null!;

    public string? Version { get; set; }

    public string? TextContents { get; set; } = null!;

    public string StatusString { get; set; } = null!;

    public long DM_StatusID { get; set; }

    public int? CanExport { get; set; }

    public string? Comments { get; set; } = null!;

    public List<int>? VesselIdList { get; set; }

    public List<string>? VesselRankList { get; set; }
}
