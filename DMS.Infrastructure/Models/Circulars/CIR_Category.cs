using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.Circulars;

[Table("CIR_Category")]
public partial class CIR_Category
{
    [Key]
    [Column("CIR_CategoryID")]
    public long CIR_CategoryID { get; set; }

    [StringLength(3)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [StringLength(15)]
    public string CircularType { get; set; } = null!;

    [StringLength(10)]
    public string CategoryCode { get; set; } = null!;

    [StringLength(50)]
    public string? Category { get; set; }

    public int? isActive { get; set; }
    
    [StringLength(10)]
    public string? Sortorder { get; set; }
}
