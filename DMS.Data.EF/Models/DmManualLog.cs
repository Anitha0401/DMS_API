using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Table("DM_Manual_Log")]
public partial class DmManualLog
{
    [Key]
    [Column("DM_Manual_LogID")]
    public long DmManualLogId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("DM_ManualID")]
    public long DM_ManualID { get; set; }

    [Column("DM_ManualVersionID")]
    public long DM_ManualVersionID { get; set; }

    [StringLength(120)]
    [Unicode(false)]
    public string Activity { get; set; } = null!;

    [StringLength(3000)]
    [Unicode(false)]
    public string Remarks { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? Logdate { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? UserRank { get; set; }

    [StringLength(250)]
    [Unicode(false)]
    public string? UserName { get; set; }
}
