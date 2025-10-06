using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Table("V_Vessel")]
public partial class VVessel
{
    [Key]
    [Column("V_VesselID")]
    public long VVesselId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [StringLength(5)]
    [Unicode(false)]
    public string VslCode { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string VslName { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string? VslType { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DelivDate { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string? BaseCurrency { get; set; }

    [StringLength(150)]
    [Unicode(false)]
    public string? VslEmailAddress { get; set; }

    [StringLength(25)]
    [Unicode(false)]
    public string? OperateBy { get; set; }

    public int? IsRetired { get; set; }
}
