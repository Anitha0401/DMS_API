using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.Models;

public class Manual_TreeviewDto
{
    public long Key { get; set; }

    public string Label { get; set; } = null!;

    [NotMapped]
    public TreeviewDataDto data { get; set; } = null!;

    public List<Manual_TreeviewDto> Children { get; set; } = new List<Manual_TreeviewDto>();
}

public class TreeviewDataDto
{
    public long DM_ManualID { get; set; }
    
    public long DM_ManualVersionID { get; set; }

    public string? ManualVersion { get; set; }

    public string? StatusString { get; set; }

    public int? IsActive { get; set; }

    public long DM_StatusID { get; set; }

    public long ParentID { get; set; }

    public int ManualLevel { get; set; }

    public int IsFavourite { get; set; }

    public int IsMainTitle { get; set; }
}
