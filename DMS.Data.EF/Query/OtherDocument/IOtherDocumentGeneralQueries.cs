using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.OtherDocument;

namespace DMS.Data.EF.Query.Circulars;

public interface IOtherDocumentGeneralQueries
{
    public Task<List<OtherDocumentList>> GetOtherDocumentListAsync(string userID, long OtherDocument_CategoryID, string OtherDocumentcularType);

    public Task<List<OtherDocumentChart>> GetOtherDocumentCategoryBasedChartDataAsync(string OtherDocumentcularType);

    public Task<List<OtherDocumentRecentList>> GetRecentOtherDocumentcularsAsync(string OtherDocumentcularType, int limit);

    public Task<List<VwOtherDocumentNotification>> GetNotificationList();

    public Task AddToLog(long OtherDocument_MasterID, string activity, string remarks);

    public Task AddNotification(long OtherDocument_MasterID, string userID, string notificationType, string notificationTitle, string description);
}
