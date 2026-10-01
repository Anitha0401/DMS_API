using DMS.Application.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;

namespace DMS.Application.Interface;

public interface ICIRService
{
    public Task<List<Circulars_TreeviewDto>> GetTreeViewCircularsListAsync(string userID, string circularType);
   
    public Task<List<CIRMasterList>> GetCIRMasterListAsync(string userID, long categoryId, string circularType);
  
    public Task<List<CircularCategoryDto>> GetCategoriesAsync(bool isToIncludeAll, string? circularType = null);

    public Task<List<CircularsChart>> GetCIR_CategoryChartDataAsync(string circularType);

    public Task<List<CIRRecentList>> GetRecentCircularsAsync(string circularType, int limit = 10);
    
    public Task<CircularCount_Dashboard> GetDashboardCircularCountsAsync();
    
    public Task<List<CircularsNotificationDto>> GetCIRNotificationListAsync(int limit);

    public Task<List<CircularUserAckDto>> GetVesselAckListAsync(long cir_MasterID);

    public Task<GetNextCIRNumberResponseDto> GetNextCIRNumbersByCategoryAsync(long categoryId, long cir_MasterID);

    public Task<EditCircularDto> GetCIRDetailsAsync(long cirMasterId);

    public Task<List<CIR_LogDto>> GetCircularLogsAsync(long cirMasterId);


    public Task RemoveTempCIRAsync(long cir_MasterID);

    public Task<long> InsertCIR_MasterRecord(string circularType);

    public Task<long> CreateCircularAsync(CreateCircularDto createDto);

    public Task AddToFavouriteAsync(long cir_MasterID, string circularType, string userID);

    public Task<long> UpdateCircularAsync(EditCircularDto editDto);

    public Task<bool> MarkNotificationReadAsync(long notificationID);
}
