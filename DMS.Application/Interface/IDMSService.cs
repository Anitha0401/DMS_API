using DMS.Data.EF.DTO;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;

namespace DMS.Application.Interface;

public interface IDMSService
{
    public Task<List<Manual_TreeviewDto>> GetTreeViewManualListAsync();

    public Task<string?> GetManualContent(long dm_ManualID, bool includeSubManuals);

    public Task<ManualContentDto?> GetManualDetails(long dm_ManualID);

    public Task<List<ManualCategoryDto>> GetManualCategory();

    public Task<List<VesselListDto>> GetVesselList();

    public Task<List<VslRankListDto>> GetVslRankList();

    public Task<List<HQUserListDto>> GetHQUserList();

    public Task<List<DM_Manual_Attachments>> GetManualAttachmentListAsync(long dm_ManualID);

    public Task<List<Dm_ManualVersion_View>> GetDmManualsAsync();

    public Task<Dm_ManualDetails> GetDmManualDetailsAsync(long dm_ManualID);

    public Task<Dm_ManualRights> GetDmManualRightsAsync(long dm_ManualID);

    public Task<CreateManualResultDto> AddDmManualAsync(CreateManualDto request);

    public Task<bool> UpdateDmManualAsync(EditManualDto request);
    
    public Task<bool> ApproveManualAsync(ApproveManualDto request);

    public Task DeleteDmManualAsync(long dm_ManualID);

    public Task DeleteDmManualVersionAsync(long dm_ManualID);
}
