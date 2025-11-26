using DMS.Data.EF.Context;
using DMS.Data.EF.DTO;
using DMS.Data.EF.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DMS.Data.EF.Query;

public class GeneralQueries : IGeneralQueries
{
    private readonly DmsContext _dmsContext;
    private readonly IConfiguration _configuration;

    public GeneralQueries(DmsContext dmsContext, IConfiguration config)
    {
        _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(_dmsContext));
        _configuration = config ?? throw new ArgumentNullException("config");
    }

    public async Task<List<Manual_TreeviewEntity>> GetDmManualTreeviewAsync(string userID)
    {
        var result = await _dmsContext
                            .DmManual_Treeview
                            .FromSqlInterpolated($"EXEC GetTreeViewManualList @UserID = {userID}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<Manual_TreeviewEntity>();
    }

    public async Task<List<ManualVersionDetailsDto>> GetDmManualVersionList(long dm_ManualID)
    {
        var result = await _dmsContext
                            .DmManualVersionList
                            .FromSqlInterpolated($"EXEC GetManualVersionList @DM_ManualID = {dm_ManualID}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<ManualVersionDetailsDto>();
    }

    public async Task<List<ManualDetails>> GetUserActionManualList(string userID, string calledMode)
    {
        var result = await _dmsContext
                            .DmManualList
                            .FromSqlInterpolated($"EXEC GetUserActionManualList @UserID = {userID}, @CalledMode = {calledMode}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<ManualDetails>();
    }
   
    public async Task<ManualCount_User> GetUserManualCount(string userID)
    {
        var result = await _dmsContext
                            .UserManualCount
                            .FromSqlInterpolated($"EXEC GetManual_Cnt_User @UserID = {userID}")
                            .AsNoTracking()
                            .ToListAsync();

        var userManualCnt = new ManualCount_User();

        if (result != null && result.Count > 0)
        {
            userManualCnt.NewCnt = (int?)result[0].NewCnt;
            userManualCnt.AckCnt = (int?)result[0].AckCnt;
            userManualCnt.ApprovalCnt = (int?)result[0].ApprovalCnt;
            userManualCnt.ReviewCnt = (int?)result[0].ReviewCnt;
            userManualCnt.FavouriteCnt = (int?)result[0].FavouriteCnt;
        }

        return userManualCnt;
    }

    public async Task<ManualCount_Dashboard> GetDashboardManualCounts(string userID)
    {
        var result = await _dmsContext
                            .DashboardManualCount
                            .FromSqlInterpolated($"EXEC GetManual_Cnt_Dashboard @UserID = {userID}")
                            .AsNoTracking()
                            .ToListAsync();

        var userManualCnt = new ManualCount_Dashboard();

        if (result != null && result.Count > 0)
        {
            userManualCnt.NewCnt = (int?)result[0].NewCnt;
            userManualCnt.ToAckCnt = (int?)result[0].ToAckCnt;
            userManualCnt.ToApproveCnt = (int?)result[0].ToApproveCnt;
            userManualCnt.ReleasedCnt = (int?)result[0].ReleasedCnt;
            userManualCnt.UserToApproveCnt = (int?)result[0].UserToApproveCnt;
            userManualCnt.UserToReviewCnt = (int?)result[0].UserToReviewCnt;
            userManualCnt.UserToAckCnt = (int?)result[0].UserToAckCnt;
            userManualCnt.FavouriteCnt = (int?)result[0].FavouriteCnt;
        }

        return userManualCnt;
    }

    public async Task<List<ManualUserAckList>> GetUserAckList(long dm_ManualID)
    {
        var result = await _dmsContext
                            .UserAckLists
                            .FromSqlInterpolated($"EXEC GetManualUserAckList @DM_ManualID = {dm_ManualID}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<ManualUserAckList>();
    }

    public async Task<List<VwManualNotification>> GetNotificationList()
    {
        var result = await _dmsContext
                            .VwManualNotification
                            .Where(x => x.CreatedDate >= DateTime.Now.AddDays(-30))
                            .OrderByDescending(x => x.CreatedDate)
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<VwManualNotification>();
    }

    public async Task AddToLog(long dm_ManualID, long dm_ManualVersionID, string activity, string remarks)
    {
        var dm_Manual_LogID = (_dmsContext.DmManualLogs
                                .Select(m => (long?)m.DmManualLogId)
                                .Max() ?? 0) + 1;

        var manualLog = new DmManualLog
        {
            DmManualLogId = dm_Manual_LogID,
            Origin = "HQ",
            VesselID = -1,
            DM_ManualID = dm_ManualID,
            DM_ManualVersionID = dm_ManualVersionID,
            Activity = activity,
            Remarks = remarks,
            UserName = "",
            UserRank = "",
            Logdate = DateTime.Now,
        };

        _dmsContext.DmManualLogs.Add(manualLog);
        await _dmsContext.SaveChangesAsync();
    }

    public async Task AddNotification(long dm_ManualID, long dm_ManualVersionID, string userID, string notificationType, string notificationTitle, string description)
    {
        try
        {
            var nextDM_ManualNotificationID = _dmsContext.DM_ManualNotification
                                              .Select(m => (long?)m.DM_ManualNotificationId)
                                              .Max() ?? 0;

            nextDM_ManualNotificationID = nextDM_ManualNotificationID + 1;

            var manualNotification = new DM_ManualNotification();
            manualNotification.DM_ManualNotificationId = nextDM_ManualNotificationID;
            manualNotification.Origin = "HQ";
            manualNotification.VesselID = -1;
            manualNotification.Description = description;
            manualNotification.NotificationType = notificationType;
            manualNotification.NotificationTitle = notificationTitle;
            manualNotification.DM_ManualID = dm_ManualID;
            manualNotification.DM_ManualVersionID = dm_ManualVersionID;
            manualNotification.UserID = userID;
            manualNotification.IsRead = 0;
            manualNotification.CreatedDate = DateTime.Now;
            await _dmsContext.DM_ManualNotification.AddAsync(manualNotification);

            await _dmsContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log or rethrow for debugging
            throw new Exception("Failed to add to notification", ex);
        }
    }

    public async Task<string?> GetDmManualContentAsync(long dm_ManualID, bool includeSubManuals)
    {
        var result = await _dmsContext.DmManualContent
                                .FromSqlInterpolated($"EXEC GetManualAndSubManualContent @DM_ManualID = {dm_ManualID}, @IncludeSubManuals = {includeSubManuals}")
                                .AsNoTracking()
                                .ToListAsync();

        var content = result.Count > 0 ? result[0]?.ManualContent : "";

        return content;
    }

    public async Task<List<VesselListDto>> GetVesselList()
    {
        var vesselList = await _dmsContext.VVessels
                                      .Select(x => new VesselListDto
                                      {
                                          VesselID = x.VesselID,
                                          VslName = x.VslName,
                                          VslCode = x.VslCode,
                                          VslType = x.VslType
                                      }).ToListAsync();
        return vesselList;
    }

    public async Task<List<VslRankListDto>> GetVslRankList()
    {
        var vslRankList = await _dmsContext.VslStaffs
            .Select(x => new VslRankListDto
            {
                userRank = x.UserRank,
                userDept = x.UserDept ?? string.Empty,
                userDeptDisplay = x.UserDept != null
                    ? (x.UserDept.ToLower() == "l" ? "Electrical" :
                       x.UserDept.ToLower() == "e" ? "Engine" :
                       x.UserDept.ToLower() == "d" ? "Deck" :
                       x.UserDept)
                    : string.Empty
            })
            .Distinct()
            .ToListAsync();
        return vslRankList;
    }

    public async Task<List<HQUserListDto>> GetHQUserList()
    {
        var hQUserLists = await _dmsContext.HqUsers
                                     .Select(x => new HQUserListDto
                                     {
                                         HQ_UsersID = x.HqUsersId,
                                         UserName = x.UserName,
                                         UserRole = x.Role,
                                         HQDept = x.HQDept
                                     }).ToListAsync();
        return hQUserLists;
    }

    public async Task<List<ApprovalDetailsDto>> GetApprovalFlowDetails(int flowID)
    {
        var result = await _dmsContext
                            .approvalFlowDetails
                            .FromSqlInterpolated($"EXEC GetApprovalFlowDetails @FlowID = {flowID}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<ApprovalDetailsDto>();
    }

    public async Task<long> GetNextApprovalStatusID(int approval_FlowID, long current_DM_StatusID)
    {
        var nextDM_StatusID = await _dmsContext.DmApprovalAndMailSetups
                                        .Where(af => af.FlowId == approval_FlowID && af.DmStatusId == current_DM_StatusID)
                                        .Select(af => (long?)af.NextDmStatusId)
                                        .FirstOrDefaultAsync();

        return nextDM_StatusID ?? current_DM_StatusID;
    }

    public async Task<long> GetSendBackApprovalStatusID(int approval_FlowID, long current_DM_StatusID)
    {
        var nextDM_StatusID = await _dmsContext.DmApprovalAndMailSetups
                                        .Where(af => af.FlowId == approval_FlowID && af.DmStatusId == current_DM_StatusID)
                                        .Select(af => (long?)af.SendBackDmStatusId)
                                        .FirstOrDefaultAsync();

        return nextDM_StatusID ?? current_DM_StatusID;
    }

    public async Task<List<Manual_TreeviewEntity>> xxGetDmManualTreeviewAsyncs()
    {
        var result = new List<Manual_TreeviewEntity>();

        var connStr = _configuration.GetConnectionString("LocalDBConnection");
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(); // Does this throw?

        var connection = _dmsContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "GetTreeViewManualList";
        command.CommandType = CommandType.StoredProcedure;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new Manual_TreeviewEntity
            {
                DM_ManualID = reader.GetInt64(0),
                ManualNo = reader.GetString(1),
                ManualCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                ManualName = reader.GetString(3),
                StatusString = reader.GetString(4),
                ParentID = reader.GetInt64(5),
                ManualLevel = reader.GetInt32(6),
                SortOrder = reader.IsDBNull(7) ? null : reader.GetString(7),
                AddlSortOrder = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return result;
    }

    public async Task<string?> xxGetDmManualContent_ScalarAsync(long DM_ManualID)
    {
        var connection = _dmsContext.Database.GetDbConnection();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "EXEC GetManualAndSubManualContent @DM_ManualID";
            command.CommandType = System.Data.CommandType.Text;

            var rootParam = command.CreateParameter();
            rootParam.ParameterName = "@DM_ManualID";
            rootParam.Value = DM_ManualID;
            command.Parameters.Add(rootParam);

            if (connection.State == System.Data.ConnectionState.Closed)
                await connection.OpenAsync();

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
    }

    public async Task<bool> xxCheckDmManualAsync(int manualID)
    {
        return await _dmsContext.DmManuals.AnyAsync(e => e.DM_ManualID == manualID);
    }


}
