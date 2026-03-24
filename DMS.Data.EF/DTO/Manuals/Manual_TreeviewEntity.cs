namespace DMS.Data.EF.DTO.Manuals;

public class Manual_TreeviewEntity
{
    public long DM_ManualID { get; set; }

    public long DM_ManualVersionID { get; set; }

    public string ManualNo { get; set; } = null!;

    public string? ManualCode { get; set; }

    public string ManualName { get; set; } = null!;

    public string? Version { get; set; }
    
    public string? StatusString { get; set; }

    public string? CategoryName { get; set; }

    public string? LastUpdated { get; set; }

    public long ParentID { get; set; }

    public int ManualLevel { get; set; }

    public int? IsActive { get; set; }

    public long DM_StatusID { get; set; }

    public string? SortOrder { get; set; }

    public string? AddlSortOrder { get; set; }

    public int IsFavourite { get; set; }

    public int IsMainTitle { get; set; }
}
