using DMS.Data.EF.DTO;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;

namespace DMS.Application.Interface;

public interface IDMSService
{
    public Task<List<Manual_TreeviewDto>> GetTreeViewManualListAsync(string userID);

    public Task<string?> GetManualContent(long dm_ManualID, bool includeSubManuals);

    public Task<List<ManualVersionDetailsDto>> GetManualVersionDetailsAsync(long dm_ManualID);

    public Task<UserManualCount?> GetUserManualCountAsync(string userID);

    public Task<CompareVersionDetailsDto> GetCompareVersionDetailsAsync(long dm_ManualID, long manualVersionIDToCompare);

    public Task<List<ManualCategoryDto>> GetManualCategory();

    public Task<List<ApprovalFlowListDto>> GetApprovalFlowList();

    public Task<List<ApprovalDetailsDto>> GetApprovalDetailsForFlowID(int flowID);

    public Task<List<DmManualAttachment>> GetManualAttachmentListAsync(long dm_ManualID);

    public Task<ManualDetails> GetManualDetailsByVersionAsync(long DM_ManualVersionID);

    public Task<Manual_SimpleDetails> GetManualSimpleDetailsAsync(long dm_ManualID);

    public Task<List<ManualDetails>> GetManualListBasedOnCalledModeAsync(string userID, string calledMode);
   
    public Task<List<ManualDetails>> GetUserFavouiteManualListAsync(string userID);

    public Task<AssignManualRightsDto> GetManualRightsUsersAsync();

    public Task<ManualRightsDto> GetDmManualAssignedRightsAsync(long dm_ManualID);

    public Task<List<ManualUserAckList>> GetManualUserAckListAsync(long dm_ManualID);
    
    public Task UpdateManualRights(ManualRightsDto request);
    
    public Task<CreateManualResultDto> AddDmManualAsync(CreateManualDto request);

    public Task<CreateManualResultDto> UpdateDmManualAsync(EditManualDto request);
    
    public Task<bool> ApproveManualAsync(ApproveManualDto request, bool isToSendBack);

    public Task DeleteDmManualAsync(long dm_ManualID);

    public Task DeleteDmManualVersionAsync(long dm_ManualID);

    public Task AddToFavouriteAsync(long dm_ManualID, string userID);
}
