namespace DMS.Application.DTO.Manuals;

public class AssignManualRightsDto
{
    public List<VesselListDto> VesselList { get; set; } = null!;
 
    public List<VslRankListDto> VslRankList { get; set; } = null!;

    public List<HQUserListDto> HqUserList { get; set; } = null!;
}
