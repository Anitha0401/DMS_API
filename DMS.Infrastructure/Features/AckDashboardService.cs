using DMS.Application.DTO.Features;
using DMS.Application.Interface;

namespace DMS.Data.EF.Features;

/// <summary>
/// Feature 1: who still has to acknowledge which manual, circular or alert.
///   Required = an active ToAck row for the vessel (or -1) and the crew member's rank, on the item's current released version.
///   Done     = a UserAck row for that version (manuals) / circular and crew member.
///   Due      = released + the record's AckDueDays (filled from the settings when the record was added).
/// The office knows a vessel's acknowledgements only as far as that vessel has synced.
/// </summary>
public sealed class AckDashboardService : IAckDashboardService
{
    private readonly SqlDb _db;

    public AckDashboardService(SqlDb db) => _db = db;

    private const string Common = @"
DECLARE @dm int = ISNULL(TRY_CAST((SELECT TOP (1) SettingValue FROM dbo.DM_Settings WHERE SettingName = 'DefaultAckDueDays') AS int), 7);
DECLARE @dc int = ISNULL(TRY_CAST((SELECT TOP (1) SettingValue FROM dbo.CIR_Settings WHERE SettingName = 'DefaultAckDueDays_Circular') AS int), 7);
DECLARE @da int = ISNULL(TRY_CAST((SELECT TOP (1) SettingValue FROM dbo.CIR_Settings WHERE SettingName = 'DefaultAckDueDays_Alert') AS int), 1);
WITH vessels AS (
    SELECT VesselID, VslName FROM dbo.V_Vessel WHERE VesselID > 0 AND ISNULL(IsRetired, 0) = 0 AND (@vessel IS NULL OR VesselID = @vessel)
),
mv AS (
    SELECT v.DM_ManualID, v.DM_ManualVersionID, v.Version,
           COALESCE(v.ReleasedDate, v.PublishedDate, v.CreatedDate) AS ReleasedOn,
           ROW_NUMBER() OVER (PARTITION BY v.DM_ManualID ORDER BY v.DM_ManualVersionID DESC) AS rn
      FROM dbo.DM_ManualVersion v WHERE v.IsActive = 1
),
man AS (
    SELECT m.DM_ManualID, m.ManualNo, m.ManualName, ISNULL(m.AckDueDays, @dm) AS AckDueDays,
           mv.DM_ManualVersionID, mv.Version, mv.ReleasedOn
      FROM dbo.DM_Manual m JOIN mv ON mv.DM_ManualID = m.DM_ManualID AND mv.rn = 1
     WHERE ISNULL(m.IsDeleted, 0) <> 1
),
cir AS (
    SELECT c.CIR_MasterID, c.CIR_Number, c.Title,
           CASE WHEN c.CircularType LIKE 'Alert%' THEN 'Alert' ELSE 'Circular' END AS ItemType,
           COALESCE(c.ReleasedDate, c.DateIssued, c.CreatedDate) AS ReleasedOn,
           ISNULL(c.AckDueDays, CASE WHEN c.CircularType LIKE 'Alert%' THEN @da ELSE @dc END) AS AckDueDays
      FROM dbo.CIR_Master c
     WHERE ISNULL(c.IsDeleted, 0) <> 1 AND ISNULL(c.Status, '') NOT IN ('Temp', 'Draft', 'Cancelled')
       AND (c.ValidUntil IS NULL OR c.ValidUntil >= CAST(GETDATE() AS date))
),
crew AS (
    SELECT s.Vsl_StaffID, s.VesselID, s.UserID, s.UserName, s.UserRank
      FROM dbo.Vsl_Staff s
     WHERE ISNULL(s.IsDisabled, 0) = 0 AND (s.LeavingDate IS NULL OR s.LeavingDate > GETDATE())
)";

    private const string VesselPendingSql = Common + @"
SELECT ves.VesselID, ves.VslName AS VesselName, 'Manual' AS ItemType, man.DM_ManualID AS ItemID, man.ManualNo AS Number,
       man.ManualName AS Title, man.Version, s.UserRank AS Rank, s.UserID, s.UserName, man.ReleasedOn,
       DATEADD(day, man.AckDueDays, man.ReleasedOn) AS DueOn, man.AckDueDays
  FROM (SELECT DISTINCT ves.VesselID, t.DM_ManualID, t.UserRank
          FROM dbo.DM_Manual_ToAck_Vsl t JOIN vessels ves ON t.VesselID IN (ves.VesselID, -1)
         WHERE ISNULL(t.IsActive, 1) = 1) r
  JOIN vessels ves ON ves.VesselID = r.VesselID
  JOIN man ON man.DM_ManualID = r.DM_ManualID
  JOIN crew s ON s.VesselID = r.VesselID AND s.UserRank = r.UserRank
 WHERE NOT EXISTS (SELECT 1 FROM dbo.DM_Manual_UserAck a WHERE a.DM_ManualVersionID = man.DM_ManualVersionID AND a.UserID = s.UserID)
UNION ALL
SELECT ves.VesselID, ves.VslName, cir.ItemType, cir.CIR_MasterID, cir.CIR_Number, cir.Title, NULL, s.UserRank, s.UserID,
       s.UserName, cir.ReleasedOn, DATEADD(day, cir.AckDueDays, cir.ReleasedOn), cir.AckDueDays
  FROM (SELECT DISTINCT ves.VesselID, t.CIR_MasterID, t.UserRank
          FROM dbo.CIR_ToAck_Vsl t JOIN vessels ves ON t.VesselID IN (ves.VesselID, -1)
         WHERE ISNULL(t.IsActive, 1) = 1) r
  JOIN vessels ves ON ves.VesselID = r.VesselID
  JOIN cir ON cir.CIR_MasterID = r.CIR_MasterID
  JOIN crew s ON s.VesselID = r.VesselID AND s.UserRank = r.UserRank
 WHERE NOT EXISTS (SELECT 1 FROM dbo.CIR_UserAck a WHERE a.CIR_MasterID = cir.CIR_MasterID AND a.Fleet_CrewID = s.Vsl_StaffID)";

    // Office users who must acknowledge (DM_Manual_ToAck_HQ / CIR_ToAck_HQ). An office circular acknowledgement is
    // taken to be a CIR_UserAck row with VesselID -1 and Fleet_CrewID = HQ_UsersID.
    private const string OfficePendingSql = Common + @"
SELECT -1 AS VesselID, 'Office' AS VesselName, 'Manual' AS ItemType, man.DM_ManualID AS ItemID, man.ManualNo AS Number,
       man.ManualName AS Title, man.Version, u.Role AS Rank, u.UserID, u.UserName, u.HQDept AS Dept, man.ReleasedOn,
       DATEADD(day, man.AckDueDays, man.ReleasedOn) AS DueOn, man.AckDueDays
  FROM dbo.DM_Manual_ToAck_HQ t
  JOIN dbo.HQ_Users u ON u.HQ_UsersID = t.HQ_UsersID AND ISNULL(u.IsDisabled, 0) = 0
  JOIN man ON man.DM_ManualID = t.DM_ManualID
 WHERE ISNULL(t.IsActive, 1) = 1 AND (@user IS NULL OR u.UserID = @user)
   AND NOT EXISTS (SELECT 1 FROM dbo.DM_Manual_UserAck a WHERE a.DM_ManualVersionID = man.DM_ManualVersionID AND a.UserID = u.UserID)
UNION ALL
SELECT -1, 'Office', cir.ItemType, cir.CIR_MasterID, cir.CIR_Number, cir.Title, NULL, u.Role, u.UserID, u.UserName, u.HQDept,
       cir.ReleasedOn, DATEADD(day, cir.AckDueDays, cir.ReleasedOn), cir.AckDueDays
  FROM dbo.CIR_ToAck_HQ t
  JOIN dbo.HQ_Users u ON u.HQ_UsersID = t.HQ_UsersID AND ISNULL(u.IsDisabled, 0) = 0
  JOIN cir ON cir.CIR_MasterID = t.CIR_MasterID
 WHERE ISNULL(t.IsActive, 1) = 1 AND (@user IS NULL OR u.UserID = @user)
   AND NOT EXISTS (SELECT 1 FROM dbo.CIR_UserAck a WHERE a.CIR_MasterID = cir.CIR_MasterID AND a.VesselID = -1
                     AND a.Fleet_CrewID = u.HQ_UsersID)";

    public async Task<List<AckPendingRow>> GetVesselPendingAsync(int? vesselId, string? itemType, string? rank)
    {
        var rows = await _db.QueryAsync<AckPendingRow>(VesselPendingSql, ("@vessel", vesselId));
        return rows
            .Where(r => string.IsNullOrEmpty(itemType) || string.Equals(r.ItemType, itemType, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(rank) || string.Equals(r.Rank, rank, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.DueOn ?? DateTime.MaxValue).ThenBy(r => r.VesselName).ThenBy(r => r.Rank).ThenBy(r => r.UserName)
            .ToList();
    }

    public async Task<List<AckVesselSummary>> GetVesselSummaryAsync()
    {
        var pending = await _db.QueryAsync<AckPendingRow>(VesselPendingSql, ("@vessel", null));
        var vessels = await _db.QueryAsync<AckVesselSummary>(@"
SELECT v.VesselID, v.VslName AS VesselName
  FROM dbo.V_Vessel v WHERE v.VesselID > 0 AND ISNULL(v.IsRetired, 0) = 0 ORDER BY v.VslName");
        if (await _db.ScalarAsync("SELECT OBJECT_ID('dbo.Sync_Vessel', 'U')") != null)
        {
            var sync = (await _db.QueryAsync<AckVesselSummary>("SELECT VesselID, LastSuccessUtc AS LastSyncUtc FROM dbo.Sync_Vessel"))
                .ToDictionary(s => s.VesselID, s => s.LastSyncUtc);
            foreach (var v in vessels) v.LastSyncUtc = sync.TryGetValue(v.VesselID, out var t) ? t : null;
        }
        foreach (var v in vessels)
        {
            var mine = pending.Where(p => p.VesselID == v.VesselID).ToList();
            v.ManualsPending = mine.Count(p => p.ItemType == "Manual");
            v.ManualsOverdue = mine.Count(p => p.ItemType == "Manual" && p.Overdue);
            v.CircularsPending = mine.Count(p => p.ItemType == "Circular");
            v.CircularsOverdue = mine.Count(p => p.ItemType == "Circular" && p.Overdue);
            v.AlertsPending = mine.Count(p => p.ItemType == "Alert");
            v.AlertsOverdue = mine.Count(p => p.ItemType == "Alert" && p.Overdue);
        }
        return vessels;
    }

    public async Task<List<AckPendingRow>> GetOfficePendingAsync(string? userId)
        => (await _db.QueryAsync<AckPendingRow>(OfficePendingSql, ("@vessel", null), ("@user", userId)))
            .OrderBy(r => r.DueOn ?? DateTime.MaxValue).ThenBy(r => r.UserName).ToList();

    public async Task<FeatureSettingsDto> GetSettingsAsync()
    {
        static int Int(string? s, int fallback) => int.TryParse(s, out var n) ? n : fallback;
        return new FeatureSettingsDto
        {
            AllowedRolesToViewAllRankPendingItems = await _db.SettingAsync("DM_Settings", "AllowedRolesToViewAllRankPendingItems"),
            DefaultAckDueDaysManual = Int(await _db.SettingAsync("DM_Settings", "DefaultAckDueDays"), 7),
            DefaultAckDueDaysCircular = Int(await _db.SettingAsync("CIR_Settings", "DefaultAckDueDays_Circular"), 7),
            DefaultAckDueDaysAlert = Int(await _db.SettingAsync("CIR_Settings", "DefaultAckDueDays_Alert"), 1),
            FolderMaxFileSizeMB = Int(await _db.SettingAsync("DM_Settings", "FLD_MaxFileSizeMB"), 25),
            FolderMaxFiles = Int(await _db.SettingAsync("DM_Settings", "FLD_MaxFiles"), 1000),
        };
    }

    public async Task SaveSettingsAsync(FeatureSettingsDto s)
    {
        await _db.SetSettingAsync("DM_Settings", "AllowedRolesToViewAllRankPendingItems", s.AllowedRolesToViewAllRankPendingItems?.Trim());
        await _db.SetSettingAsync("DM_Settings", "DefaultAckDueDays", Math.Max(0, s.DefaultAckDueDaysManual).ToString());
        await _db.SetSettingAsync("CIR_Settings", "DefaultAckDueDays_Circular", Math.Max(0, s.DefaultAckDueDaysCircular).ToString());
        await _db.SetSettingAsync("CIR_Settings", "DefaultAckDueDays_Alert", Math.Max(0, s.DefaultAckDueDaysAlert).ToString());
        await _db.SetSettingAsync("DM_Settings", "FLD_MaxFileSizeMB", Math.Max(1, s.FolderMaxFileSizeMB).ToString());
        await _db.SetSettingAsync("DM_Settings", "FLD_MaxFiles", Math.Max(1, s.FolderMaxFiles).ToString());
    }
}
