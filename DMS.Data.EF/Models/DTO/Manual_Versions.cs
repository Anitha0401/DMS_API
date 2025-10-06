using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Keyless]
public class Manual_Versions
{
    public long DM_ManualVersionID { get; set; }

    public string? Version { get; set; } = null!;
}
