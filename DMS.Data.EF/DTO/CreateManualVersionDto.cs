namespace DMS.Data.EF.Query;

public class CreateManualVersionDto
{
    public required long DM_ManualID { get; set; }
    
    public int VesselID { get; set; } = 0;

    public string Origin { get; set; } = "";

    public string TextContents { get; set; } = null!;

    public string Comments { get; set; } = string.Empty;

    public int IsActive { get; set; } = 0;

    public int IsCurrent { get; set; } = 0;

    public DateTime ReleasedDate { get; set; }

    public string? ReleasedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public int IsDeleted { get; set; }
}
