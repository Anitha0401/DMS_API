using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.OtherDocument;

[Table("CIR_OtherDocument_Log")]
public partial class CIR_OtherDocument_Log
{
    [Key]
    [Column("CIR_OtherDocument_LogID")]
    public long CIR_OtherDocument_LogID { get; set; }

    [StringLength(3)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("CIR_OtherDocumentID")]
    public long? CIR_OtherDocumentID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LogDate { get; set; }

    [StringLength(150)]
    public string Activity { get; set; } = null!;

    [StringLength(3000)]
    public string? Remarks { get; set; }

    [StringLength(150)]
    public string? UserId { get; set; }

    [StringLength(150)]
    [Unicode(false)]
    public string? UserName { get; set; }

    public long? Fleet_CrewId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? UserRank { get; set; }
}
