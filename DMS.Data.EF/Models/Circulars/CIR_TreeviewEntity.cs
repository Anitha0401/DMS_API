using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Circulars;

[Keyless]
public class CIR_TreeviewEntity
{
    public long CIR_CategoryID { get; set; }

    public long CIR_MasterID { get; set; }

    public long CIR_BlobId { get; set; }

    public string? Category { get; set; }

    public string CIR_Number { get; set; } = null!;

    public string? Reference { get; set; }

    public string Title { get; set; } = null!;

    public string? StatusString { get; set; }

    public string? DateIssued { get; set; }

    public long ParentID { get; set; }

    public int CIRLevel { get; set; }

    public int? IsActive { get; set; }

    public int IsFavourite { get; set; }
}
