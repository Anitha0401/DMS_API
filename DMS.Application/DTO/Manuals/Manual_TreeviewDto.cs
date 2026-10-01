using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Application.DTO.Manuals;

public class Manual_TreeviewDto
{
    public long Key { get; set; }

    public string Label { get; set; } = null!;

    [NotMapped]
    public TreeviewDataDto data { get; set; } = null!;

    public List<Manual_TreeviewDto> Children { get; set; } = new List<Manual_TreeviewDto>();

    /// <summary>PrimeReact Tree reads this: false = node shown but cannot be selected.</summary>
    public bool Selectable { get; set; } = true;

    /// <summary>CSS class for the node; "tree-node-context" greys out parents shown only for context.</summary>
    public string? ClassName { get; set; }
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

    /// <summary>False when the node is only shown as the parent of a filtered result.</summary>
    public bool IsMatch { get; set; } = true;
}
