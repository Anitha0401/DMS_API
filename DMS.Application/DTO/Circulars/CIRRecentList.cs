using Microsoft.EntityFrameworkCore;

namespace DMS.Application.DTO.Circulars;

[Keyless]
public class CIRRecentList
{
    public long CIR_MasterID { get; set; }

    public string? Category { get; set; }

    public string CIR_Number { get; set; } = null!;

    public string? Title { get; set; } = null!;

    public string? StatusString { get; set; }

    public string? Priority { get; set; }

    public string? DateIssued { get; set; }
}