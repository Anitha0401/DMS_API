using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.OtherDocument;

[Table("CIR_OtherDocument_Type")]
public partial class CIR_OtherDocument_Type
{
    [Key]
    [Column("CIR_OtherDocument_TypeID")]
    public int CIR_OtherDocument_TypeID { get; set; }

    [StringLength(3)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [StringLength(25)]
    public string DocumentCode { get; set; } = null!;

    [StringLength(150)]
    public string? DocumentType { get; set; }

    public int? IsActive { get; set; }

    public int SortOrder { get; set; }
}