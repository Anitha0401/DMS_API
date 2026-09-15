using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.DTO.OtherDocument;

[Keyless]
public class OtherDocumentRecentList
{
    public long CIR_MasterID { get; set; }

    public string? Category { get; set; }

    public string CIR_Number { get; set; } = null!;

    public string? Title { get; set; } = null!;

    public string? StatusString { get; set; }

    public string? Priority { get; set; }

    public string? DateIssued { get; set; }
}