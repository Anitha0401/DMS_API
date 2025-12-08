using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Circulars;

[Table("CIR_UserAck")]
public partial class CIR_UserAck
{
    [Key]
    [Column("CIR_MasterID")]
    public long CIR_MasterID { get; set; }

    [StringLength(3)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("DM_CategoryID")]
    public long? DM_CategoryID { get; set; }

    [StringLength(10)]
    public string ManualNo { get; set; } = null!;

    [StringLength(20)]
    public string? ManualCode { get; set; }

    [StringLength(250)]
    public string? ManualName { get; set; }

    [Column("DM_StatusID")]
    public long DmStatusId { get; set; }

    [Column("ParentId")]
    public long ParentId { get; set; }

    public int ManualLevel { get; set; }

    public int? IsMainTitle { get; set; }

    public int? IsAcknowledgementRequired { get; set; }

    public int CanExport { get; set; }

    public int IsApprovalRequired { get; set; }

    public int? Approval_FlowID { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? NextApproval_HQRole { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? NextApproval_HQDept { get; set; }

    [StringLength(50)]
    public string? SortOrder { get; set; }

    [StringLength(250)]
    public string? AddlSortOrder { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDate { get; set; }

    public int? IsDeleted { get; set; }
}
