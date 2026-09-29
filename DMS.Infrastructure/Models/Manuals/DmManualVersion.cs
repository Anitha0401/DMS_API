using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.Manuals;

[Table("DM_ManualVersion")]
public partial class DmManualVersion
{
    [Key]
    public long DM_ManualVersionID { get; set; }

    [StringLength(5)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long DM_ManualID { get; set; }

    [StringLength(15)]
    public string Version { get; set; } = null!;

    public string? TextContents { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? PublishedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? PublishedDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? ReleasedBy { get; set; }

    public DateTime? ReleasedDate { get; set; }

    public int? IsActive { get; set; }

    public int? IsDisabled { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? LastModifiedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastModifiedDate { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? Comments { get; set; }
}
