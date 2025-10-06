using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Table("DM_Manual_ApprovalSettings")]
public partial class DmManualApprovalSetting
{
    [Key]
    [Column("DM_Manual_ApprovalSettingsID")]
    public long DmManualApprovalSettingsId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("DM_CategoryID")]
    public long DM_CategoryID { get; set; }

    [Column("HQDept")]
    [StringLength(10)]
    [Unicode(false)]
    public string Hqdept { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string UserRole { get; set; } = null!;

    [Column("UserID")]
    [StringLength(50)]
    [Unicode(false)]
    public string? UserId { get; set; }

    [Column("DM_StatusID")]
    public long DmStatusId { get; set; }

    [Column("Next_DM_StatusID")]
    public long NextDmStatusId { get; set; }

    [Column("SendBack_DM_StatusID")]
    public long? SendBackDmStatusId { get; set; }

    public int? CanAddEmailApprovalLink { get; set; }

    public int? IsDisabled { get; set; }
}
