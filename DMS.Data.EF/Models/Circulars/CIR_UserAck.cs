using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.Circulars;

[Table("CIR_UserAck")]
public partial class CIR_UserAck
{
    [Key]
    [Column("CIR_UserAckID")]
    public long CIR_UserAckID { get; set; }

    [StringLength(3)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("CIR_MasterID")]
    public long? CIR_MasterID { get; set; }

    [Column("Fleet_CrewID")]
    public long? Fleet_CrewID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DateRead { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }

    public int? IsActive { get; set; }
}
