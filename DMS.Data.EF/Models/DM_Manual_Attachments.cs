using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class DM_Manual_Attachments
{
    public long DM_Manual_AttachmentsID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long DM_ManualID { get; set; }

    public int? ImageRefNo { get; set; }

    public byte[]? BlobContents { get; set; }

    public int BlobSize { get; set; }

    public string FileType { get; set; } = null!;

    public DateTime? AddedOn { get; set; }
    public int IsDeleted { get; set; }
}
