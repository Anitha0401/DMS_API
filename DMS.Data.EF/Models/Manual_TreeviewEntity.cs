using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.Models;

public class Manual_TreeviewEntity
{
    public long DM_ManualID { get; set; }

    public long DM_ManualVersionID { get; set; }

    public string ManualNo { get; set; } = null!;

    public string? ManualCode { get; set; }

    public string ManualName { get; set; } = null!;

    public string? Version { get; set; }
    
    public string? StatusString { get; set; }

    public long ParentID { get; set; }

    public int ManualLevel { get; set; }

    public string? SortOrder { get; set; }

    public string? AddlSortOrder { get; set; }
}
