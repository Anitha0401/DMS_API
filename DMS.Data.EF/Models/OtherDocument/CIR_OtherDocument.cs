using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.OtherDocument;

[Table("CIR_OtherDocument")]
public partial class CIR_OtherDocument
{
    [Key]
    [Column("CIR_OtherDocumentID")]
    public long CIR_OtherDocumentID { get; set; }

    [StringLength(3)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("CIR_OtherDocument_TypeID")]
    public int? CIR_OtherDocument_TypeID { get; set; }

    [StringLength(50)]
    public string DocumentNumber { get; set; } = null!;

    [StringLength(500)]
    public string DocumentTitle { get; set; } = null!;

    [StringLength(50)]
    public string? Status { get; set; }

    [StringLength(150)]
    public string? Reference { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UploadedOn { get; set; }

    [StringLength(150)]
    public string? UploadedBy { get; set; }

    public int? IsDeleted { get; set; }

}