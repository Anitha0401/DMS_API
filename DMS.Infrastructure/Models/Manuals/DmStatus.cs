using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.Manuals;

[Table("DM_Status")]
public partial class DmStatus
{
    [Key]
    [Column("DM_StatusID")]
    public long DmStatusId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? StatusCode { get; set; }

    [StringLength(150)]
    [Unicode(false)]
    public string StatusName { get; set; } = null!;

    public int? IsDisabled { get; set; }

    public int? IsVisible { get; set; }

    public int? SortOrder { get; set; }
}
