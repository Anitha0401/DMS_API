namespace DMS.Application.DTO.Manuals;

public partial class ManualRightsDto
{
    public long DM_ManualID { get; set; }

    public List<int>? VesselIdList { get; set; }

    public List<string>? VesselRankList { get; set; }

    public List<long>? HQ_UsersIDList { get; set; }
}
