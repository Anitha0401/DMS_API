using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.DTO.Manuals;

[Keyless]
public class Manual_Content
{
    public string? ManualContent { get; set; } = null!;
}
