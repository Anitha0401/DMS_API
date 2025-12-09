namespace DMS.Data.EF.Models.DTO;

public partial class ManualDetails
{
    public long DM_ManualID { get; set; }

    public long DM_ManualVersionID { get; set; }

    public int VesselID { get; set; }

    public long? DM_CategoryID { get; set; }

    public long DM_StatusID { get; set; }

    public string? CategoryName { get; set; }

    public string? ManualNo { get; set; }

    public string? ManualCode { get; set; }

    public string? ManualName { get; set; } = null!;

    public string? Version { get; set; }

    public string StatusString { get; set; } = null!;

    public string? TextContents { get; set; } = null!;

    public int? IsMainTitle { get; set; }

    public int? IsAcknowledgementRequired { get; set; }
    
    public int CanExport { get; set; }

    public int IsApprovalRequired { get; set; }

    public int? Approval_FlowID { get; set; }

    public string? Comments { get; set; } = null!;

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? ReleasedBy { get; set; }

    public DateTime? ReleasedDate { get; set; }

    public string? PublishedBy { get; set; }

    public DateTime? PublishedDate { get; set; }
    
    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }
}