using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Table("DM_Manual_UserAck")]
public partial class DmManualUserAck
{
    [Key]
    [Column("DM_Manual_UserAckID")]
    public long DmManualUserAckId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("DM_ManualID")]
    public long DM_ManualID { get; set; }

    [Column("DM_ManualVersionID")]
    public long DM_ManualVersionID { get; set; }

    public int Version { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? UserRank { get; set; }

    [StringLength(250)]
    [Unicode(false)]
    public string? UserName { get; set; }

    [Column("UserID")]
    [StringLength(150)]
    [Unicode(false)]
    public string UserId { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? DateRead { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? Comments { get; set; }

    public int? IsActive { get; set; }
}
