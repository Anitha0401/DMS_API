using DMS.Data.EF.Models.Features;
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
        return _installed = await db.TableExistsAsync("Notification_Read");
    }

    /// <summary>The notification IDs of one module this user has read (notifications are listed for 30 days).</summary>
    public static async Task<HashSet<long>> ReadIdsAsync(DbContext db, string module, string userId)
    {
        var since = DateTime.UtcNow.AddDays(-35);
        var ids = await db.Set<NotificationRead>().AsNoTracking()
            .Where(r => r.Module == module && r.UserID == userId && r.ReadOn >= since)
            .Select(r => r.NotificationID)
            .ToListAsync();
        return ids.ToHashSet();
    }

    public static async Task MarkReadAsync(DbContext db, string module, long notificationId, string userId)
    {
        var reads = db.Set<NotificationRead>();
        if (await reads.AnyAsync(r => r.Module == module && r.NotificationID == notificationId && r.UserID == userId))
            return;
        var row = reads.Add(new NotificationRead { Module = module, NotificationID = notificationId, UserID = userId, ReadOn = DateTime.UtcNow });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            // Marked read by a second click at the same moment: already done.
        }
        finally
        {
            row.State = EntityState.Detached;
        }
    }
}

public static class DatabaseObjects
{
    /// <summary>True when dbo.{table} exists (stored procedure dbo.Api_TableExists).</summary>
    public static async Task<bool> TableExistsAsync(this DbContext db, string table)
    {
        var found = await db.Database
            .SqlQuery<int>($"EXEC dbo.Api_TableExists @TableName = {table}")
            .ToListAsync();
        return found.FirstOrDefault() == 1;
    }
}
