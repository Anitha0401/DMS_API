using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.DTO;

[Keyless]
public class Manual_Versions
{
    public long DM_ManualVersionID { get; set; }

    public string? Version { get; set; } = null!;
}
