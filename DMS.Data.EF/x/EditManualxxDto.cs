using DMS.Data.EF.DTO;

namespace DMS.Data.EF.x;

public class EditManualxxDto : BaseDto
{
    public required long DM_CategoryID { get; set; }

    public string? ManualCode { get; set; }

    public string TextContents { get; set; } = string.Empty;

    public string CalledAction { get; set; } = string.Empty; 
}
