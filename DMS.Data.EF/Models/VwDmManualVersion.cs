using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Keyless]
public partial class VwDmManualVersion
{
    [Column("DM_ManualID")]
    public long DM_ManualID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    [Column("DM_CategoryID")]
    public long? DM_CategoryID { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string ManualNo { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string? ManualCode { get; set; }

    [StringLength(250)]
    [Unicode(false)]
    public string? ManualName { get; set; }

    [Column("DM_StatusID")]
    public long DmStatusId { get; set; }

    [Column("ParentId")]
    public long ParentId { get; set; }

    public int ManualLevel { get; set; }

    public int? IsMainTitle { get; set; }

    public int? IsAcknowledgementRequired { get; set; } = 0;

    public int CanExport { get; set; }

    public int IsApprovalRequired { get; set; }

    public int? Approval_FlowID { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? SortOrder { get; set; }

    [StringLength(250)]
    [Unicode(false)]
    public string? AddlSortOrder { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? LastModifiedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastModifiedDate { get; set; }

    public int? IsDeleted { get; set; }

    [Column("DM_ManualVersionID")]
    public long DM_ManualVersionID { get; set; }

    [StringLength(15)]
    public string Version { get; set; } = null!;

    public string? TextContents { get; set; }

    public DateTime? ReleasedDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? ReleasedBy { get; set; }

    public int? IsActive { get; set; }

    public int? IsDisabled { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? PublishedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? PublishedDate { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? Comments { get; set; }
}
