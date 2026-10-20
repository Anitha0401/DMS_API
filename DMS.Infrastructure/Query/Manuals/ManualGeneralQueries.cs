using DMS.Data.EF.Context;
using DMS.Application.DTO;
using DMS.Data.EF.Models;
using DMS.Data.EF.Models.Manuals;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using DMS.Data.EF.Query.Manuals;
using DMS.Application.DTO.Manuals;
using DMS.Application.Interface;
using DMS.Data.EF.Features;

namespace DMS.Data.EF.Query.Manuals;

public class ManualGeneralQueries : IManualGeneralQueries
{
    private readonly DmsContext _dbContext;
    private readonly GeneralContext _generalContext;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUser _currentUser;

    public ManualGeneralQueries(DmsContext dbContext, GeneralContext generalContext, IConfiguration config, ICurrentUser currentUser)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
        _generalContext = generalContext ?? throw new ArgumentNullException(nameof(_generalContext));
        _configuration = config ?? throw new ArgumentNullException("config");
        _currentUser = currentUser;
    }

    public async Task<List<Manual_TreeviewEntity>> GetDmManualTreeviewAsync(string userID)
    {
        var result = await _dbContext
                            .DmManual_Treeview
                            .FromSqlInterpolated($"EXEC GetManual_TreeViewList @UserID = {userID}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<Manual_TreeviewEntity>();
    }

    /// <summary>Tree limited to manuals matching a dashboard/menu link, plus their parents (IsMatch = 0).</summary>
    public async Task<List<Manual_TreeviewFilteredEntity>> GetDmManualTreeviewFilteredAsync(string userID, string calledMode)
    {
        var result = await _dbContext
                            .DmManual_TreeviewFiltered
                            .FromSqlInterpolated($"EXEC GetManual_TreeViewList_Filtered @UserID = {userID}, @CalledMode = {calledMode}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<Manual_TreeviewFilteredEntity>();
    }

    public async Task<List<ManualVersionDetailsDto>> GetDmManualVersionList(long dm_ManualID)
    {
        var result = await _dbContext
                            .DmManualVersionList
                            .FromSqlInterpolated($"EXEC GetManualVersionList @DM_ManualID = {dm_ManualID}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<ManualVersionDetailsDto>();
    }

    public async Task<List<ManualDetails>> GetUserActionManualList(string userID, string calledMode)
    {
        var result = await _dbContext
                            .DmManualList
                            .FromSqlInterpolated($"EXEC GetManual_UserActionList @UserID = {userID}, @CalledMode = {calledMode}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<ManualDetails>();
    }
   
    public async Task<ManualCount_User> GetUserManualCount(string userID)
    {
        var result = await _dbContext
                            .UserManualCount
                            .FromSqlInterpolated($"EXEC GetManual_Cnt_User @UserID = {userID}")
                            .AsNoTracking()
                            .ToListAsync();

        var userManualCnt = new ManualCount_User();

        if (result != null && result.Count > 0)
        {
            userManualCnt.NewCnt = result[0].NewCnt;
            userManualCnt.AckCnt = result[0].AckCnt;
            userManualCnt.ApprovalCnt = result[0].ApprovalCnt;
            userManualCnt.ReviewCnt = result[0].ReviewCnt;
            userManualCnt.FavouriteCnt = result[0].FavouriteCnt;
        }

        return userManualCnt;
    }

    public async Task<ManualCount_Dashboard> GetDashboardManualCounts(string userID)
    {
        var result = await _dbContext
                            .DashboardManualCount
                            .FromSqlInterpolated($"EXEC GetManual_Cnt_Dashboard @UserID = {userID}")
                            .AsNoTracking()
                            .ToListAsync();

        var userManualCnt = new ManualCount_Dashboard();

        if (result != null && result.Count > 0)
        {
            userManualCnt.NewCnt = result[0].NewCnt;
            userManualCnt.ToAckCnt = result[0].ToAckCnt;
            userManualCnt.ToApproveCnt = result[0].ToApproveCnt;
            userManualCnt.ReleasedCnt = result[0].ReleasedCnt;
            userManualCnt.UserToApproveCnt = result[0].UserToApproveCnt;
            userManualCnt.UserToReviewCnt = result[0].UserToReviewCnt;
            userManualCnt.UserToAckCnt = result[0].UserToAckCnt;
            userManualCnt.FavouriteCnt = result[0].FavouriteCnt;
        }

        return userManualCnt;
    }

    public async Task<List<ManualUserAckList>> GetUserAckList(long dm_ManualID)
    {
        var result = await _dbContext
                            .UserAckLists
                            .FromSqlInterpolated($"EXEC GetManualUserAckList @DM_ManualID = {dm_ManualID}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<ManualUserAckList>();
    }

    /// <summary>The newest notifications of the last 30 days meant for everyone ('') or for this user.</summary>
    public async Task<List<VwManualNotification>> GetNotificationList(string userID, int limit)
    {
        var since = DateTime.Now.AddDays(-30);
        var result = await _dbContext
                            .VwManualNotification
                            .Where(x => x.CreatedDate >= since && (x.UserID == null || x.UserID == "" || x.UserID == userID))
                            .OrderByDescending(x => x.CreatedDate)
                            .Take(NotificationReads.ClampLimit(limit))
                            .AsNoTracking()
                            .ToListAsync();

        return result;
    }

    // ---------------------------------------------------------------- Approval cycle
    // DM_Manual_ApprovedList.DM_ManualVersionID / IsVoid (Database/015_BackendFixes.sql) tie each approval to the
    // version it was given for, so a new version, or a version sent back, goes through the whole flow again.
    // Before the script is run, every approval of the manual counts (the old behaviour).

    private static bool _approvalCycleInstalled;

    private async Task<bool> ApprovalCycleInstalledAsync()
    {
        if (_approvalCycleInstalled) return true;
        var found = await _dbContext.Database.SqlQueryRaw<int>(
            "SELECT CASE WHEN COL_LENGTH('dbo.DM_Manual_ApprovedList', 'DM_ManualVersionID') IS NOT NULL " +
            "AND COL_LENGTH('dbo.DM_Manual_ApprovedList', 'IsVoid') IS NOT NULL THEN 1 ELSE 0 END AS [Value]").SingleAsync();
        return _approvalCycleInstalled = found == 1;
    }

    public sealed class ApprovedRole
    {
        public string? UserRole { get; set; }
        public string? HQDept { get; set; }
    }

    /// <summary>The role/department pairs that have already approved this version in its current cycle.</summary>
    public async Task<List<ApprovedRole>> GetApprovedRolesAsync(long dm_ManualID, long dm_ManualVersionID)
    {
        if (await ApprovalCycleInstalledAsync())
        {
            return await _dbContext.Database.SqlQuery<ApprovedRole>($@"
SELECT UserRole, HQDept FROM dbo.DM_Manual_ApprovedList
 WHERE DM_ManualID = {dm_ManualID} AND DM_ManualVersionID = {dm_ManualVersionID} AND ISNULL(IsVoid, 0) = 0").ToListAsync();
        }

        return await _dbContext.DmManualApprovedLists
            .Where(a => a.DM_ManualID == dm_ManualID)
            .Select(a => new ApprovedRole { UserRole = a.UserRole, HQDept = a.HQDept })
            .ToListAsync();
    }

    /// <summary>The first approver (by ApprovalOrder) at this status who has not yet approved this version.</summary>
    public async Task<DmApprovalAndMailSetup?> GetNextApproverAsync(int? approval_FlowID, long dm_StatusID, long dm_ManualID, long dm_ManualVersionID)
    {
        var approved = await GetApprovedRolesAsync(dm_ManualID, dm_ManualVersionID);
        var steps = await _dbContext.DmApprovalAndMailSetups
            .Where(s => s.FlowId == approval_FlowID && s.DmStatusId == dm_StatusID)
            .OrderBy(s => s.ApprovalOrder)
            .AsNoTracking()
            .ToListAsync();

        return steps.FirstOrDefault(s => !approved.Any(a =>
            string.Equals(a.UserRole, s.UserRole, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.HQDept, s.HQDept, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Records which version an approval (already saved) was given for.</summary>
    public async Task TagApprovalAsync(long dm_ManualApprovedListID, long dm_ManualVersionID)
    {
        if (!await ApprovalCycleInstalledAsync()) return;
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE dbo.DM_Manual_ApprovedList SET DM_ManualVersionID = {dm_ManualVersionID}, IsVoid = 0
 WHERE DM_Manual_ApprovedListID = {dm_ManualApprovedListID}");
    }

    /// <summary>Send back or version removed: this version's approvals no longer count (they stay as history).</summary>
    public async Task VoidApprovalsAsync(long dm_ManualID, long dm_ManualVersionID)
    {
        if (!await ApprovalCycleInstalledAsync()) return;
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE dbo.DM_Manual_ApprovedList SET IsVoid = 1
 WHERE DM_ManualID = {dm_ManualID} AND DM_ManualVersionID = {dm_ManualVersionID} AND ISNULL(IsVoid, 0) = 0");
    }

    public async Task AddToLog(long dm_ManualID, long dm_ManualVersionID, string activity, string remarks)
    {
        var dm_Manual_LogID = await _dbContext.NextIdAsync<DmManualLog>();

        var manualLog = new DmManualLog
        {
            DmManualLogId = dm_Manual_LogID,
            Origin = "HQ",
            VesselID = -1,
            DM_ManualID = dm_ManualID,
            DM_ManualVersionID = dm_ManualVersionID,
            Activity = activity,
            Remarks = remarks,
            UserName = _currentUser.UserId, // read back as the user ID (ManualLogDto.UserId)
            UserRank = "",
            Logdate = DateTime.Now,
        };

        _dbContext.DmManualLogs.Add(manualLog);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddNotification(long dm_ManualID, long dm_ManualVersionID, string userID, string notificationType, string notificationTitle, string description)
    {
        var nextDM_ManualNotificationID = await _dbContext.NextIdAsync<DM_ManualNotification>();

        var manualNotification = new DM_ManualNotification
        {
            DM_ManualNotificationId = nextDM_ManualNotificationID,
            Origin = "HQ",
            VesselID = -1,
            Description = description,
            NotificationType = notificationType,
            NotificationTitle = notificationTitle,
            DM_ManualID = dm_ManualID,
            DM_ManualVersionID = dm_ManualVersionID,
            UserID = userID,
            IsRead = 0,
            CreatedDate = DateTime.Now
        };
        await _dbContext.DM_ManualNotification.AddAsync(manualNotification);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<string?> GetDmManualContentAsync(long dm_ManualID, bool includeSubManuals)
    {
        var result = await _dbContext.DmManualContent
                                .FromSqlInterpolated($"EXEC GetManualAndSubManualContent @DM_ManualID = {dm_ManualID}, @IncludeSubManuals = {includeSubManuals}")
                                .AsNoTracking()
                                .ToListAsync();

        var content = result.Count > 0 ? result[0]?.ManualContent : "";

        return content;
    }

    public async Task<List<VesselListDto>> GetVesselList()
    {
        var vesselList = await _generalContext.VVessels
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
        var vslRankList = await _generalContext.VslStaffs
            .Select(x => new VslRankListDto
            {
                userRank = x.UserRank,
                userDept = x.UserDept ?? string.Empty,
                userDeptDisplay = x.UserDept != null
                    ? x.UserDept.ToLower() == "l" ? "Electrical" :
                       x.UserDept.ToLower() == "e" ? "Engine" :
                       x.UserDept.ToLower() == "d" ? "Deck" :
                       x.UserDept
                    : string.Empty
            })
            .Distinct()
            .ToListAsync();
        return vslRankList;
    }

    public async Task<List<HQUserListDto>> GetHQUserList()
    {
        var hQUserLists = await _generalContext.HqUsers
                                     .Select(x => new HQUserListDto
                                     {
                                         HQ_UsersID = x.HqUsersId,
                                         UserName = x.UserName,
                                         UserRole = x.Role,
                                         HQDept = x.HQDept
                                     }).ToListAsync();
        return hQUserLists;
    }

    public async Task<HqUser?> GetHQUser(string userID)
    {
        var user = await _generalContext.HqUsers
                        .Where(x => x.UserId == userID)
                        .FirstOrDefaultAsync();
        return user;
    }

    public async Task<List<ManualApprovalRoleListDto>> GetManualApprovalFlowDetails(int flowID)
    {
        var result = await _dbContext
                            .approvalFlowDetails
                            .FromSqlInterpolated($"EXEC GetManualApprovalFlowDetails @FlowID = {flowID}")
                            .AsNoTracking()
                            .ToListAsync();

        return result ?? new List<ManualApprovalRoleListDto>();
    }

    public async Task<long> GetNextApprovalStatusID(int approval_FlowID, long current_DM_StatusID)
    {
        var nextDM_StatusID = await _dbContext.DmApprovalAndMailSetups
                                        .Where(af => af.FlowId == approval_FlowID && af.DmStatusId == current_DM_StatusID)
                                        .Select(af => af.NextDmStatusId)
                                        .FirstOrDefaultAsync();

        return nextDM_StatusID ?? current_DM_StatusID;
    }

    public async Task<long> GetSendBackApprovalStatusID(int approval_FlowID, long current_DM_StatusID)
    {
        var nextDM_StatusID = await _dbContext.DmApprovalAndMailSetups
                                        .Where(af => af.FlowId == approval_FlowID && af.DmStatusId == current_DM_StatusID)
                                        .Select(af => af.SendBackDmStatusId)
                                        .FirstOrDefaultAsync();

        return nextDM_StatusID ?? current_DM_StatusID;
    }

    public async Task<List<Manual_TreeviewEntity>> xxGetDmManualTreeviewAsyncs()
    {
        var result = new List<Manual_TreeviewEntity>();

        var connStr = _configuration.GetConnectionString("LocalDBConnection");
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(); // Does this throw?

        var connection = _dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "GetManual_TreeViewList";
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
        var connection = _dbContext.Database.GetDbConnection();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "EXEC GetManualAndSubManualContent @DM_ManualID";
            command.CommandType = CommandType.Text;

            var rootParam = command.CreateParameter();
            rootParam.ParameterName = "@DM_ManualID";
            rootParam.Value = DM_ManualID;
            command.Parameters.Add(rootParam);

            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync();

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
    }

    public async Task<bool> xxCheckDmManualAsync(int manualID)
    {
        return await _dbContext.DmManuals.AnyAsync(e => e.DM_ManualID == manualID);
    }


}
