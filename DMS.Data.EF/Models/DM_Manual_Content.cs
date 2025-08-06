using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Keyless]
public class DM_Manual_Content
{
    public string? ManualContent { get; set; } = null!;
}
