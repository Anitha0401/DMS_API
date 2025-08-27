namespace DMS.Data.EF.DTO;

public class CreateManualDto
{
    public required long DM_CategoryID { get; set; }

    public string? ManualCode { get; set; }

    public required string ManualName { get; set; }

    public string TextContents { get; set; } = string.Empty;

    public long CalledManualID { get; set; } = -1;

    public string Comments { get; set; } = string.Empty;

    public string? CreatedBy { get; set; }

    public string CalledMode { get; set; } = string.Empty;
}
