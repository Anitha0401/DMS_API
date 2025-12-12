using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.DTO;

namespace DMS.Data.EF.Query;

public interface ICIRGeneralQueries
{
    public Task<List<CIR_TreeviewEntity>> GetCIRTreeviewAsync(string userID, string circularType);

    public Task<List<CIRMasterList>> GetCIRListAsync(string userID, long cir_CategoryID, string circularType);

    public Task<List<CircularsChart>> GetCIRCategoryBasedChartDataAsync(string circularType);

    public Task<List<CIRRecentList>> GetRecentCircularsAsync(string circularType, int limit);

    public Task<CircularCount_Dashboard> GetCIRDashboardCountsAsync();

    public Task<List<VwCIRNotification>> GetNotificationList();

    public Task<List<CircularUserAckDto>> GetVesselAckListAsync(long cir_MasterID);

    public Task<string> GetNextCIRNumbersByCategoryAsync(long categoryId);


    public Task AddToLog(long cir_MasterID, string activity, string remarks);

    public Task AddNotification(long cir_MasterID, string userID, string notificationType, string notificationTitle, string description);
}
