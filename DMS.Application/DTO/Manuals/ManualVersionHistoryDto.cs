namespace DMS.Application.DTO.Manuals;

/// <summary>One row of a manual's version history (Version History dialog).</summary>
public class ManualVersionHistoryDto
{
    public long DM_ManualVersionID { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? ReleasedBy { get; set; }
    public DateTime? ReleasedDate { get; set; }
    public bool IsActive { get; set; }
}
