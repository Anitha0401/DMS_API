using Microsoft.EntityFrameworkCore;

namespace DMS.Application.DTO.Manuals;

[Keyless]
public class Manual_Content
{
    public string? ManualContent { get; set; } = null!;
}
