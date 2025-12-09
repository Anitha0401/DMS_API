using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.DTO;

[Keyless]
public class Manual_Content
{
    public string? ManualContent { get; set; } = null!;
}
