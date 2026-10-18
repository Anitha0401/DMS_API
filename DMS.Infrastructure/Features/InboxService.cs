using DMS.Application.DTO.Features;
using DMS.Application.Interface;

namespace DMS.Data.EF.Features;

/// <summary>
/// Feature 2: the new-items inbox. Entries are written by triggers (012_NewFeatures.sql) when an item is released,
/// added or changed; an office user sees every entry not aimed at someone else. Read state per user in DMS_InboxRead
/// (office rows are office only, VesselID -2).
/// </summary>
public sealed class InboxService : IInboxService
{
    private readonly SqlDb _db;
    private static DateTime _lastExpiryCheckUtc = DateTime.MinValue;
    private static readonly SemaphoreSlim ExpiryLock = new(1, 1);

    public InboxService(SqlDb db) => _db = db;

    /// <summary>Certificate expiry entries, at most once an hour (the procedure itself never adds one twice).</summary>
    private async Task CheckCertificateExpiryAsync()
    {
        if (DateTime.UtcNow - _lastExpiryCheckUtc < TimeSpan.FromHours(1)) return;
        if (!await ExpiryLock.WaitAsync(0)) return;
        try
        {
            _lastExpiryCheckUtc = DateTime.UtcNow;
            await _db.ExecuteAsync("IF OBJECT_ID('dbo.Inbox_CertificateExpiry', 'P') IS NOT NULL EXEC dbo.Inbox_CertificateExpiry;");
        }
        finally { ExpiryLock.Release(); }
    }

    private const string Visible = "i.IsActive = 1 AND (i.TargetUserID IS NULL OR i.TargetUserID = @me)";

    public async Task<List<InboxItem>> GetAsync(OfficeUser user, bool unreadOnly, string? itemType, string? search, int take)
    {
        await CheckCertificateExpiryAsync();
        return await _db.QueryAsync<InboxItem>($@"
SELECT TOP (@take) i.DMS_InboxID, i.VesselID, i.ItemType, i.ItemID, i.VersionID, i.Action, i.Title, i.Priority, i.CreatedOn,
       CAST(CASE WHEN r.DMS_InboxReadID IS NULL THEN 0 ELSE 1 END AS bit) AS IsRead
  FROM dbo.DMS_Inbox i
  OUTER APPLY (SELECT TOP (1) x.DMS_InboxReadID FROM dbo.DMS_InboxRead x WHERE x.DMS_InboxID = i.DMS_InboxID AND x.UserID = @me) r
 WHERE {Visible}
   AND (@unread = 0 OR r.DMS_InboxReadID IS NULL)
   AND (@type IS NULL OR i.ItemType = @type)
   AND (@q IS NULL OR i.Title LIKE '%' + @q + '%')
 ORDER BY i.CreatedOn DESC, i.DMS_InboxID DESC",
            ("@take", Math.Clamp(take, 1, 1000)), ("@me", user.UserId), ("@unread", unreadOnly ? 1 : 0),
            ("@type", string.IsNullOrWhiteSpace(itemType) ? null : itemType),
            ("@q", string.IsNullOrWhiteSpace(search) ? null : search.Trim()));
    }

    public async Task<int> UnreadCountAsync(OfficeUser user)
    {
        await CheckCertificateExpiryAsync();
        return Convert.ToInt32(await _db.ScalarAsync($@"
SELECT COUNT(*) FROM dbo.DMS_Inbox i
 WHERE {Visible} AND NOT EXISTS (SELECT 1 FROM dbo.DMS_InboxRead r WHERE r.DMS_InboxID = i.DMS_InboxID AND r.UserID = @me)",
            ("@me", user.UserId)));
    }

    public async Task MarkReadAsync(OfficeUser user, long inboxId, bool read)
    {
        if (!read)
        {
            await _db.ExecuteAsync("DELETE FROM dbo.DMS_InboxRead WHERE DMS_InboxID = @id AND UserID = @me", ("@id", inboxId), ("@me", user.UserId));
            return;
        }
        var exists = await _db.ScalarAsync("SELECT 1 FROM dbo.DMS_InboxRead WHERE DMS_InboxID = @id AND UserID = @me",
            ("@id", inboxId), ("@me", user.UserId));
        if (exists != null) return;
        var id = await _db.NextIdAsync("DMS_InboxRead");
        await _db.ExecuteAsync(@"INSERT dbo.DMS_InboxRead (DMS_InboxReadID, Origin, VesselID, DMS_InboxID, UserID, ReadOn)
                                 VALUES (@id, 'HQ', -2, @inbox, @me, GETUTCDATE())",
            ("@id", id), ("@inbox", inboxId), ("@me", user.UserId));
    }

    public async Task MarkAllReadAsync(OfficeUser user)
    {
        var unread = await _db.QueryAsync<InboxItem>($@"
SELECT i.DMS_InboxID FROM dbo.DMS_Inbox i
 WHERE {Visible} AND NOT EXISTS (SELECT 1 FROM dbo.DMS_InboxRead r WHERE r.DMS_InboxID = i.DMS_InboxID AND r.UserID = @me)",
            ("@me", user.UserId));
        if (unread.Count == 0) return;
        var first = await _db.NextIdAsync("DMS_InboxRead", count: unread.Count);
        await _db.InTransactionAsync(async () =>
        {
            var next = first;
            foreach (var item in unread)
                await _db.ExecuteAsync(@"INSERT dbo.DMS_InboxRead (DMS_InboxReadID, Origin, VesselID, DMS_InboxID, UserID, ReadOn)
                                         VALUES (@id, 'HQ', -2, @inbox, @me, GETUTCDATE())",
                    ("@id", next++), ("@inbox", item.DMS_InboxID), ("@me", user.UserId));
        });
    }
}
