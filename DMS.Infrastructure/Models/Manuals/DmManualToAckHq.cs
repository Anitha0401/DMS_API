using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.Manuals;

[Table("DM_Manual_ToAck_HQ")]
public partial class DmManualToAckHq
{
    [Key]
    [Column("DM_Manual_ToAck_HQID")]
    public long DmManualToAckHqid { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("DM_ManualID")]
    public long DM_ManualID { get; set; }

    [Column("HQ_UsersID")]
    public long HqUsersId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Role { get; set; } = null!;

    public int? IsActive { get; set; }
}
