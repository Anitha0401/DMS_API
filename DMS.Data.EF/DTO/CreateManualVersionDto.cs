namespace DMS.Data.EF.DTO;

public class CreateManualVersionDto
{
    public required long DM_ManualID { get; set; }

    public string TextContents { get; set; } = null!;

    public string Comments { get; set; } = string.Empty;

    public string? CreatedBy { get; set; }

    public string CalledMode { get; set; } = string.Empty;
}
