namespace DMS.Application.DTO.Manuals;

public class CreateManualDto
{
    public required long DM_CategoryID { get; set; }

    public string? ManualCode { get; set; }

    public required string ManualName { get; set; }

    public string TextContents { get; set; } = string.Empty;

    public int? IsMainTitle { get; set; } = 0;

    public int? IsAcknowledgementRequired { get; set; } = 0;

    public int CanExport { get; set; } = 0;

    public int IsApprovalRequired { get; set; } = 0;

    public int? Approval_FlowID { get; set; } = 0;

    public long CalledManualID { get; set; } = -1;

    public string Comments { get; set; } = string.Empty;

    public string? CreatedBy { get; set; }

    public bool IsToPublish { get; set; } = false;

    public string CalledMode { get; set; } = string.Empty;
}
