using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.DTO.Manuals;

public class Manual_TreeviewDto
{
    public long Key { get; set; }

    public string Label { get; set; } = null!;

    [NotMapped]
    public TreeviewDataDto data { get; set; } = null!;

    public List<Manual_TreeviewDto> Children { get; set; } = new List<Manual_TreeviewDto>();
}

public class TreeviewDataDto
{
    public long DM_ManualID { get; set; }
    
    public long DM_ManualVersionID { get; set; }

    public string? ManualVersion { get; set; }

    public string? StatusString { get; set; }

    public string? Category { get; set; }

    public string? LastUpdated { get; set; }

    public int? IsActive { get; set; }

    public long DM_StatusID { get; set; }

    public long ParentID { get; set; }

    public int ManualLevel { get; set; }

    public int IsFavourite { get; set; }

    public int IsMainTitle { get; set; }
}
