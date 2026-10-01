using DMS.Data.EF.Context;
using DMS.Application.DTO.Circulars;
using DMS.Application.DTO.OtherDocument;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.OtherDocument;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DMS.Data.EF.Query.Circulars;

public class OtherDocumentGeneralQueries : ICIRGeneralQueries
{
    private readonly OtherDocumentsContext _dbContext;
    private readonly GeneralContext _generalContext;
    private readonly IConfiguration _configuration;

    public OtherDocumentGeneralQueries(OtherDocumentsContext dbContext, GeneralContext generalContext, IConfiguration config)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
        _generalContext = generalContext ?? throw new ArgumentNullException(nameof(_generalContext));
        _configuration = config ?? throw new ArgumentNullException("config");
    }

    public async Task<List<OtherDocumentList>> GetOtherDocumentistAsync(string userID, long documentTypeID)
    {
        try
        {
            var result = await _dbContext
                                .OtherDocumentList
                                .FromSqlInterpolated($"EXEC GetOtherDocumentList @UserID = {userID}, @CIR_CategoryID = {documentTypeID}")
                                .AsNoTracking()
                                .ToListAsync();

            return result ?? new List<OtherDocumentList>();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<OtherDocumentChart>> GetOtherDocumentVesselBasedChartDataAsync(string otherDocumentTypeID)
    {
        try
        {
            var result = await _dbContext
                                .Set<OtherDocumentChart>()
                                .FromSqlInterpolated($"EXEC GetOtherDocument_VesselChartData @OtherDocumentTypeID = {otherDocumentTypeID}")
                                .AsNoTracking()
                                .ToListAsync();

            return result ?? new List<OtherDocumentChart>();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<OtherDocumentRecentList>> GetRecentOtherDocumentcularsAsync(string otherDocumentTypeID, int limit)
    {
        try
        {
            var result = await _dbContext
                                .Set<OtherDocumentRecentList>()
                                .FromSqlInterpolated($"EXEC GetOtherDocument_RecentList @OtherDocumentTypeID = {otherDocumentTypeID}, @Limit = {limit}")
                                .AsNoTracking()
                                .ToListAsync();

            return result ?? new List<OtherDocumentRecentList>();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<VwOtherDocumentNotification>> GetNotificationList()
    {
        try
        {
            var result = await _dbContext
                                .VwOtherDocumentNotification
                                .Where(x => x.CreatedDate >= DateTime.Now.AddDays(-30))
                                .OrderByDescending(x => x.CreatedDate)
                                .AsNoTracking()
                                .ToListAsync();

            return result ?? new List<VwOtherDocumentNotification>();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task AddToLog(long otherDocumentMasterID, string activity, string remarks)
    {
        try
        {
            var otherDocument_LogID = (_dbContext.OtherDocument_Log
                                    .Select(m => (long?)m.OtherDocument_LogID)
                                    .Max() ?? 0) + 1;

            var docLog = new OtherDocument_Log
            {
                OtherDocument_LogID = otherDocument_LogID,
                Origin = "HQ",
                VesselID = -1,
                OtherDocumentMasterID = otherDocumentMasterID,
                Activity = activity,
                Remarks = remarks,
                UserName = "",
                UserRank = "",
                LogDate = DateTime.Now,
            };

            _dbContext.OtherDocument_Log.Add(docLog);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task AddNotification(long otherDocumentMasterID, string userID, string notificationType, string notificationTitle, string description)
    {
        try
        {
            var nextOtherDocument_NotificationID = _dbContext.OtherDocument_Notification
                                              .Select(m => (long?)m.OtherDocument_NotificationID)
                                              .Max() ?? 0;

            nextOtherDocument_NotificationID = nextOtherDocument_NotificationID + 1;

            var notification = new OtherDocument_Notification();
            notification.OtherDocument_NotificationID = nextOtherDocument_NotificationID;
            notification.Origin = "HQ";
            notification.VesselID = -1;
            notification.Description = description;
            notification.NotificationType = notificationType;
            notification.NotificationTitle = notificationTitle;
            notification.OtherDocumentMasterID = otherDocumentMasterID;
            notification.UserID = userID;
            notification.IsRead = 0;
            notification.CreatedDate = DateTime.Now;
            await _dbContext.OtherDocument_Notification.AddAsync(notification);

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public Task<List<CIR_TreeviewEntity>> GetCIRTreeviewAsync(string userID, string circularType)
    {
        throw new NotImplementedException();
    }

    public Task<List<CIRMasterList>> GetCIRListAsync(string userID, long cir_CategoryID, string circularType)
    {
        throw new NotImplementedException();
    }

    public Task<List<CircularsChart>> GetCIRCategoryBasedChartDataAsync(string circularType)
    {
        throw new NotImplementedException();
    }

    public Task<List<CIRRecentList>> GetRecentCircularsAsync(string circularType, int limit)
    {
        throw new NotImplementedException();
    }

    public Task<CircularCount_Dashboard> GetCIRDashboardCountsAsync()
    {
        throw new NotImplementedException();
    }

    Task<List<VwCIRNotification>> ICIRGeneralQueries.GetNotificationList()
    {
        throw new NotImplementedException();
    }

    public Task<List<CircularUserAckDto>> GetVesselAckListAsync(long cir_MasterID)
    {
        throw new NotImplementedException();
    }
}
