using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Manuals;

[Table("DM_Manual_Attachments")]
public partial class DmManualAttachment
{
    [Key]
    [Column("DM_Manual_AttachmentsID")]
    public long DmManualAttachmentsId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    public long DM_ManualID { get; set; }

    public int? ImageRefNo { get; set; }

    [Column(TypeName = "image")]
    public byte[]? BlobContents { get; set; }

    public int BlobSize { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string FileType { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? AddedOn { get; set; }

    public int? IsDeleted { get; set; }
}
