namespace DMS.Data.EF.DTO;

public class EditManualDto : BaseDto
{
    public required long DM_CategoryID { get; set; }

    public string? ManualCode { get; set; }

    public string TextContents { get; set; } = string.Empty;

    public string CalledMode { get; set; } = string.Empty;

    public List<int>? VesselIdList { get; set; }

    public List<string>? VesselRankList { get; set; }
}
