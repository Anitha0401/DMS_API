using DMS.Data.EF.DTO;

namespace DMS.Data.EF.x;

public class EditManualVersionDto : BaseDto
{
    public string TextContents { get; set; } = string.Empty;

    public string CalledMode { get; set; } = string.Empty;
}
