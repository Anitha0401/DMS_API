using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.Circulars;

[Table("CIR_ToAck_Vsl")]
public partial class CIR_ToAck_Vsl
{
    [Key]
    [Column("CIR_ToAck_VslID")]
    public long CIR_ToAck_VslID { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("CIR_MasterID")]
    public long CIR_MasterID { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string UserRank { get; set; } = null!;

    public int? IsActive { get; set; }
}
