using DMS.Data.EF.Models.Circulars;

namespace DMS.Data.EF.Query;

public interface ICIRGeneralQueries
{
    public Task<List<CIR_TreeviewEntity>> GetCIRTreeviewAsync(string userID, string circularType);

    public Task<List<VwCIRNotification>> GetNotificationList();

    public Task AddToLog(long cir_MasterID, string activity, string remarks);

    public Task AddNotification(long cir_MasterID, string userID, string notificationType, string notificationTitle, string description);

}
