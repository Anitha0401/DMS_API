namespace DMS.Data.EF.Query;

public class CreateManualDto
{
    public long VesselID { get; set; } = -1;

    public string Origin { get; set; } = "HQ";
   
    public required long DM_CategoryID { get; set; }

    public long DM_StatusID { get; set; }

    public string? ManualCode { get; set; }

    public required string ManualName { get; set; }

    public string TextContents { get; set; } = string.Empty;

    public long ParentID { get; set; } = -1;

    public int ManualLevel { get; set; } = 0;

    public int CanExport { get; set; } = 0;

    public string Comments { get; set; } = string.Empty;

    public int IsActive { get; set; } = 0;

    public int IsCurrent { get; set; } = 0;

    public DateTime ReleasedDate { get; set; }

    public string? ReleasedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public int IsDeleted { get; set; }

    public string CalledMode { get; set; } = string.Empty;

    public List<int>? VesselIDList { get; set; }

    public List<string>? VslRankList { get; set; }
}
