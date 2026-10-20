using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Features;

/// <summary>
/// Which notifications each office user has read (dbo.Notification_Read, Database/015_BackendFixes.sql).
/// Notifications are shared by everyone (UserID ''), so "read" has to be kept per user: one user opening a
/// notification no longer marks it read for all. Until the script is run, the old shared IsRead flag is used.
/// </summary>
public static class NotificationReads
{
    public const string Manual = "DM";
    public const string Circular = "CIR";

    /// <summary>Largest number of notifications one call returns.</summary>
    public const int MaxLimit = 100;

    private static bool _installed;

    public static int ClampLimit(int limit) => limit <= 0 ? 10 : Math.Min(limit, MaxLimit);

    public static async Task<bool> IsInstalledAsync(DbContext db)
    {
        if (_installed) return true;
        var found = await db.Database
            .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.Notification_Read', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .SingleAsync();
        return _installed = found == 1;
    }

    /// <summary>The notification IDs of one module this user has read (notifications are listed for 30 days).</summary>
    public static async Task<HashSet<long>> ReadIdsAsync(DbContext db, string module, string userId)
    {
        var ids = await db.Database
            .SqlQuery<long>($@"SELECT NotificationID AS [Value] FROM dbo.Notification_Read
                               WHERE Module = {module} AND UserID = {userId} AND ReadOn >= DATEADD(DAY, -35, SYSUTCDATETIME())")
            .ToListAsync();
        return ids.ToHashSet();
    }

    public static async Task MarkReadAsync(DbContext db, string module, long notificationId, string userId)
    {
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT dbo.Notification_Read (Module, NotificationID, UserID, ReadOn)
SELECT {module}, {notificationId}, {userId}, SYSUTCDATETIME()
 WHERE NOT EXISTS (SELECT 1 FROM dbo.Notification_Read WHERE Module = {module} AND NotificationID = {notificationId} AND UserID = {userId})");
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            // Marked read by a second click at the same moment: already done.
        }
    }
}
