using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.Manuals;

[Keyless]
[Table("DM_ApprovalAndMailSetup")]
public partial class DmApprovalAndMailSetup
{
    [Column("DM_ApprovalAndMailSetupID")]
    public long DmApprovalAndMailSetupId { get; set; }

    [StringLength(50)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("FlowID")]
    public int FlowId { get; set; }

    [Column("HQDept")]
    [StringLength(50)]
    public string HQDept { get; set; } = null!;

    [StringLength(50)]
    public string UserRole { get; set; } = null!;

    [Column("DM_StatusID")]
    public long? DmStatusId { get; set; }

    [Column("Next_DM_StatusID")]
    public long? NextDmStatusId { get; set; }

    [Column("SendBack_DM_StatusID")]
    public long? SendBackDmStatusId { get; set; }

    public int? IsToSendMail { get; set; }

    public int? ApprovalOrder { get; set; }
}
