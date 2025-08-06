namespace DMS.Data.EF.DTO;

public class EditManualVersionDto : BaseDto
{
    public string TextContents { get; set; } = string.Empty;

    public string CalledMode { get; set; } = string.Empty;
}
