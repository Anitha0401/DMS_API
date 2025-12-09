using DMS.Data.EF.Context;
using DMS.Data.EF.Models.Circulars;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DMS.Data.EF.Query;

public class CIRGeneralQueries : ICIRGeneralQueries
{
    private readonly CircularsContext _dbContext;
    private readonly GeneralContext _generalContext;
    private readonly IConfiguration _configuration;

    public CIRGeneralQueries(CircularsContext dbContext, GeneralContext generalContext, IConfiguration config)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
        _generalContext = generalContext ?? throw new ArgumentNullException(nameof(_generalContext));
        _configuration = config ?? throw new ArgumentNullException("config");
    }

    public async Task<List<CIR_TreeviewEntity>> GetCIRTreeviewAsync(string userID, string circularType)
    {
        var result = await _dbContext
                            .CIR_Treeview
                            .FromSqlInterpolated($"EXEC GetCircular_TreeViewList @UserID = {userID}, @CircularType = {circularType}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<CIR_TreeviewEntity>();
    }


    public async Task<List<VwCIRNotification>> GetNotificationList()
    {
        var result = await _dbContext
                            .VwCIRNotification
                            .Where(x => x.CreatedDate >= DateTime.Now.AddDays(-30))
                            .OrderByDescending(x => x.CreatedDate)
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<VwCIRNotification>();
    }

    public async Task AddToLog(long cir_MasterID, string activity, string remarks)
    {
        var cir_LogID = (_dbContext.CIR_Log
                                .Select(m => (long?)m.CIR_LogID)
                                .Max() ?? 0) + 1;

        var manualLog = new CIR_Log
        {
            CIR_LogID = cir_LogID,
            Origin = "HQ",
            VesselID = -1,
            CIR_MasterID = cir_MasterID,
            Activity = activity,
            Remarks = remarks,
            UserName = "",
            UserRank = "",
            LogDate = DateTime.Now,
        };

        _dbContext.CIR_Log.Add(manualLog);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddNotification(long cir_MasterID, string userID, string notificationType, string notificationTitle, string description)
    {
        try
        {
            var nextCIR_NotificationID = _dbContext.CIR_Notification
                                              .Select(m => (long?)m.CIR_NotificationID)
                                              .Max() ?? 0;

            nextCIR_NotificationID = nextCIR_NotificationID + 1;

            var notification = new CIR_Notification();
            notification.CIR_NotificationID = nextCIR_NotificationID;
            notification.Origin = "HQ";
            notification.VesselID = -1;
            notification.Description = description;
            notification.NotificationType = notificationType;
            notification.NotificationTitle = notificationTitle;
            notification.CIR_MasterID = cir_MasterID;
            notification.UserID = userID;
            notification.IsRead = 0;
            notification.CreatedDate = DateTime.Now;
            await _dbContext.CIR_Notification.AddAsync(notification);

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log or rethrow for debugging
            throw new Exception("Failed to add to notification", ex);
        }
    }

}
