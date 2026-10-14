using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using DMS.Application.DTO.Sync;
using DMS.Application.Interface;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.Manuals;
using DMS.Data.EF.Models.OtherDocument;
using Microsoft.EntityFrameworkCore;

namespace DMS.Application;

/// <summary>
/// Office side of the vessel sync (api/Sync/...). Needs the tables from Database/005_VesselSync.sql.
///
/// What a vessel gets:
///   Manuals    every manual listed for the vessel in DM_Manual_ToAck_Vsl (VesselID = the vessel, or -1 for all
///              vessels) that has a released version (IsActive = 1), plus the manuals above it in the tree.
///   Circulars  every circular and alert listed for the vessel in CIR_ToAck_Vsl (vessel or -1), except Temp and Draft.
///   Crew       Vsl_Staff for the vessel. Categories: all manual and circular categories.
///
/// How "already received" is tracked (Sync_VesselItem): each item has a version key, a hash of everything the
/// vessel shows for it (text, version, attachments, ranks that must acknowledge ...). An item is sent while the key
/// differs from the key the vessel confirmed. A pull marks what it sends as pending; the vessel's next pull
/// (its "since" = ServerTimeUtc of the pull it applied) confirms them. A lost response is simply sent again.
/// </summary>
public class SyncService : ISyncService
{
    /// <summary>A first sync is sent in batches so one response never gets too big for a satellite link.</summary>
    private const int MaxItemsPerPull = 200;
    private const long MaxTextCharsPerPull = 4_000_000;

    private const string TypeManual = "Manual";
    private const string TypeCircular = "Circular";
    private const string TypeCrew = "Crew";
    private const string TypeCategories = "Categories";
    private const string TypeFixedTables = "FixedTables";
    private const string TypeOtherDocuments = "OtherDocuments";
    private const string DeletedKey = "DELETED";

    private readonly DmsContext _dms;
    private readonly CircularsContext _cir;
    private readonly GeneralContext _general;
    private readonly OtherDocumentsContext _other;

    public SyncService(DmsContext dms, CircularsContext cir, GeneralContext general, OtherDocumentsContext other)
    {
        _dms = dms;
        _cir = cir;
        _general = general;
        _other = other;
    }

    // =========================================================================================
    // Install check and vessel keys
    // =========================================================================================

    public async Task<bool> IsInstalledAsync()
    {
        var value = await ScalarAsync(
            "SELECT CASE WHEN OBJECT_ID('dbo.Sync_VesselItem', 'U') IS NULL OR OBJECT_ID('dbo.Sync_VesselKey', 'U') IS NULL " +
            "OR OBJECT_ID('dbo.Sync_Vessel', 'U') IS NULL OR OBJECT_ID('dbo.Sync_Log', 'U') IS NULL THEN 0 ELSE 1 END");
        return Convert.ToInt32(value) == 1;
    }

    public async Task<bool> ValidateVesselKeyAsync(int vesselId, string key)
    {
        if (vesselId <= 0 || string.IsNullOrWhiteSpace(key)) return false;

        var stored = await ScalarAsync(
            "SELECT KeyHash FROM dbo.Sync_VesselKey WHERE VesselID = @v AND IsActive = 1", ("@v", vesselId));
        if (stored is not string storedHash) return false;

        var given = Encoding.ASCII.GetBytes(HashKey(key));
        var expected = Encoding.ASCII.GetBytes(storedHash.Trim().ToUpperInvariant());
        if (!CryptographicOperations.FixedTimeEquals(given, expected)) return false;

        await ExecuteAsync("UPDATE dbo.Sync_VesselKey SET LastUsedUtc = SYSUTCDATETIME() WHERE VesselID = @v", ("@v", vesselId));
        return true;
    }

    public async Task<string> GenerateVesselKeyAsync(int vesselId, string createdBy)
    {
        var exists = await _general.VVessels.AsNoTracking().AnyAsync(v => v.VesselID == vesselId);
        if (!exists) throw new KeyNotFoundException($"Vessel {vesselId} does not exist.");

        // 32 random bytes, URL-safe text (43 characters). Only the hash is kept.
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        await ExecuteAsync(@"
MERGE dbo.Sync_VesselKey WITH (HOLDLOCK) AS t
USING (SELECT @v AS VesselID) AS s ON t.VesselID = s.VesselID
WHEN MATCHED THEN UPDATE SET KeyHash = @h, IsActive = 1, CreatedUtc = SYSUTCDATETIME(), CreatedBy = @by, LastUsedUtc = NULL
WHEN NOT MATCHED THEN INSERT (VesselID, KeyHash, IsActive, CreatedUtc, CreatedBy) VALUES (@v, @h, 1, SYSUTCDATETIME(), @by);",
            ("@v", vesselId), ("@h", HashKey(key)), ("@by", Truncate(createdBy, 150)));

        return key;
    }

    public async Task RequestResyncAsync(int vesselId)
    {
        await ExecuteAsync("DELETE FROM dbo.Sync_VesselItem WHERE VesselID = @v", ("@v", vesselId));
    }

    // =========================================================================================
    // Pull
    // =========================================================================================

    public async Task<SyncPullResponse> PullAsync(int vesselId, DateTime since)
    {
        var started = DateTime.UtcNow;
        try
        {
            var response = await BuildPullAsync(vesselId, ToUtc(since));
            var count = response.Manuals.Count + response.Circulars.Count +
                        response.DeletedManualIds.Count + response.DeletedCircularIds.Count;
            await RecordCallAsync(vesselId, "Down", started, count, null);
            return response;
        }
        catch (Exception ex)
        {
            await TryRecordCallAsync(vesselId, "Down", started, ex.Message);
            throw;
        }
    }

    private async Task<SyncPullResponse> BuildPullAsync(int vesselId, DateTime since)
    {
        // Whole milliseconds, so the value the vessel sends back matches PendingUtc (datetime2(3)) exactly.
        var now = DateTime.UtcNow;
        var serverTime = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        var response = new SyncPullResponse { ServerTimeUtc = serverTime };

        // 1. No "since": the vessel has nothing yet (first sync, new or rebuilt database on board).
        //    Forget what it was sent before, so everything goes again.
        //    Otherwise everything sent in the pull the vessel has applied is now on board.
        if (since <= DateTime.MinValue.AddDays(1))
        {
            await ExecuteAsync("DELETE FROM dbo.Sync_VesselItem WHERE VesselID = @v", ("@v", vesselId));
        }
        else
        {
            // Normally already done by api/Sync/Confirm; repeated here in case that call was lost.
            await ConfirmAppliedAsync(vesselId, since);
        }

        // Change table (Sync_Outbox, filled by triggers on insert, edit and delete at HQ):
        // rows without the DownloadedUtc marker are waiting for this vessel.
        var outboxInstalled = await IsOutboxInstalledAsync();
        var outbox = new HashSet<(string Type, long Id)>();
        if (outboxInstalled)
            outbox = await LoadOutboxAsync(vesselId);

        var tracked = await LoadTrackedAsync(vesselId);
        // Until the vessel has confirmed at least one item (first sync, or after "Force resync"), send the full
        // list of what it should hold - again if the previous response was lost on the way.
        var firstSync = !tracked.Values.Any(t => (t.Type == TypeManual || t.Type == TypeCircular) && t.Confirmed != null);

        // 2. What the vessel should have, with the current version key of each item.
        var catalog = await LoadCatalogAsync(new[] { vesselId });
        var expected = catalog.ExpectedFor(vesselId);

        string? Confirmed((string, long) k) => tracked.TryGetValue(k, out var t) ? t.Confirmed : null;

        // 3. New or changed items, alerts and high-priority circulars first, manuals from the top of the tree down.
        // An item is sent when the change table has a row waiting for it, or (safety net) when its
        // version key differs from the version the vessel confirmed.
        var changed = expected
            .Where(e => outbox.Contains((e.Type, e.Id)) || Confirmed((e.Type, e.Id)) != e.Key)
            .OrderBy(e => e.Order)
            .ToList();

        var toSend = new List<ExpectedItem>();
        long textBudget = 0;
        foreach (var item in changed)
        {
            if (toSend.Count >= MaxItemsPerPull) break;
            if (toSend.Count > 0 && textBudget + item.TextLength > MaxTextCharsPerPull) break;
            toSend.Add(item);
            textBudget += item.TextLength;
        }
        response.HasMore = toSend.Count < changed.Count;

        // 4. Items the vessel has (or was sent) that it should no longer have.
        var expectedKeys = expected.Select(e => (e.Type, e.Id)).ToHashSet();
        var removed = tracked.Keys
            .Where(k => (k.Type == TypeManual || k.Type == TypeCircular) && !expectedKeys.Contains(k))
            .Union(outbox.Where(k => (k.Type == TypeManual || k.Type == TypeCircular) && !expectedKeys.Contains(k)))
            .ToList();
        response.DeletedManualIds = removed.Where(k => k.Type == TypeManual).Select(k => k.Id).ToList();
        response.DeletedCircularIds = removed.Where(k => k.Type == TypeCircular).Select(k => k.Id).ToList();

        // 5. Full details for what is being sent.
        var manualIds = toSend.Where(i => i.Type == TypeManual).Select(i => i.Id).ToList();
        var circularIds = toSend.Where(i => i.Type == TypeCircular).Select(i => i.Id).ToList();
        response.Manuals = await LoadManualsAsync(catalog, manualIds, vesselId);
        response.Circulars = await LoadCircularsAsync(circularIds, vesselId);

        foreach (var id in manualIds)
            foreach (var rank in catalog.ManualRanks(vesselId, id))
                response.ManualAckRequirements.Add(new SyncManualAckRequirementDto { DM_ManualID = id, UserRank = rank });
        foreach (var id in circularIds)
            foreach (var rank in catalog.CircularRanks(vesselId, id))
                response.CircularAckRequirements.Add(new SyncCircularAckRequirementDto { CIR_MasterID = id, UserRank = rank });

        // 6. Crew and categories go as whole lists, only when they changed.
        var pendingRows = new List<(string Type, long Id, string Key)>();

        var crew = await LoadCrewAsync(vesselId);
        var crewKey = Hash(string.Join("\n", crew.OrderBy(c => c.UserId, StringComparer.Ordinal)
            .Select(c => $"{c.VslStaffId}|{c.UserId}|{c.UserName}|{c.UserRank}|{c.UserDept}|{c.IsDisabled}|{c.Password}")));
        if (outbox.Contains((TypeCrew, 0)) || Confirmed((TypeCrew, 0)) != crewKey)
        {
            response.Crew = crew;
            pendingRows.Add((TypeCrew, 0, crewKey));
        }

        var (manualCategories, circularCategories) = await LoadCategoriesAsync();
        var categoriesKey = Hash(System.Text.Json.JsonSerializer.Serialize(new
        {
            m = manualCategories.OrderBy(c => c.DM_CategoryID),
            c = circularCategories.OrderBy(c => c.CIR_CategoryID),
        }));
        if (outbox.Contains((TypeCategories, 0)) || Confirmed((TypeCategories, 0)) != categoriesKey)
        {
            response.ManualCategories = manualCategories;
            response.CircularCategories = circularCategories;
            pendingRows.Add((TypeCategories, 0, categoriesKey));
        }

        // Fixed tables (DM_Status, DM_Settings, CIR_Setting, V_Vessel): copied whole, every row, only when changed.
        // They are small, so the version key is worked out on every pull; no trigger is needed.
        var fixedTables = await LoadFixedTablesAsync();
        var fixedKey = Hash(System.Text.Json.JsonSerializer.Serialize(fixedTables));
        if (outbox.Contains((TypeFixedTables, 0)) || Confirmed((TypeFixedTables, 0)) != fixedKey)
        {
            response.DmStatuses = fixedTables.DmStatuses;
            response.DmSettings = fixedTables.DmSettings;
            response.CirSettings = fixedTables.CirSettings;
            response.Vessels = fixedTables.Vessels;
            pendingRows.Add((TypeFixedTables, 0, fixedKey));
        }

        // Other documents for this vessel: sent whole when anything in them changed.
        var otherDocuments = await LoadOtherDocumentsAsync(vesselId);
        var otherKey = Hash(System.Text.Json.JsonSerializer.Serialize(otherDocuments));
        if (outbox.Contains((TypeOtherDocuments, 0)) || Confirmed((TypeOtherDocuments, 0)) != otherKey)
        {
            response.OtherDocuments = otherDocuments;
            pendingRows.Add((TypeOtherDocuments, 0, otherKey));
        }

        // 7. First sync (or a forced resync): tell the vessel everything it should hold, so it drops anything else.
        if (firstSync)
        {
            response.AllManualIds = expected.Where(e => e.Type == TypeManual).Select(e => e.Id).ToList();
            response.AllCircularIds = expected.Where(e => e.Type == TypeCircular).Select(e => e.Id).ToList();
        }

        // 8. Mark what was sent as pending until the vessel confirms it.
        // Items whose details could not be loaded (deleted in the meantime) are left for the next pull.
        var sentManuals = response.Manuals.Select(m => m.DM_ManualID).ToHashSet();
        var sentCirculars = response.Circulars.Select(c => c.CIR_MasterID).ToHashSet();
        pendingRows.AddRange(toSend
            .Where(i => i.Type == TypeManual ? sentManuals.Contains(i.Id) : sentCirculars.Contains(i.Id))
            .Select(i => (i.Type, i.Id, i.Key)));
        pendingRows.AddRange(removed.Select(k => (k.Type, k.Id, DeletedKey)));
        await MarkPendingAsync(vesselId, pendingRows, serverTime);

        // Change-table rows for what this response carries are now sent; the vessel's next pull confirms them.
        if (outboxInstalled && outbox.Count > 0)
        {
            var sent = pendingRows.Select(r => (r.Type, r.Id)).Where(outbox.Contains).ToList();
            response.SyncRows = await MarkOutboxSentAsync(vesselId, sent, serverTime);
        }

        return response;
    }

    // =========================================================================================
    // Change table (dbo.Sync_Outbox, Database/006_VesselSyncOutbox.sql)
    // =========================================================================================

    private bool? _outboxInstalled;

    private async Task<bool> IsOutboxInstalledAsync()
    {
        _outboxInstalled ??= Convert.ToInt32(await ScalarAsync(
            "SELECT CASE WHEN OBJECT_ID('dbo.Sync_Outbox', 'U') IS NULL THEN 0 ELSE 1 END")) == 1;
        return _outboxInstalled.Value;
    }

    /// <summary>Items with a change-table row not yet downloaded by the vessel (or by every vessel when null).</summary>
    private async Task<List<(int VesselId, string Type, long Id)>> LoadOutboxRowsAsync(int? vesselId)
    {
        var rows = new List<(int, string, long)>();
        await WithConnectionAsync(async connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT VesselID, ItemType, ItemID FROM dbo.Sync_Outbox WHERE DownloadedUtc IS NULL" +
                              (vesselId == null ? "" : " AND VesselID = @v");
            if (vesselId != null) AddParameter(cmd, "@v", vesselId.Value);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                rows.Add((r.GetInt32(0), r.GetString(1).Trim(), r.GetInt64(2)));
        });
        return rows;
    }

    private async Task<HashSet<(string Type, long Id)>> LoadOutboxAsync(int vesselId) =>
        (await LoadOutboxRowsAsync(vesselId)).Select(r => (r.Type, r.Id)).ToHashSet();

    /// <summary>Sets SentUtc on the waiting rows of the items being sent and returns those rows for the response.</summary>
    private async Task<List<SyncOutboxRowDto>> MarkOutboxSentAsync(int vesselId, List<(string Type, long Id)> items, DateTime serverTime)
    {
        var rows = new List<SyncOutboxRowDto>();
        foreach (var chunk in items.Chunk(500))
        {
            var values = new StringBuilder();
            var parameters = new List<(string, object?)> { ("@v", vesselId), ("@now", serverTime) };
            for (var i = 0; i < chunk.Length; i++)
            {
                if (i > 0) values.Append(',');
                values.Append($"(@t{i}, @i{i})");
                parameters.Add(($"@t{i}", chunk[i].Type));
                parameters.Add(($"@i{i}", chunk[i].Id));
            }
            await WithConnectionAsync(async connection =>
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
UPDATE o SET SentUtc = @now
OUTPUT inserted.SyncOutboxID, inserted.ItemType, inserted.ItemID, inserted.Action, inserted.CreatedUtc
  FROM dbo.Sync_Outbox o
  JOIN (VALUES {values}) AS s (ItemType, ItemID) ON s.ItemType = o.ItemType AND s.ItemID = o.ItemID
 WHERE o.VesselID = @v AND o.DownloadedUtc IS NULL;";
                foreach (var (name, value) in parameters) AddParameter(cmd, name, value);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    rows.Add(new SyncOutboxRowDto
                    {
                        SyncOutboxID = r.GetInt64(0),
                        ItemType = r.GetString(1).Trim(),
                        ItemID = r.GetInt64(2),
                        Action = r.GetString(3).Trim(),
                        ChangedUtc = DateTime.SpecifyKind(r.GetDateTime(4), DateTimeKind.Utc),
                    });
                }
            });
        }

        // Keep the table small: downloaded rows older than 30 days are no longer needed.
        await ExecuteAsync("DELETE TOP (5000) FROM dbo.Sync_Outbox WHERE DownloadedUtc < DATEADD(day, -30, SYSUTCDATETIME());");
        return rows.OrderBy(x => x.SyncOutboxID).ToList();
    }

    /// <summary>
    /// Everything sent up to and including the pull answered at <paramref name="appliedServerTime"/> is on board:
    /// pending version keys become confirmed (removals are forgotten), and change-table rows sent by then get
    /// their DownloadedUtc marker.
    /// </summary>
    private async Task ConfirmAppliedAsync(int vesselId, DateTime appliedServerTime)
    {
        await ExecuteAsync(@"
DELETE FROM dbo.Sync_VesselItem
 WHERE VesselID = @v AND PendingKey = @del AND PendingUtc <= @since;
UPDATE dbo.Sync_VesselItem
   SET ConfirmedKey = PendingKey, ConfirmedUtc = PendingUtc, PendingKey = NULL, PendingUtc = NULL
 WHERE VesselID = @v AND PendingKey IS NOT NULL AND PendingUtc <= @since;",
            ("@v", vesselId), ("@del", DeletedKey), ("@since", appliedServerTime));

        if (await IsOutboxInstalledAsync())
        {
            await ExecuteAsync(@"
UPDATE dbo.Sync_Outbox SET DownloadedUtc = SYSUTCDATETIME()
 WHERE VesselID = @v AND DownloadedUtc IS NULL AND SentUtc IS NOT NULL AND SentUtc <= @since;",
                ("@v", vesselId), ("@since", appliedServerTime));
        }
    }

    // =========================================================================================
    // Confirm (the vessel has applied a pull)
    // =========================================================================================

    public async Task<SyncConfirmResult> ConfirmAsync(SyncConfirmRequest request)
    {
        var result = new SyncConfirmResult();
        if (!await IsInstalledAsync()) return result;
        var vesselId = request.VesselID;

        // 1. The rows the vessel names: set the marker (only its own rows, only rows that were sent).
        if (await IsOutboxInstalledAsync())
        {
            foreach (var chunk in request.SyncOutboxIds.Where(id => id > 0).Distinct().Chunk(500))
            {
                var names = new List<string>();
                var parameters = new List<(string, object?)> { ("@v", vesselId) };
                for (var i = 0; i < chunk.Length; i++)
                {
                    names.Add($"@id{i}");
                    parameters.Add(($"@id{i}", chunk[i]));
                }
                var count = await ScalarAsync($@"
UPDATE dbo.Sync_Outbox SET DownloadedUtc = SYSUTCDATETIME()
 WHERE VesselID = @v AND DownloadedUtc IS NULL AND SentUtc IS NOT NULL
   AND SyncOutboxID IN ({string.Join(",", names)});
SELECT @@ROWCOUNT;", parameters.ToArray());
                result.Confirmed += Convert.ToInt32(count);
            }
        }

        // 2. Everything that pull sent is on board (version keys, and any row the vessel did not list).
        var applied = ToUtc(request.ServerTimeUtc);
        if (applied > DateTime.MinValue.AddDays(1) && applied <= DateTime.UtcNow.AddMinutes(1))
            await ConfirmAppliedAsync(vesselId, applied);

        return result;
    }

    // =========================================================================================
    // Push (acknowledgements made on board)
    // =========================================================================================

    public async Task<SyncPushResult> PushAsync(SyncPushRequest request)
    {
        var started = DateTime.UtcNow;
        try
        {
            var result = await ApplyPushAsync(request);
            await RecordCallAsync(request.VesselID, "Up", started, result.ManualAcksAccepted + result.CircularAcksAccepted, null);
            return result;
        }
        catch (Exception ex)
        {
            await TryRecordCallAsync(request.VesselID, "Up", started, ex.Message);
            throw;
        }
    }

    private async Task<SyncPushResult> ApplyPushAsync(SyncPushRequest request)
    {
        var result = new SyncPushResult();
        var vesselId = request.VesselID;

        // ---- Manuals ----
        if (request.ManualAcks.Count > 0)
        {
            var versionIds = request.ManualAcks.Select(a => a.DM_ManualVersionID).Distinct().ToList();
            var versions = await _dms.DmManualVersions.AsNoTracking()
                .Where(v => versionIds.Contains(v.DM_ManualVersionID))
                .Select(v => new { v.DM_ManualVersionID, v.DM_ManualID, v.Version })
                .ToDictionaryAsync(v => v.DM_ManualVersionID);
            var existing = (await _dms.DmManualUserAcks.AsNoTracking()
                    .Where(a => a.VesselID == vesselId && versionIds.Contains(a.DM_ManualVersionID))
                    .Select(a => new { a.DM_ManualVersionID, a.UserId })
                    .ToListAsync())
                .Select(a => (a.DM_ManualVersionID, a.UserId.Trim().ToUpperInvariant()))
                .ToHashSet();

            // DM_Manual_UserAck has no identity column: the next ID is MAX + 1.
            var nextId = (await _dms.DmManualUserAcks.MaxAsync(a => (long?)a.DmManualUserAckId) ?? 0) + 1;

            foreach (var ack in request.ManualAcks)
            {
                if (string.IsNullOrWhiteSpace(ack.UserId)) continue;
                if (!versions.TryGetValue(ack.DM_ManualVersionID, out var version))
                {
                    result.Warnings.Add($"Manual acknowledgement for unknown version {ack.DM_ManualVersionID} was skipped.");
                    result.SkippedManualAcks.Add($"{ack.DM_ManualVersionID}:{ack.UserId}");
                    continue;
                }
                if (!existing.Add((ack.DM_ManualVersionID, ack.UserId.Trim().ToUpperInvariant())))
                {
                    result.ManualAcksAlreadyApplied++;
                    continue;
                }

                _dms.DmManualUserAcks.Add(new DmManualUserAck
                {
                    DmManualUserAckId = nextId++,
                    Origin = "VSL",
                    VesselID = vesselId,
                    DM_ManualID = version.DM_ManualID,
                    DM_ManualVersionID = ack.DM_ManualVersionID,
                    Version = LeadingNumber(version.Version) ?? ack.Version,
                    UserId = ack.UserId.Trim(),
                    UserRank = ack.UserRank,
                    UserName = ack.UserName,
                    DateRead = ToUtc(ack.DateRead),
                    Comments = Truncate(ack.Comments, 500),
                    IsActive = 1,
                });
                result.ManualAcksAccepted++;
            }
            await _dms.SaveChangesAsync();
        }

        // ---- Circulars ----
        if (request.CircularAcks.Count > 0)
        {
            var crew = (await _general.VslStaffs.AsNoTracking()
                    .Where(s => s.VesselID == vesselId)
                    .Select(s => new { s.UserId, s.VslStaffId })
                    .ToListAsync())
                .GroupBy(s => s.UserId.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().VslStaffId, StringComparer.OrdinalIgnoreCase);

            var circularIds = request.CircularAcks.Select(a => a.CIR_MasterID).Distinct().ToList();
            var knownCirculars = (await _cir.CIR_Master.AsNoTracking()
                .Where(c => circularIds.Contains(c.CIR_MasterID))
                .Select(c => c.CIR_MasterID)
                .ToListAsync()).ToHashSet();
            var existing = (await _cir.CIR_UserAck.AsNoTracking()
                    .Where(a => a.VesselID == vesselId && a.CIR_MasterID != null && circularIds.Contains(a.CIR_MasterID.Value))
                    .Select(a => new { a.CIR_MasterID, a.Fleet_CrewID })
                    .ToListAsync())
                .Select(a => (a.CIR_MasterID!.Value, a.Fleet_CrewID ?? 0))
                .ToHashSet();

            var nextId = (await _cir.CIR_UserAck.MaxAsync(a => (long?)a.CIR_UserAckID) ?? 0) + 1;

            foreach (var ack in request.CircularAcks)
            {
                if (!crew.TryGetValue((ack.UserId ?? "").Trim(), out var crewId))
                {
                    result.Warnings.Add($"Circular acknowledgement by unknown crew member '{ack.UserId}' was skipped.");
                    result.SkippedCircularAcks.Add($"{ack.CIR_MasterID}:{ack.UserId}");
                    continue;
                }
                if (!knownCirculars.Contains(ack.CIR_MasterID))
                {
                    result.Warnings.Add($"Acknowledgement for unknown circular {ack.CIR_MasterID} was skipped.");
                    result.SkippedCircularAcks.Add($"{ack.CIR_MasterID}:{ack.UserId}");
                    continue;
                }
                if (!existing.Add((ack.CIR_MasterID, crewId)))
                {
                    result.CircularAcksAlreadyApplied++;
                    continue;
                }

                _cir.CIR_UserAck.Add(new CIR_UserAck
                {
                    CIR_UserAckID = nextId++,
                    Origin = "VSL",
                    VesselID = vesselId,
                    CIR_MasterID = ack.CIR_MasterID,
                    Fleet_CrewID = crewId,
                    DateRead = ToUtc(ack.DateRead),
                    Comments = Truncate(ack.Comments, 500),
                    IsActive = 1,
                });
                result.CircularAcksAccepted++;
            }
            await _cir.SaveChangesAsync();
        }

        // The office rows for what was sent (new or already stored), so the vessel can replace its own
        // pending rows (negative IDs) with them.
        if (request.ManualAcks.Count > 0)
        {
            var versionIds = request.ManualAcks.Select(a => a.DM_ManualVersionID).Distinct().ToList();
            var users = request.ManualAcks.Select(a => (a.UserId ?? "").Trim().ToUpperInvariant()).ToHashSet();
            result.ManualAckRows = (await _dms.DmManualUserAcks.AsNoTracking()
                    .Where(a => a.VesselID == vesselId && versionIds.Contains(a.DM_ManualVersionID))
                    .Select(a => new SyncDMManualUserAckRow
                    {
                        DM_Manual_UserAckID = a.DmManualUserAckId,
                        Origin = a.Origin,
                        VesselID = a.VesselID,
                        DM_ManualID = a.DM_ManualID,
                        DM_ManualVersionID = a.DM_ManualVersionID,
                        Version = a.Version,
                        UserRank = a.UserRank,
                        UserName = a.UserName,
                        UserID = a.UserId,
                        DateRead = a.DateRead,
                        Comments = a.Comments,
                        IsActive = a.IsActive,
                    })
                    .ToListAsync())
                .Where(r => users.Contains(r.UserID.Trim().ToUpperInvariant()))
                .ToList();
        }
        if (request.CircularAcks.Count > 0)
        {
            var circularIds = request.CircularAcks.Select(a => a.CIR_MasterID).Distinct().ToList();
            result.CircularAckRows = await _cir.CIR_UserAck.AsNoTracking()
                .Where(a => a.VesselID == vesselId && a.CIR_MasterID != null && circularIds.Contains(a.CIR_MasterID.Value))
                .Select(a => new SyncCIRUserAckRow
                {
                    CIR_UserAckID = a.CIR_UserAckID,
                    Origin = a.Origin,
                    VesselID = a.VesselID,
                    CIR_MasterID = a.CIR_MasterID,
                    Fleet_CrewID = a.Fleet_CrewID,
                    DateRead = a.DateRead,
                    Comments = a.Comments,
                    IsActive = a.IsActive,
                })
                .ToListAsync();
        }

        return result;
    }

    // =========================================================================================
    // Push (other documents added on board)
    // =========================================================================================

    public async Task<OtherDocumentPushResult> PushOtherDocumentAsync(int vesselId, OtherDocumentPushDto doc, IReadOnlyList<byte[]> files)
    {
        var started = DateTime.UtcNow;
        try
        {
            var result = await ApplyOtherDocumentPushAsync(vesselId, doc, files);
            await RecordCallAsync(vesselId, "Up", started, result.Stored && !result.AlreadyApplied ? 1 : 0, result.Error);
            return result;
        }
        catch (Exception ex)
        {
            await TryRecordCallAsync(vesselId, "Up", started, ex.Message);
            throw;
        }
    }

    private async Task<OtherDocumentPushResult> ApplyOtherDocumentPushAsync(int vesselId, OtherDocumentPushDto doc, IReadOnlyList<byte[]> files)
    {
        var number = (doc.DocumentNumber ?? "").Trim();
        var title = (doc.DocumentTitle ?? "").Trim();
        if (number.Length == 0 || title.Length == 0)
            return new OtherDocumentPushResult { Error = "The document has no number or title." };
        if (doc.Files.Count != files.Count)
            return new OtherDocumentPushResult { Error = $"{doc.Files.Count} file(s) described but {files.Count} sent." };
        if (!await _other.OtherDocument_Type.AsNoTracking().AnyAsync(t => t.OtherDocument_TypeID == doc.OtherDocument_TypeID))
            return new OtherDocumentPushResult { Error = $"Document type {doc.OtherDocument_TypeID} is not known at the office." };

        // Sent before (the answer was lost on the way back): return what is held.
        var held = await _other.OtherDocumentMaster.AsNoTracking()
            .Where(m => m.VesselID == vesselId && m.Origin == "VSL" && (m.IsDeleted ?? 0) != 1
                        && m.OtherDocument_TypeID == doc.OtherDocument_TypeID && m.DocumentNumber == number)
            .Select(m => (long?)m.OtherDocumentMasterID)
            .FirstOrDefaultAsync();
        if (held != null)
            return await OtherDocumentPushResultAsync(held.Value, doc, alreadyApplied: true);

        var uploadedOn = doc.UploadedOn.HasValue ? ToUtc(doc.UploadedOn.Value) : DateTime.UtcNow;
        // OtherDocumentMaster and OtherDocument_Blob have no identity column: the next ID is MAX + 1.
        var masterId = (await _other.OtherDocumentMaster.MaxAsync(m => (long?)m.OtherDocumentMasterID) ?? 0) + 1;
        _other.OtherDocumentMaster.Add(new OtherDocumentMaster
        {
            OtherDocumentMasterID = masterId,
            Origin = "VSL",
            VesselID = vesselId,
            OtherDocument_TypeID = doc.OtherDocument_TypeID,
            DocumentNumber = Truncate(number, 50)!,
            DocumentTitle = Truncate(title, 500)!,
            Status = Truncate(doc.Status?.Trim(), 50),
            Reference = Truncate(doc.Reference?.Trim(), 150),
            Comments = Truncate(doc.Comments?.Trim(), 500),
            UploadedOn = uploadedOn,
            UploadedBy = Truncate(doc.UploadedBy?.Trim(), 150),
            IsDeleted = 0,
            ModifiedBy = Truncate(doc.UploadedBy?.Trim(), 50),
            ModifiedDate = uploadedOn,
        });

        var blobId = (await _other.OtherDocument_Blob.MaxAsync(b => (long?)b.OtherDocument_BlobID) ?? 0) + 1;
        for (var i = 0; i < files.Count; i++)
        {
            var f = doc.Files[i];
            _other.OtherDocument_Blob.Add(new OtherDocument_Blob
            {
                OtherDocument_BlobID = blobId++,
                OtherDocumentMasterID = masterId,
                Origin = "VSL",
                VesselID = vesselId,
                Description = Truncate(f.Description ?? f.FileName, 150),
                FileName = Truncate(f.FileName, 150),
                FileType = Truncate(f.FileType, 50),
                OriginalFileType = Truncate(f.OriginalFileType, 50),
                BlobSize = files[i].Length,
                BlobContents = files[i],
                AddedOn = f.AddedOn.HasValue ? ToUtc(f.AddedOn.Value) : DateTime.UtcNow,
                HQ_Storage_Option = "Database",
                HQ_Local_Folder = "",
                HQ_DatabaseName = "",
                VSL_Storage_Option = "Database",
                VSL_Local_Folder = "",
                VSL_DatabaseName = "",
            });
        }

        var logId = (await _other.OtherDocument_Log.MaxAsync(l => (long?)l.OtherDocument_LogID) ?? 0) + 1;
        _other.OtherDocument_Log.Add(new OtherDocument_Log
        {
            OtherDocument_LogID = logId,
            Origin = "VSL",
            VesselID = vesselId,
            OtherDocumentMasterID = masterId,
            LogDate = DateTime.Now,
            Activity = Truncate("New " + number + " Added on board", 150)!,
            Remarks = Truncate($"{number}: {doc.Reference} (added on vessel {vesselId} by {doc.UploadedBy}, {files.Count} file(s))", 3000),
            UserName = Truncate(doc.UploadedBy, 150),
            UserRank = "",
        });

        await _other.SaveChangesAsync(); // one transaction: the document, its files and the log row, or nothing
        _other.ChangeTracker.Clear();     // the file bytes are not needed in memory any longer

        return await OtherDocumentPushResultAsync(masterId, doc, alreadyApplied: false);
    }

    /// <summary>The office rows for a document a vessel sent, read back as stored (datetime precision included).</summary>
    private async Task<OtherDocumentPushResult> OtherDocumentPushResultAsync(long masterId, OtherDocumentPushDto doc, bool alreadyApplied)
    {
        var m = await _other.OtherDocumentMaster.AsNoTracking().FirstAsync(x => x.OtherDocumentMasterID == masterId);
        var blobs = await _other.OtherDocument_Blob.AsNoTracking()
            .Where(b => b.OtherDocumentMasterID == masterId)
            .OrderBy(b => b.OtherDocument_BlobID)
            .Select(b => new SyncOtherDocumentBlobRow
            {
                OtherDocument_BlobID = b.OtherDocument_BlobID,
                OtherDocumentMasterID = b.OtherDocumentMasterID,
                Origin = b.Origin,
                VesselID = b.VesselID,
                Description = b.Description,
                FileName = b.FileName,
                FileType = b.FileType,
                OriginalFileType = b.OriginalFileType,
                BlobSize = b.BlobSize,
                AddedOn = b.AddedOn,
                Cloud_ContainerName = b.Cloud_ContainerName,
                Cloud_Directory = b.Cloud_Directory,
                HQ_Storage_Option = b.HQ_Storage_Option,
                HQ_DatabaseName = b.HQ_DatabaseName,
                HQ_Local_Folder = b.HQ_Local_Folder,
                VSL_Storage_Option = b.VSL_Storage_Option,
                VSL_DatabaseName = b.VSL_DatabaseName,
                VSL_Local_Folder = b.VSL_Local_Folder,
            })
            .ToListAsync();

        // Stored in the order sent, so file i is doc.Files[i]. On a repeat, also match by name and size.
        var vesselIds = new List<long>();
        var unmatched = doc.Files.ToList();
        foreach (var b in blobs)
        {
            var f = unmatched.FirstOrDefault(x => x.FileName == b.FileName && x.BlobSize == b.BlobSize) ?? unmatched.FirstOrDefault();
            if (f != null) unmatched.Remove(f);
            vesselIds.Add(f?.VesselFileID ?? 0);
        }

        return new OtherDocumentPushResult
        {
            Stored = true,
            AlreadyApplied = alreadyApplied,
            Document = new SyncOtherDocumentMasterRow
            {
                OtherDocumentMasterID = m.OtherDocumentMasterID,
                Origin = m.Origin,
                VesselID = m.VesselID,
                OtherDocument_TypeID = m.OtherDocument_TypeID,
                DocumentNumber = m.DocumentNumber,
                DocumentTitle = m.DocumentTitle,
                Status = m.Status,
                Reference = m.Reference,
                Comments = m.Comments,
                UploadedOn = m.UploadedOn,
                UploadedBy = m.UploadedBy,
                IsDeleted = m.IsDeleted,
                ModifiedBy = m.ModifiedBy,
                ModifiedDate = m.ModifiedDate,
            },
            Files = blobs,
            VesselFileIDs = vesselIds,
        };
    }

    // =========================================================================================
    // Heartbeat and attachments
    // =========================================================================================

    public async Task HeartbeatAsync(SyncHeartbeatRequest request)
    {
        var pending = Math.Max(0, request.PendingManualAcks) + Math.Max(0, request.PendingCircularAcks);
        int? progress = request.ProgressPercent is int p ? Math.Clamp(p, 0, 100) : null;
        var failed = !request.LastCycleSucceeded;

        await ExecuteAsync(@"
MERGE dbo.Sync_Vessel WITH (HOLDLOCK) AS t
USING (SELECT @v AS VesselID) AS s ON t.VesselID = s.VesselID
WHEN NOT MATCHED THEN INSERT (VesselID, PendingOnShip) VALUES (@v, 0);
UPDATE dbo.Sync_Vessel
   SET LastContactUtc = SYSUTCDATETIME(),
       PendingOnShip = @pending,
       CurrentActivity = @activity,
       ProgressPercent = @progress,
       AppVersion = COALESCE(@app, AppVersion),
       LastStatus = CASE WHEN @failed = 1 THEN 'Failed' ELSE LastStatus END,
       LastError = CASE WHEN @failed = 1 THEN @error ELSE LastError END
 WHERE VesselID = @v;",
            ("@v", request.VesselID), ("@pending", pending), ("@activity", Truncate(request.Activity, 100)),
            ("@progress", progress), ("@app", Truncate(request.AppVersion, 50)), ("@failed", failed ? 1 : 0),
            ("@error", Truncate(request.LastError, 1000)));
    }

    public async Task<(byte[] Bytes, string FileType, string? FileName)?> GetManualAttachmentBytesAsync(int vesselId, long attachmentId)
    {
        var a = await _dms.DmManualAttachments.AsNoTracking()
            .Where(x => x.DmManualAttachmentsId == attachmentId && (x.IsDeleted ?? 0) != 1)
            .Select(x => new { x.DM_ManualID, x.BlobContents, x.FileType })
            .FirstOrDefaultAsync();
        if (a?.BlobContents == null) return null;
        if (!await IsTrackedAsync(vesselId, TypeManual, a.DM_ManualID)) return null; // not sent to this vessel
        return (a.BlobContents, a.FileType, null);
    }

    public async Task<(byte[] Bytes, string FileType, string? FileName)?> GetCircularAttachmentBytesAsync(int vesselId, long attachmentId)
    {
        var b = await _cir.CIR_Blob.AsNoTracking()
            .Where(x => x.CIR_BlobID == attachmentId)
            .Select(x => new { x.CIR_MasterID, x.BlobContents, x.FileType, x.FileName })
            .FirstOrDefaultAsync();
        if (b?.BlobContents == null || b.CIR_MasterID == null) return null;
        if (!await IsTrackedAsync(vesselId, TypeCircular, b.CIR_MasterID.Value)) return null;
        return (b.BlobContents, b.FileType ?? "application/octet-stream", b.FileName);
    }

    public async Task<(byte[] Bytes, string FileType, string? FileName)?> GetOtherDocumentFileBytesAsync(int vesselId, long blobId)
    {
        var f = await (
                from b in _other.OtherDocument_Blob.AsNoTracking()
                join m in _other.OtherDocumentMaster.AsNoTracking() on b.OtherDocumentMasterID equals m.OtherDocumentMasterID
                where b.OtherDocument_BlobID == blobId && (m.VesselID == vesselId || m.VesselID == -1) && (m.IsDeleted ?? 0) != 1
                select new { b.BlobContents, b.FileType, b.OriginalFileType, b.FileName })
            .FirstOrDefaultAsync();
        if (f?.BlobContents == null) return null;
        return (f.BlobContents, f.FileType ?? f.OriginalFileType ?? "application/octet-stream", f.FileName);
    }

    /// <summary>Every document type, and this vessel's documents (or all vessels' -1) with their file details.</summary>
    private async Task<SyncOtherDocumentsDto> LoadOtherDocumentsAsync(int vesselId)
    {
        var types = await _other.OtherDocument_Type.AsNoTracking()
            .OrderBy(t => t.OtherDocument_TypeID)
            .Select(t => new SyncOtherDocumentTypeRow
            {
                OtherDocument_TypeID = t.OtherDocument_TypeID,
                Origin = t.Origin,
                VesselID = t.VesselID,
                DocumentCode = t.DocumentCode,
                DocumentType = t.DocumentType,
                IsActive = t.IsActive,
                SortOrder = t.SortOrder,
            })
            .ToListAsync();
        var documents = await _other.OtherDocumentMaster.AsNoTracking()
            .Where(m => (m.VesselID == vesselId || m.VesselID == -1) && (m.IsDeleted ?? 0) != 1)
            .OrderBy(m => m.OtherDocumentMasterID)
            .Select(m => new SyncOtherDocumentMasterRow
            {
                OtherDocumentMasterID = m.OtherDocumentMasterID,
                Origin = m.Origin,
                VesselID = m.VesselID,
                OtherDocument_TypeID = m.OtherDocument_TypeID,
                DocumentNumber = m.DocumentNumber,
                DocumentTitle = m.DocumentTitle,
                Status = m.Status,
                Reference = m.Reference,
                Comments = m.Comments,
                UploadedOn = m.UploadedOn,
                UploadedBy = m.UploadedBy,
                IsDeleted = m.IsDeleted,
                ModifiedBy = m.ModifiedBy,
                ModifiedDate = m.ModifiedDate,
            })
            .ToListAsync();
        var ids = documents.Select(d => d.OtherDocumentMasterID).ToList();
        var files = ids.Count == 0 ? new List<SyncOtherDocumentBlobRow>() : await _other.OtherDocument_Blob.AsNoTracking()
            .Where(b => ids.Contains(b.OtherDocumentMasterID))
            .OrderBy(b => b.OtherDocument_BlobID)
            .Select(b => new SyncOtherDocumentBlobRow
            {
                OtherDocument_BlobID = b.OtherDocument_BlobID,
                OtherDocumentMasterID = b.OtherDocumentMasterID,
                Origin = b.Origin,
                VesselID = b.VesselID,
                Description = b.Description,
                FileName = b.FileName,
                FileType = b.FileType,
                OriginalFileType = b.OriginalFileType,
                BlobSize = b.BlobSize,
                AddedOn = b.AddedOn,
                Cloud_ContainerName = b.Cloud_ContainerName,
                Cloud_Directory = b.Cloud_Directory,
                HQ_Storage_Option = b.HQ_Storage_Option,
                HQ_DatabaseName = b.HQ_DatabaseName,
                HQ_Local_Folder = b.HQ_Local_Folder,
                VSL_Storage_Option = b.VSL_Storage_Option,
                VSL_DatabaseName = b.VSL_DatabaseName,
                VSL_Local_Folder = b.VSL_Local_Folder,
            })
            .ToListAsync();
        return new SyncOtherDocumentsDto { Types = types, Documents = documents, Files = files };
    }

    // =========================================================================================
    // Sync monitor
    // =========================================================================================

    public async Task<Dictionary<int, HashSet<(string ItemType, long ItemId)>>> GetQueuedItemsAsync(IReadOnlyCollection<int> vesselIds)
    {
        var result = new Dictionary<int, HashSet<(string ItemType, long ItemId)>>();
        if (vesselIds.Count == 0 || !await IsInstalledAsync()) return result;

        var catalog = await LoadCatalogAsync(vesselIds);
        // One vessel (the vessel details page): read only its rows, not the whole fleet's.
        int? only = vesselIds.Distinct().Count() == 1 ? vesselIds.First() : null;
        var trackedByVessel = (await LoadTrackedRowsAsync(only))
            .GroupBy(t => t.VesselId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(t => (t.Type, t.Id)));
        var outboxByVessel = await IsOutboxInstalledAsync()
            ? (await LoadOutboxRowsAsync(only))
                .Where(r => r.Type == TypeManual || r.Type == TypeCircular)
                .ToLookup(r => r.VesselId, r => (r.Type, r.Id))
            : Enumerable.Empty<(int VesselId, string Type, long Id)>().ToLookup(r => r.VesselId, r => (r.Type, r.Id));

        foreach (var vesselId in vesselIds.Distinct())
        {
            trackedByVessel.TryGetValue(vesselId, out var rows);
            rows ??= new Dictionary<(string, long), TrackedRow>();

            var expected = catalog.ExpectedFor(vesselId);
            var queued = expected
                .Where(e => !rows.TryGetValue((e.Type, e.Id), out var t) || t.Confirmed != e.Key)
                .Select(e => (e.Type, e.Id))
                .ToHashSet();

            // Still on board but no longer meant for this vessel: a removal is waiting to go out.
            var expectedKeys = expected.Select(e => (e.Type, e.Id)).ToHashSet();
            foreach (var k in rows.Keys)
                if ((k.Item1 == TypeManual || k.Item1 == TypeCircular) && !expectedKeys.Contains(k))
                    queued.Add(k);

            // Changes recorded in the change table and not downloaded yet.
            foreach (var k in outboxByVessel[vesselId])
                queued.Add(k);

            result[vesselId] = queued;
        }
        return result;
    }

    // =========================================================================================
    // Catalog: what each vessel should have, with version keys
    // =========================================================================================

    private sealed record ExpectedItem(string Type, long Id, string Key, int Order, long TextLength);

    private sealed record ManualMeta(long Id, long ParentId, int Level, string BaseKey, bool HasVersion, long? VersionId, long TextLength);

    private sealed record CircularMeta(long Id, string BaseKey, int Order);

    private sealed class Catalog
    {
        public Dictionary<long, ManualMeta> Manuals { get; } = new();
        public Dictionary<long, CircularMeta> Circulars { get; } = new();
        /// <summary>(vesselId or -1) -> manual -> ranks</summary>
        public Dictionary<int, Dictionary<long, SortedSet<string>>> ManualAssign { get; } = new();
        public Dictionary<int, Dictionary<long, SortedSet<string>>> CircularAssign { get; } = new();

        private static IEnumerable<string> Ranks(Dictionary<int, Dictionary<long, SortedSet<string>>> map, int vesselId, long id)
        {
            var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var v in new[] { vesselId, -1 })
                if (map.TryGetValue(v, out var byItem) && byItem.TryGetValue(id, out var ranks))
                    set.UnionWith(ranks);
            return set;
        }

        public IEnumerable<string> ManualRanks(int vesselId, long id) => Ranks(ManualAssign, vesselId, id);
        public IEnumerable<string> CircularRanks(int vesselId, long id) => Ranks(CircularAssign, vesselId, id);

        private static IEnumerable<long> Assigned(Dictionary<int, Dictionary<long, SortedSet<string>>> map, int vesselId) =>
            new[] { vesselId, -1 }.SelectMany(v => map.TryGetValue(v, out var byItem) ? byItem.Keys : Enumerable.Empty<long>()).Distinct();

        public List<ExpectedItem> ExpectedFor(int vesselId)
        {
            var items = new List<ExpectedItem>();

            // Manuals with a released version, and every manual above them so the tree is complete on board.
            var manualIds = new HashSet<long>();
            foreach (var id in Assigned(ManualAssign, vesselId))
            {
                if (!Manuals.TryGetValue(id, out var m) || !m.HasVersion) continue;
                var current = m;
                while (current != null && manualIds.Add(current.Id))
                    current = current.ParentId != 0 && Manuals.TryGetValue(current.ParentId, out var parent) ? parent : null;
            }
            foreach (var id in manualIds)
            {
                var m = Manuals[id];
                var key = Hash($"{m.BaseKey}|r:{string.Join(",", ManualRanks(vesselId, id))}");
                items.Add(new ExpectedItem(TypeManual, id, key, 1_000 + m.Level, m.TextLength));
            }

            foreach (var id in Assigned(CircularAssign, vesselId))
            {
                if (!Circulars.TryGetValue(id, out var c)) continue;
                var key = Hash($"{c.BaseKey}|r:{string.Join(",", CircularRanks(vesselId, id))}");
                items.Add(new ExpectedItem(TypeCircular, id, key, c.Order, 0));
            }
            return items;
        }
    }

    /// <summary>Loads the metadata needed to work out what the given vessels should have (no manual text or files).</summary>
    private async Task<Catalog> LoadCatalogAsync(IReadOnlyCollection<int> vesselIds)
    {
        var catalog = new Catalog();
        var ids = vesselIds.Distinct().ToList();

        // ---- Manuals ----
        var manualAssign = await _dms.DmManualToAckVsls.AsNoTracking()
            .Where(a => (ids.Contains(a.VesselID) || a.VesselID == -1) && (a.IsActive ?? 1) == 1)
            .Select(a => new { a.VesselID, a.DM_ManualID, a.UserRank })
            .ToListAsync();
        foreach (var a in manualAssign)
            AddRank(catalog.ManualAssign, a.VesselID, a.DM_ManualID, a.UserRank);

        // One-time full copy (Database/008_Sync_FullCopy.sql): items given to a vessel whether or not they are
        // assigned to it. They count as assigned with no ranks to acknowledge.
        var fullCopy = await LoadFullCopyAsync(ids);
        foreach (var f in fullCopy.Where(f => f.Type == TypeManual))
            AddRank(catalog.ManualAssign, f.VesselId, f.Id, null);

        // Only the assigned manuals and the manuals above them are needed. Work that set out from a
        // narrow (id, parent) list first, so the heavy reads below (version text length, attachments)
        // touch those manuals only instead of the whole library.
        var parentOf = (await _dms.DmManuals.AsNoTracking()
                .Where(m => (m.IsDeleted ?? 0) != 1)
                .Select(m => new { m.DM_ManualID, m.ParentId })
                .ToListAsync())
            .ToDictionary(m => m.DM_ManualID, m => m.ParentId);
        var neededManuals = new HashSet<long>();
        foreach (var start in manualAssign.Select(a => a.DM_ManualID)
                     .Concat(fullCopy.Where(f => f.Type == TypeManual).Select(f => f.Id)).Distinct())
        {
            var id = start;
            while (parentOf.TryGetValue(id, out var parentId) && neededManuals.Add(id) && parentId != 0)
                id = parentId;
        }
        var neededManualIds = neededManuals.ToList();

        var manuals = await _dms.DmManuals.AsNoTracking()
            .Where(m => neededManualIds.Contains(m.DM_ManualID))
            .Select(m => new
            {
                m.DM_ManualID, m.ParentId, m.ManualLevel, m.DM_CategoryID, m.ManualNo, m.ManualCode, m.ManualName,
                m.IsMainTitle, m.IsAcknowledgementRequired, m.ModifiedDate,
            })
            .ToListAsync();
        var versions = (await _dms.DmManualVersions.AsNoTracking()
                .Where(v => v.IsActive == 1 && neededManualIds.Contains(v.DM_ManualID))
                .Select(v => new
                {
                    v.DM_ManualID, v.DM_ManualVersionID, v.Version, v.PublishedDate, v.ReleasedDate, v.LastModifiedDate,
                    TextLength = (long?)v.TextContents!.Length,
                })
                .ToListAsync())
            .GroupBy(v => v.DM_ManualID)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.DM_ManualVersionID).First());
        var attachments = (await _dms.DmManualAttachments.AsNoTracking()
                .Where(a => (a.IsDeleted ?? 0) != 1 && neededManualIds.Contains(a.DM_ManualID))
                .Select(a => new { a.DmManualAttachmentsId, a.DM_ManualID, a.BlobSize, a.FileType })
                .ToListAsync())
            .ToLookup(a => a.DM_ManualID);

        foreach (var m in manuals)
        {
            versions.TryGetValue(m.DM_ManualID, out var v);
            var files = string.Join(",", attachments[m.DM_ManualID].OrderBy(a => a.DmManualAttachmentsId)
                .Select(a => $"{a.DmManualAttachmentsId}:{a.BlobSize}:{a.FileType}"));
            var baseKey = string.Join("|", m.ParentId, m.ManualLevel, m.DM_CategoryID, m.ManualNo, m.ManualCode, m.ManualName,
                m.IsMainTitle, m.IsAcknowledgementRequired, Ticks(m.ModifiedDate),
                v?.DM_ManualVersionID, v?.Version, Ticks(v?.PublishedDate), Ticks(v?.ReleasedDate), Ticks(v?.LastModifiedDate),
                v?.TextLength, files);
            catalog.Manuals[m.DM_ManualID] = new ManualMeta(m.DM_ManualID, m.ParentId, m.ManualLevel, baseKey,
                v != null, v?.DM_ManualVersionID, v?.TextLength ?? 0);
        }

        // ---- Circulars ----
        var circularAssign = await _cir.CIR_ToAck_Vsl.AsNoTracking()
            .Where(a => (ids.Contains(a.VesselID) || a.VesselID == -1) && (a.IsActive ?? 1) == 1)
            .Select(a => new { a.VesselID, a.CIR_MasterID, a.UserRank })
            .ToListAsync();
        foreach (var a in circularAssign)
            AddRank(catalog.CircularAssign, a.VesselID, a.CIR_MasterID, a.UserRank);
        foreach (var f in fullCopy.Where(f => f.Type == TypeCircular))
            AddRank(catalog.CircularAssign, f.VesselId, f.Id, null);

        var circularIds = circularAssign.Select(a => a.CIR_MasterID)
            .Concat(fullCopy.Where(f => f.Type == TypeCircular).Select(f => f.Id))
            .Distinct().ToList();
        if (circularIds.Count > 0)
        {
            var masters = await _cir.CIR_Master.AsNoTracking()
                .Where(c => circularIds.Contains(c.CIR_MasterID) && (c.IsDeleted ?? 0) != 1
                         && c.Status != "Temp" && c.Status != "Draft")
                .Select(c => new
                {
                    c.CIR_MasterID, c.CIR_CategoryID, c.CircularType, c.CIR_Number, c.Reference, c.Title, c.Status,
                    c.Priority, c.DateIssued, c.Remarks, c.ReleasedDate, c.ModifiedDate,
                })
                .ToListAsync();
            var blobs = (await _cir.CIR_Blob.AsNoTracking()
                    .Where(b => b.CIR_MasterID != null && circularIds.Contains(b.CIR_MasterID.Value))
                    .Select(b => new { b.CIR_BlobID, CIR_MasterID = b.CIR_MasterID!.Value, b.BlobSize, b.FileName, b.FileType })
                    .ToListAsync())
                .ToLookup(b => b.CIR_MasterID);

            foreach (var c in masters)
            {
                var files = string.Join(",", blobs[c.CIR_MasterID].OrderBy(b => b.CIR_BlobID)
                    .Select(b => $"{b.CIR_BlobID}:{b.BlobSize}:{b.FileName}:{b.FileType}"));
                var baseKey = string.Join("|", c.CIR_CategoryID, c.CircularType, c.CIR_Number, c.Reference, c.Title, c.Status,
                    c.Priority, Ticks(c.DateIssued), c.Remarks, Ticks(c.ReleasedDate), Ticks(c.ModifiedDate), files);
                // Alerts first, then by priority (1 High, 2 Medium, 3 Low).
                var isAlert = (c.CircularType ?? "").Trim().StartsWith("Alert", StringComparison.OrdinalIgnoreCase);
                var order = (isAlert ? 0 : 10) + Math.Clamp(c.Priority ?? 3, 1, 9);
                catalog.Circulars[c.CIR_MasterID] = new CircularMeta(c.CIR_MasterID, baseKey, order);
            }
        }

        return catalog;
    }

    private bool? _fullCopyInstalled;

    /// <summary>Rows of dbo.Sync_FullCopyItem for these vessels (empty when 008_Sync_FullCopy.sql has not been run).</summary>
    private async Task<List<(int VesselId, string Type, long Id)>> LoadFullCopyAsync(List<int> vesselIds)
    {
        var rows = new List<(int, string, long)>();
        if (vesselIds.Count == 0) return rows;
        _fullCopyInstalled ??= Convert.ToInt32(await ScalarAsync(
            "SELECT CASE WHEN OBJECT_ID('dbo.Sync_FullCopyItem', 'U') IS NULL THEN 0 ELSE 1 END")) == 1;
        if (!_fullCopyInstalled.Value) return rows;

        await WithConnectionAsync(async connection =>
        {
            using var cmd = connection.CreateCommand();
            // Vessel IDs are ints from our own code, so they can be listed inline.
            cmd.CommandText = "SELECT VesselID, ItemType, ItemID FROM dbo.Sync_FullCopyItem WHERE VesselID IN (" +
                              string.Join(",", vesselIds.Distinct()) + ")";
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                rows.Add((r.GetInt32(0), r.GetString(1).Trim(), r.GetInt64(2)));
        });
        return rows;
    }

    private static void AddRank(Dictionary<int, Dictionary<long, SortedSet<string>>> map, int vesselId, long itemId, string? rank)
    {
        if (!map.TryGetValue(vesselId, out var byItem)) map[vesselId] = byItem = new();
        if (!byItem.TryGetValue(itemId, out var ranks)) byItem[itemId] = ranks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(rank)) ranks.Add(rank!.Trim());
    }

    // =========================================================================================
    // Full details for the items being sent
    // =========================================================================================

    /// <summary>The manuals being sent, as office rows: DM_Manual, its released DM_ManualVersion, DM_Manual_Attachments
    /// (without the file contents), DM_Manual_ToAck_Vsl for this vessel or -1, and this vessel's DM_Manual_UserAck.</summary>
    private async Task<List<SyncManualDto>> LoadManualsAsync(Catalog catalog, List<long> manualIds, int vesselId)
    {
        if (manualIds.Count == 0) return new();

        var manuals = await _dms.DmManuals.AsNoTracking()
            .Where(m => manualIds.Contains(m.DM_ManualID) && (m.IsDeleted ?? 0) != 1)
            .Select(m => new SyncDMManualRow
            {
                DM_ManualID = m.DM_ManualID,
                Origin = m.Origin,
                VesselID = m.VesselID,
                DM_CategoryID = m.DM_CategoryID,
                ManualNo = m.ManualNo,
                ManualCode = m.ManualCode,
                ManualName = m.ManualName,
                DM_StatusID = m.DmStatusId,
                ParentId = m.ParentId,
                ManualLevel = m.ManualLevel,
                IsMainTitle = m.IsMainTitle,
                IsAcknowledgementRequired = m.IsAcknowledgementRequired,
                CanExport = m.CanExport,
                IsApprovalRequired = m.IsApprovalRequired,
                Approval_FlowID = m.Approval_FlowID,
                NextApproval_HQRole = m.NextApproval_HQRole,
                NextApproval_HQDept = m.NextApproval_HQDept,
                SortOrder = m.SortOrder,
                AddlSortOrder = m.AddlSortOrder,
                CreatedBy = m.CreatedBy,
                CreatedDate = m.CreatedDate,
                ModifiedBy = m.ModifiedBy,
                ModifiedDate = m.ModifiedDate,
                IsDeleted = m.IsDeleted,
            })
            .ToListAsync();
        var versionIds = manualIds
            .Select(id => catalog.Manuals.TryGetValue(id, out var meta) ? meta.VersionId : null)
            .Where(v => v != null).Select(v => v!.Value).ToList();
        var versions = (await _dms.DmManualVersions.AsNoTracking()
                .Where(v => versionIds.Contains(v.DM_ManualVersionID))
                .Select(v => new SyncDMManualVersionRow
            {
                DM_ManualVersionID = v.DM_ManualVersionID,
                Origin = v.Origin,
                VesselID = v.VesselID,
                DM_ManualID = v.DM_ManualID,
                Version = v.Version,
                TextContents = v.TextContents,
                PublishedBy = v.PublishedBy,
                PublishedDate = v.PublishedDate,
                CreatedBy = v.CreatedBy,
                CreatedDate = v.CreatedDate,
                ReleasedBy = v.ReleasedBy,
                ReleasedDate = v.ReleasedDate,
                IsActive = v.IsActive,
                IsDisabled = v.IsDisabled,
                LastModifiedBy = v.LastModifiedBy,
                LastModifiedDate = v.LastModifiedDate,
                Comments = v.Comments,
            })
                .ToListAsync())
            .ToDictionary(v => v.DM_ManualID);
        var attachments = (await _dms.DmManualAttachments.AsNoTracking()
                .Where(a => manualIds.Contains(a.DM_ManualID) && (a.IsDeleted ?? 0) != 1)
                .Select(a => new SyncDMManualAttachmentsRow
            {
                DM_Manual_AttachmentsID = a.DmManualAttachmentsId,
                Origin = a.Origin,
                VesselID = a.VesselID,
                DM_ManualID = a.DM_ManualID,
                ImageRefNo = a.ImageRefNo,
                BlobSize = a.BlobSize,
                FileType = a.FileType,
                AddedOn = a.AddedOn,
                IsDeleted = a.IsDeleted,
            })
                .ToListAsync())
            .ToLookup(a => a.DM_ManualID);
        var toAck = (await _dms.DmManualToAckVsls.AsNoTracking()
                .Where(a => manualIds.Contains(a.DM_ManualID) && (a.VesselID == vesselId || a.VesselID == -1))
                .Select(a => new SyncDMManualToAckVslRow
            {
                DM_Manual_ToAck_VslID = a.DmManualToAckVslId,
                Origin = a.Origin,
                VesselID = a.VesselID,
                DM_ManualID = a.DM_ManualID,
                UserRank = a.UserRank,
                IsActive = a.IsActive,
            })
                .ToListAsync())
            .ToLookup(a => a.DM_ManualID);
        var userAcks = (await _dms.DmManualUserAcks.AsNoTracking()
                .Where(a => a.VesselID == vesselId && versionIds.Contains(a.DM_ManualVersionID))
                .Select(a => new SyncDMManualUserAckRow
            {
                DM_Manual_UserAckID = a.DmManualUserAckId,
                Origin = a.Origin,
                VesselID = a.VesselID,
                DM_ManualID = a.DM_ManualID,
                DM_ManualVersionID = a.DM_ManualVersionID,
                Version = a.Version,
                UserRank = a.UserRank,
                UserName = a.UserName,
                UserID = a.UserId,
                DateRead = a.DateRead,
                Comments = a.Comments,
                IsActive = a.IsActive,
            })
                .ToListAsync())
            .ToLookup(a => a.DM_ManualID);

        return manuals.Select(m => new SyncManualDto
        {
            DM_ManualID = m.DM_ManualID,
            Manual = m,
            Version = versions.TryGetValue(m.DM_ManualID, out var v) ? v : null,
            Attachments = attachments[m.DM_ManualID].ToList(),
            ToAck = toAck[m.DM_ManualID].ToList(),
            UserAcks = userAcks[m.DM_ManualID].ToList(),
        }).ToList();
    }

    /// <summary>The circulars being sent, as office rows: CIR_Master, CIR_Blob (without the file contents),
    /// CIR_ToAck_Vsl for this vessel or -1, and this vessel's CIR_UserAck.</summary>
    private async Task<List<SyncCircularDto>> LoadCircularsAsync(List<long> circularIds, int vesselId)
    {
        if (circularIds.Count == 0) return new();

        var masters = await _cir.CIR_Master.AsNoTracking()
            .Where(c => circularIds.Contains(c.CIR_MasterID) && (c.IsDeleted ?? 0) != 1)
            .Select(c => new SyncCIRMasterRow
            {
                CIR_MasterID = c.CIR_MasterID,
                Origin = c.Origin,
                VesselID = c.VesselID,
                CircularType = c.CircularType,
                CIR_CategoryID = c.CIR_CategoryID,
                CIR_Number = c.CIR_Number,
                Reference = c.Reference,
                Title = c.Title,
                Status = c.Status,
                Priority = c.Priority,
                DateIssued = c.DateIssued,
                Remarks = c.Remarks,
                ReleasedDate = c.ReleasedDate,
                CreatedBy = c.CreatedBy,
                CreatedDate = c.CreatedDate,
                ModifiedBy = c.ModifiedBy,
                ModifiedDate = c.ModifiedDate,
                IsDeleted = c.IsDeleted,
            })
            .ToListAsync();
        var blobs = (await _cir.CIR_Blob.AsNoTracking()
                .Where(b => b.CIR_MasterID != null && circularIds.Contains(b.CIR_MasterID.Value))
                .Select(b => new SyncCIRBlobRow
            {
                CIR_BlobID = b.CIR_BlobID,
                Origin = b.Origin,
                VesselID = b.VesselID,
                CIR_MasterID = b.CIR_MasterID,
                Description = b.Description,
                FileName = b.FileName,
                FileType = b.FileType,
                OriginalFileType = b.OriginalFileType,
                BlobSize = b.BlobSize,
                AddedOn = b.AddedOn,
                Cloud_ContainerName = b.Cloud_ContainerName,
                Cloud_Directory = b.Cloud_Directory,
                HQ_Storage_Option = b.HQ_Storage_Option,
                HQ_DatabaseName = b.HQ_DatabaseName,
                HQ_Local_Folder = b.HQ_Local_Folder,
                VSL_Storage_Option = b.VSL_Storage_Option,
                VSL_DatabaseName = b.VSL_DatabaseName,
                VSL_Local_Folder = b.VSL_Local_Folder,
            })
                .ToListAsync())
            .ToLookup(b => b.CIR_MasterID ?? 0);
        var toAck = (await _cir.CIR_ToAck_Vsl.AsNoTracking()
                .Where(a => circularIds.Contains(a.CIR_MasterID) && (a.VesselID == vesselId || a.VesselID == -1))
                .Select(a => new SyncCIRToAckVslRow
            {
                CIR_ToAck_VslID = a.CIR_ToAck_VslID,
                Origin = a.Origin,
                VesselID = a.VesselID,
                CIR_MasterID = a.CIR_MasterID,
                UserRank = a.UserRank,
                IsActive = a.IsActive,
            })
                .ToListAsync())
            .ToLookup(a => a.CIR_MasterID);
        var userAcks = (await _cir.CIR_UserAck.AsNoTracking()
                .Where(a => a.VesselID == vesselId && a.CIR_MasterID != null && circularIds.Contains(a.CIR_MasterID.Value))
                .Select(a => new SyncCIRUserAckRow
            {
                CIR_UserAckID = a.CIR_UserAckID,
                Origin = a.Origin,
                VesselID = a.VesselID,
                CIR_MasterID = a.CIR_MasterID,
                Fleet_CrewID = a.Fleet_CrewID,
                DateRead = a.DateRead,
                Comments = a.Comments,
                IsActive = a.IsActive,
            })
                .ToListAsync())
            .ToLookup(a => a.CIR_MasterID ?? 0);

        return masters.Select(c => new SyncCircularDto
        {
            CIR_MasterID = c.CIR_MasterID,
            Master = c,
            Blobs = blobs[c.CIR_MasterID].ToList(),
            ToAck = toAck[c.CIR_MasterID].ToList(),
            UserAcks = userAcks[c.CIR_MasterID].ToList(),
        }).ToList();
    }

    private async Task<List<SyncCrewDto>> LoadCrewAsync(int vesselId)
    {
        var today = DateTime.UtcNow.Date;
        var staff = await _general.VslStaffs.AsNoTracking()
            .Where(s => s.VesselID == vesselId)
            .Select(s => new { s.VslStaffId, s.UserId, s.UserName, s.UserRank, s.UserDept, s.IsDisabled, s.LeavingDate, s.Password })
            .ToListAsync();

        return staff
            .Where(s => !string.IsNullOrWhiteSpace(s.UserId))
            .GroupBy(s => s.UserId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(s => new SyncCrewDto
            {
                VslStaffId = s.VslStaffId,
                UserId = s.UserId.Trim(),
                UserName = s.UserName,
                UserRank = s.UserRank,
                UserDept = s.UserDept,
                // Signed off: cannot log in on board any more.
                IsDisabled = s.IsDisabled == 1 || (s.LeavingDate != null && s.LeavingDate.Value.Date < today) ? 1 : 0,
                Password = s.Password,
            })
            .ToList();
    }

    private sealed record FixedTables(List<SyncDmStatusDto> DmStatuses, List<SyncDmSettingDto> DmSettings, List<SyncCirSettingDto> CirSettings, List<SyncVesselDto> Vessels);

    /// <summary>The fixed tables, every row (no filter), ordered by key so the version key is stable.</summary>
    private async Task<FixedTables> LoadFixedTablesAsync()
    {
        var dmStatuses = await ReadRowsAsync(
            "SELECT DM_StatusID, Origin, VesselID, StatusCode, StatusName, IsDisabled, IsVisible, SortOrder FROM dbo.DM_Status ORDER BY DM_StatusID",
            r => new SyncDmStatusDto
            {
                DM_StatusID = Convert.ToInt64(r.GetValue(0)),
                Origin = r.IsDBNull(1) ? string.Empty : Convert.ToString(r.GetValue(1))!.TrimEnd(),
                VesselID = Convert.ToInt32(r.GetValue(2)),
                StatusCode = r.IsDBNull(3) ? null : Convert.ToString(r.GetValue(3))!.TrimEnd(),
                StatusName = r.IsDBNull(4) ? string.Empty : Convert.ToString(r.GetValue(4))!.TrimEnd(),
                IsDisabled = r.IsDBNull(5) ? null : (int?)Convert.ToInt32(r.GetValue(5)),
                IsVisible = r.IsDBNull(6) ? null : (int?)Convert.ToInt32(r.GetValue(6)),
                SortOrder = r.IsDBNull(7) ? null : (int?)Convert.ToInt32(r.GetValue(7)),
            });
        var dmSettings = await ReadRowsAsync(
            "SELECT DM_SettingsID, Origin, VesselID, SettingName, SettingValue FROM dbo.DM_Settings ORDER BY DM_SettingsID",
            r => new SyncDmSettingDto
            {
                DM_SettingsID = Convert.ToInt64(r.GetValue(0)),
                Origin = r.IsDBNull(1) ? string.Empty : Convert.ToString(r.GetValue(1))!.TrimEnd(),
                VesselID = Convert.ToInt32(r.GetValue(2)),
                SettingName = r.IsDBNull(3) ? string.Empty : Convert.ToString(r.GetValue(3))!.TrimEnd(),
                SettingValue = r.IsDBNull(4) ? null : Convert.ToString(r.GetValue(4))!.TrimEnd(),
            });
        var cirSettings = await ReadRowsAsync(
            "SELECT CIR_SettingsID, Origin, VesselID, SettingName, SettingValue FROM dbo.CIR_Setting ORDER BY CIR_SettingsID",
            r => new SyncCirSettingDto
            {
                CIR_SettingsID = Convert.ToInt64(r.GetValue(0)),
                Origin = r.IsDBNull(1) ? string.Empty : Convert.ToString(r.GetValue(1))!.TrimEnd(),
                VesselID = Convert.ToInt32(r.GetValue(2)),
                SettingName = r.IsDBNull(3) ? string.Empty : Convert.ToString(r.GetValue(3))!.TrimEnd(),
                SettingValue = r.IsDBNull(4) ? null : Convert.ToString(r.GetValue(4))!.TrimEnd(),
            });
        var vessels = await ReadRowsAsync(
            "SELECT V_VesselID, Origin, VesselID, VslCode, VslName, VslType, DelivDate, BaseCurrency, VslEmailAddress, OperateBy, IsRetired FROM dbo.V_Vessel ORDER BY V_VesselID",
            r => new SyncVesselDto
            {
                V_VesselID = Convert.ToInt64(r.GetValue(0)),
                Origin = r.IsDBNull(1) ? string.Empty : Convert.ToString(r.GetValue(1))!.TrimEnd(),
                VesselID = Convert.ToInt32(r.GetValue(2)),
                VslCode = r.IsDBNull(3) ? string.Empty : Convert.ToString(r.GetValue(3))!.TrimEnd(),
                VslName = r.IsDBNull(4) ? string.Empty : Convert.ToString(r.GetValue(4))!.TrimEnd(),
                VslType = r.IsDBNull(5) ? null : Convert.ToString(r.GetValue(5))!.TrimEnd(),
                DelivDate = r.IsDBNull(6) ? null : (DateTime?)r.GetDateTime(6),
                BaseCurrency = r.IsDBNull(7) ? null : Convert.ToString(r.GetValue(7))!.TrimEnd(),
                VslEmailAddress = r.IsDBNull(8) ? null : Convert.ToString(r.GetValue(8))!.TrimEnd(),
                OperateBy = r.IsDBNull(9) ? null : Convert.ToString(r.GetValue(9))!.TrimEnd(),
                IsRetired = r.IsDBNull(10) ? null : (int?)Convert.ToInt32(r.GetValue(10)),
            });
        return new FixedTables(dmStatuses, dmSettings, cirSettings, vessels);
    }

    /// <summary>Runs a query and maps each row; a table that does not exist gives an empty list.</summary>
    private async Task<List<T>> ReadRowsAsync<T>(string sql, Func<DbDataReader, T> map)
    {
        var rows = new List<T>();
        await WithConnectionAsync(async connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            try
            {
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) rows.Add(map(r));
            }
            catch (DbException ex) when (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
            {
                // Table not in this database: nothing to send.
            }
        });
        return rows;
    }

    private async Task<(List<SyncManualCategoryDto>, List<SyncCircularCategoryDto>)> LoadCategoriesAsync()
    {
        // Every row, disabled ones too: a manual or circular on board may still be filed under one.
        var manual = await _dms.DmCategories.AsNoTracking()
            .Select(c => new SyncManualCategoryDto
            {
                DM_CategoryID = c.DM_CategoryID, Origin = c.Origin, VesselID = c.VesselID,
                CategoryCode = c.CategoryCode, CategoryName = c.CategoryName, IsDisabled = c.IsDisabled,
            })
            .ToListAsync();
        var circular = await _cir.CIR_Category.AsNoTracking()
            .Select(c => new SyncCircularCategoryDto
            {
                CIR_CategoryID = c.CIR_CategoryID, Origin = c.Origin, VesselID = c.VesselID, CircularType = c.CircularType,
                CategoryCode = c.CategoryCode, Category = c.Category, isActive = c.isActive, Sortorder = c.Sortorder,
                CategoryName = c.Category,
            })
            .ToListAsync();
        return (manual, circular);
    }

    // =========================================================================================
    // Sync_VesselItem, Sync_Vessel, Sync_Log
    // =========================================================================================

    private sealed record TrackedRow(int VesselId, string Type, long Id, string? Pending, string? Confirmed);

    /// <summary>Tracked items of one vessel (or all vessels when null).</summary>
    private async Task<List<TrackedRow>> LoadTrackedRowsAsync(int? vesselId)
    {
        var rows = new List<TrackedRow>();
        await WithConnectionAsync(async connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT VesselID, ItemType, ItemID, PendingKey, ConfirmedKey FROM dbo.Sync_VesselItem" +
                              (vesselId == null ? "" : " WHERE VesselID = @v");
            if (vesselId != null) AddParameter(cmd, "@v", vesselId.Value);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                rows.Add(new TrackedRow(
                    r.GetInt32(0), r.GetString(1).Trim(), r.GetInt64(2),
                    r.IsDBNull(3) ? null : r.GetString(3).Trim(),
                    r.IsDBNull(4) ? null : r.GetString(4).Trim()));
            }
        });
        return rows;
    }

    private async Task<Dictionary<(string Type, long Id), TrackedRow>> LoadTrackedAsync(int vesselId) =>
        (await LoadTrackedRowsAsync(vesselId)).ToDictionary(t => (t.Type, t.Id));

    private async Task<bool> IsTrackedAsync(int vesselId, string type, long id)
    {
        var value = await ScalarAsync(
            "SELECT COUNT(*) FROM dbo.Sync_VesselItem WHERE VesselID = @v AND ItemType = @t AND ItemID = @id AND ISNULL(PendingKey, '') <> @del",
            ("@v", vesselId), ("@t", type), ("@id", id), ("@del", DeletedKey));
        return Convert.ToInt32(value) > 0;
    }

    private async Task MarkPendingAsync(int vesselId, List<(string Type, long Id, string Key)> rows, DateTime serverTime)
    {
        // 500 rows x 3 parameters per statement keeps well under SQL Server's 2100-parameter limit.
        foreach (var chunk in rows.Chunk(500))
        {
            var values = new StringBuilder();
            var parameters = new List<(string, object?)> { ("@v", vesselId), ("@now", serverTime) };
            for (var i = 0; i < chunk.Length; i++)
            {
                if (i > 0) values.Append(',');
                values.Append($"(@t{i}, @i{i}, @k{i})");
                parameters.Add(($"@t{i}", chunk[i].Type));
                parameters.Add(($"@i{i}", chunk[i].Id));
                parameters.Add(($"@k{i}", chunk[i].Key));
            }

            await ExecuteAsync($@"
MERGE dbo.Sync_VesselItem WITH (HOLDLOCK) AS t
USING (VALUES {values}) AS s (ItemType, ItemID, ItemKey)
   ON t.VesselID = @v AND t.ItemType = s.ItemType AND t.ItemID = s.ItemID
WHEN MATCHED THEN UPDATE SET PendingKey = s.ItemKey, PendingUtc = @now
WHEN NOT MATCHED THEN INSERT (VesselID, ItemType, ItemID, PendingKey, PendingUtc) VALUES (@v, s.ItemType, s.ItemID, s.ItemKey, @now);",
                parameters.ToArray());
        }
    }

    private async Task RecordCallAsync(int vesselId, string direction, DateTime startedUtc, int items, string? error)
    {
        var ok = error == null;
        await ExecuteAsync(@"
MERGE dbo.Sync_Vessel WITH (HOLDLOCK) AS t
USING (SELECT @v AS VesselID) AS s ON t.VesselID = s.VesselID
WHEN NOT MATCHED THEN INSERT (VesselID, PendingOnShip) VALUES (@v, 0);
UPDATE dbo.Sync_Vessel
   SET LastContactUtc = SYSUTCDATETIME(),
       LastSuccessUtc = CASE WHEN @ok = 1 THEN SYSUTCDATETIME() ELSE LastSuccessUtc END,
       LastPullUtc = CASE WHEN @dir = 'Down' THEN SYSUTCDATETIME() ELSE LastPullUtc END,
       LastPushUtc = CASE WHEN @dir = 'Up' THEN SYSUTCDATETIME() ELSE LastPushUtc END,
       LastStatus = CASE WHEN @ok = 1 THEN 'OK' ELSE 'Failed' END,
       LastError = @error
 WHERE VesselID = @v;
INSERT INTO dbo.Sync_Log (VesselID, Direction, StartedUtc, FinishedUtc, ItemsCount, Status, Error)
VALUES (@v, @dir, @started, SYSUTCDATETIME(), @items, CASE WHEN @ok = 1 THEN 'OK' ELSE 'Failed' END, @error);",
            ("@v", vesselId), ("@dir", direction), ("@ok", ok ? 1 : 0), ("@started", startedUtc),
            ("@items", items), ("@error", Truncate(error, 1000)));
    }

    private async Task TryRecordCallAsync(int vesselId, string direction, DateTime startedUtc, string error)
    {
        try { await RecordCallAsync(vesselId, direction, startedUtc, 0, error); }
        catch { /* the original error is what matters */ }
    }

    // =========================================================================================
    // Helpers
    // =========================================================================================

    private async Task WithConnectionAsync(Func<DbConnection, Task> work)
    {
        var connection = _general.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try { await work(connection); }
        finally { if (openedHere) await connection.CloseAsync(); }
    }

    private async Task<object?> ScalarAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        object? result = null;
        await WithConnectionAsync(async connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters) AddParameter(cmd, name, value);
            result = await cmd.ExecuteScalarAsync();
        });
        return result is DBNull ? null : result;
    }

    private async Task ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await WithConnectionAsync(async connection =>
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters) AddParameter(cmd, name, value);
            await cmd.ExecuteNonQueryAsync();
        });
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        if (value is DateTime) p.DbType = DbType.DateTime2;
        cmd.Parameters.Add(p);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string HashKey(string key) => Hash(key.Trim());

    private static long? Ticks(DateTime? value) => value?.Ticks;

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static int? LeadingNumber(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;
        var digits = new string(version.Trim().TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : null;
    }

    private static string? Truncate(string? value, int max) =>
        value == null ? null : value.Length <= max ? value : value[..max];
}
