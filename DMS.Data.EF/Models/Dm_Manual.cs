using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_Manual
{
    public long DM_ManualID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long DM_CategoryID { get; set; }

    public string? ManualNo { get; set; }

    public string? ManualCode { get; set; }

    public string ManualName { get; set; } = null!;

    public long DM_StatusID { get; set; }

    public long ParentID { get; set; }

    public int ManualLevel { get; set; }

    public int? CanExport { get; set; }

    public string? SortOrder { get; set; }
  
    public string? AddlSortOrder { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? IsDeleted { get; set; }
}
