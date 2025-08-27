using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class Dm_ManualVersion_View
{
    public long DM_ManualID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string? DM_ManualCategory { get; set; }

    public long DM_CategoryID { get; set; }

    public string? ManualNo { get; set; }

    public string? ManualCode { get; set; }

    public string ManualName { get; set; } = null!;

    public string? DM_Manual_Status { get; set; }

    public long DM_StatusID { get; set; }

    public string? Version { get; set; }

    public string? TextContents { get; set; } = null!;

    public string? ReleasedBy { get; set; }

    public DateTime? ReleasedDate { get; set; }

    public string? FinalisedBy { get; set; }

    public DateTime? FinalisedDate { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public long ParentID { get; set; }

    public int ManualLevel { get; set; }

    public int? CanExport { get; set; }

    public string? SortOrder { get; set; }

    public string? AddlSortOrder { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? Comments { get; set; }

    public int? IsDeleted { get; set; }

    public int IsActive { get; set; }

    public int IsCurrent { get; set; }
}
