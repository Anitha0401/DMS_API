namespace DMS.Data.EF.DTO.Circulars;

public class EditCirculars : CircularsBaseDto
{
    public required long DM_CategoryID { get; set; }

    public string? ManualCode { get; set; }

    public string TextContents { get; set; } = string.Empty;

    public int? IsMainTitle {  get; set; } = 0;
    
    public int? IsAcknowledgementRequired {  get; set; } = 0;

    public int CanExport { get; set; } = 0;

    public int IsApprovalRequired { get; set; } = 0;

    public int? Approval_FlowID { get; set; } = 0;

    public string CalledMode { get; set; } = string.Empty;

    public bool IsToPublish { get; set; } = false;
}
