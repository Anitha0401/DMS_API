namespace DMS.Application.DTO.Manuals;

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

/// <summary>
/// Row of GetManual_TreeViewList_Filtered: the same columns as the tree plus IsMatch.
/// A separate class (not derived from Manual_TreeviewEntity) so EF does not treat the two as an inheritance pair.
/// </summary>
public class Manual_TreeviewFilteredEntity
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

    /// <summary>1 = matches the link (selectable); 0 = parent shown only for context (disabled).</summary>
    public int IsMatch { get; set; }

    public Manual_TreeviewEntity ToTreeRow() => new()
    {
        DM_ManualID = DM_ManualID, DM_ManualVersionID = DM_ManualVersionID, ManualNo = ManualNo, ManualCode = ManualCode,
        ManualName = ManualName, Version = Version, StatusString = StatusString, CategoryName = CategoryName,
        LastUpdated = LastUpdated, ParentID = ParentID, ManualLevel = ManualLevel, IsActive = IsActive,
        DM_StatusID = DM_StatusID, SortOrder = SortOrder, AddlSortOrder = AddlSortOrder,
        IsFavourite = IsFavourite, IsMainTitle = IsMainTitle
    };
}
