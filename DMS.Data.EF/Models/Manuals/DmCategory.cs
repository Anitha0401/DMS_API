using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Manuals;

[Table("DM_Category")]
public partial class DmCategory
{
    [Key]
    public long DM_CategoryID { get; set; }

    [StringLength(3)]
    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    [StringLength(50)]
    public string? CategoryCode { get; set; }

    [StringLength(250)]
    public string CategoryName { get; set; } = null!;

    public int? IsDisabled { get; set; }
}
