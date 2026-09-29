using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Application.DTO.Manuals;

public class Manual_FilterDto
{
    public long Key { get; set; }

    public string Label { get; set; } = null!;

    [NotMapped]
    public TreeviewDataDto data { get; set; } = null!;
}
