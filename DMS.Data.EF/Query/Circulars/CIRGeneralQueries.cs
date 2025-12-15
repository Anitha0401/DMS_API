using DMS.Data.EF.Context;
using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.DTO;
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
        try
        {
            var result = await _dbContext
                                .CIR_Treeview
                                .FromSqlInterpolated($"EXEC GetCIR_TreeViewList @UserID = {userID}, @CircularType = {circularType}")
                                .AsNoTracking()
                                .ToListAsync();

            return result ?? new List<CIR_TreeviewEntity>();
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to retrieve CIR treeview data", ex);
        }
    }

    public async Task<List<CIRMasterList>> GetCIRListAsync(string userID, long cir_CategoryID, string circularType)
    {
        try
        {
            var result = await _dbContext
                                .CIRMasterList
                                .FromSqlInterpolated($"EXEC GetCIRMasterList @UserID = {userID}, @CIR_CategoryID = {cir_CategoryID}, @CircularType = {circularType}")
                                .AsNoTracking()
                                .ToListAsync();

            return result ?? new List<CIRMasterList>();
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to retrieve CIR master list", ex);
        }
    }

    public async Task<List<CircularsChart>> GetCIRCategoryBasedChartDataAsync(string circularType)
    {
        try
        {
            var result = await _dbContext
                                .Set<CircularsChart>()
                                .FromSqlInterpolated($"EXEC GetCIR_CategoryChartData @CircularType = {circularType}")
                                .AsNoTracking()
                                .ToListAsync();

            return result ?? new List<CircularsChart>();
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to retrieve CIR category chart data", ex);
        }
    }

    public async Task<List<CIRRecentList>> GetRecentCircularsAsync(string circularType, int limit)
    {
        try
        {
            var result = await _dbContext
                                .Set<CIRRecentList>()
                                .FromSqlInterpolated($"EXEC GetRecentCirculars @CircularType = {circularType}, @Limit = {limit}")
                                .AsNoTracking()
                                .ToListAsync();

            return result ?? new List<CIRRecentList>();
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to retrieve recent circulars", ex);
        }
    }

    public async Task<CircularCount_Dashboard> GetCIRDashboardCountsAsync()
    {
        try
        {
            var result = await _dbContext
                                .Set<CircularCount_Dashboard>()
                                .FromSqlInterpolated($"EXEC GetCIR_DashboardCount")
                                .AsNoTracking()
                                .ToListAsync();
            var dashboard = new CircularCount_Dashboard();

            if (result != null && result.Count > 0)
            {
                var r = result[0];
                dashboard.TotalCirculars = r.TotalCirculars;
                dashboard.TotalAlerts = r.TotalAlerts;
            }

            return dashboard;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to retrieve CIR dashboard counts", ex);
        }
    }

    public async Task<List<VwCIRNotification>> GetNotificationList()
    {
        try
        {
            var result = await _dbContext
                                .VwCIRNotification
                                .Where(x => x.CreatedDate >= DateTime.Now.AddDays(-30))
                                .OrderByDescending(x => x.CreatedDate)
                                .AsNoTracking()
                                .ToListAsync();

            return result ?? new List<VwCIRNotification>();
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to retrieve CIR notifications", ex);
        }
    }

    public async Task<List<CircularUserAckDto>> GetVesselAckListAsync(long cir_MasterID)
    {
        try
        {
            var vessels = await _generalContext.VVessels.AsNoTracking().ToListAsync();
            var ackVsl = await _dbContext.CIR_ToAck_Vsl
                                .Where(a => a.CIR_MasterID == cir_MasterID)
                                .AsNoTracking()
                                .ToListAsync();
            var userAcks = await _dbContext.Vw_CIR_VesselAck
                               .Where(x => x.CIR_MasterID == cir_MasterID)
                               .AsNoTracking()
                               .ToListAsync();

            var userAckByVsl = userAcks
                              .GroupBy(u => (VesselId: Convert.ToInt64(u.VesselID), UserRank: u.UserRank))
                              .ToDictionary(g => g.Key, g => g.First());


            var result = new List<CircularUserAckDto>(vessels.Count);
            foreach (var vsl in vessels)
            {
                var vslId = Convert.ToInt64(vsl.VesselID);

                foreach (var ack in ackVsl)
                {
                    userAckByVsl.TryGetValue((vslId, ack.UserRank), out var ua);

                    var dateRead = ua?.DateRead;
                    var userName = ua?.UserName;
                    var userRank = ack.UserRank;
                    var comments = ua?.Comments;

                    var dto = new CircularUserAckDto
                    {
                        VesselID = vsl.VesselID,
                        VslCode = vsl.VslCode,
                        VslName = vsl.VslName,
                        DateRead = dateRead.HasValue ? dateRead.Value.ToString("dd-MMM-yyyy") : " ",
                        Status = (userName != null || dateRead.HasValue) ? "Acknowledged" :  "Pending",
                        AcknowledgedBy = (userName != null)
                                            ? $"{(userName ?? string.Empty)} ({(userRank ?? string.Empty)})"
                                            : $"Pending ({(userRank ?? string.Empty)})",
                        Comments = comments
                    };

                    result.Add(dto);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to retrieve vessel ack list for circular", ex);
        }
    }

    public async Task AddToLog(long cir_MasterID, string activity, string remarks)
    {
        try
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
        catch (Exception ex)
        {
            throw new Exception("Failed to add CIR log", ex);
        }
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
            throw new Exception("Failed to add to notification", ex);
        }
    }
}
