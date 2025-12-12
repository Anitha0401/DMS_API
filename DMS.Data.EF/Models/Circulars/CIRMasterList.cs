using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Circulars;

[Keyless]
public class CIRMasterList
{
    public long? CIR_CategoryID { get; set; }

    public long CIR_MasterID { get; set; }

    public long CIR_BlobId { get; set; }

    public string? CircularType { get; set; }

    public string? Category { get; set; }

    public string CIR_Number { get; set; } = null!;

    public string? Reference { get; set; }

    public string? Title { get; set; } = null!;

    public string? StatusString { get; set; }

    public string? Priority { get; set; }

    public string? DateIssued { get; set; }
    
    public string? ReleasedDate { get; set; }

    public int CIRLevel { get; set; }

    public int AttachmentCount { get; set; }

    public int? IsActive { get; set; }

    public int IsFavourite { get; set; }

    public int IsCategory { get; set; }
}