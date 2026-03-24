using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.OtherDocument;

[Table("CIR_OtherDocument_Blob")]
public partial class CIR_OtherDocument_Blob
{
    [Key]
    [Column("CIR_OtherDocument_BlobID")]
    public long CIR_OtherDocument_BlobID { get; set; }

    [Column("CIR_OtherDocumentID")]
    public long CIR_OtherDocumentID { get; set; }

    [StringLength(3)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [StringLength(150)]
    public string? Description { get; set; }

    [StringLength(150)]
    public string? FileName { get; set; }

    [StringLength(50)]
    public string? FileType { get; set; }

    [StringLength(50)]
    public string? OriginalFileType { get; set; }

    public int BlobSize { get; set; }

    public byte[]? BlobContents { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? AddedOn { get; set; }

    [StringLength(150)]
    [Unicode(false)]
    public string? Cloud_ContainerName { get; set; }

    [StringLength(150)]
    [Unicode(false)]
    public string? Cloud_Directory { get; set; }

    [StringLength(50)]
    public string? HQ_Storage_Option { get; set; }

    [StringLength(150)]
    public string? HQ_DatabaseName { get; set; }

    [StringLength(150)]
    public string? HQ_Local_Folder { get; set; }

    [StringLength(50)]
    public string? VSL_Storage_Option { get; set; }

    [StringLength(150)]
    public string? VSL_DatabaseName { get; set; }

    [StringLength(150)]
    public string? VSL_Local_Folder { get; set; }
}