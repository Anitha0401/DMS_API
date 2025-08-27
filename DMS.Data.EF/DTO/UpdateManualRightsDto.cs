namespace DMS.Data.EF.DTO;

public class UpdateManualRightsDto
{
    public long DM_ManualID { get; set; } = -1;

    public List<int>? VesselIDList { get; set; }

    public List<string>? VslRankList { get; set; }

    public List<int>? HQUserIDList { get; set; }
}
