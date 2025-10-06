using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Table("HQ_UserRoles")]
public partial class HqUserRole
{
    [Key]
    [Column("HQ_UserRolesID")]
    public long HqUserRolesId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("HQDept")]
    [StringLength(10)]
    [Unicode(false)]
    public string Hqdept { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string UserRole { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string? UserDesignation { get; set; }

    public int? ApprovalOrder { get; set; }

    public int? IsDisabled { get; set; }
}
