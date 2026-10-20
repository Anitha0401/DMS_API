using System.Globalization;
using System.Text.Json;
using DMS.Application.DTO.Sync;
using DMS.Data.EF.Features;

namespace DMS.Application;

/// <summary>
/// Ship -> office push of the new feature tables (Database/012_NewFeatures.sql), row by row under the same key.
/// The ship sends a record as it is; the office inserts it, or updates it when it is already this vessel's record.
/// </summary>
public partial class SyncService
{
    /// <summary>The tables a vessel may push, with their key column.</summary>
    public static readonly IReadOnlyDictionary<string, string> PushableTables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["DMS_InboxRead"] = "DMS_InboxReadID",
        ["SUG_Master"] = "SUG_MasterID",
        ["SUG_Log"] = "SUG_LogID",
        ["SUG_Blob"] = "SUG_BlobID",
        ["FLD_Folder"] = "FLD_FolderID",
    };

    /// <summary>Records the office owns once stored: a repeat from the ship never overwrites the office's changes.</summary>
    private static readonly HashSet<string> InsertOnlyTables = new(StringComparer.OrdinalIgnoreCase)
        { "DMS_InboxRead", "SUG_Master", "SUG_Log", "SUG_Blob" };

    public async Task<SyncPushRowsResult> PushRowsAsync(int vesselId, SyncPushRowsRequest request)
    {
        var started = DateTime.UtcNow;
        var result = new SyncPushRowsResult();
        var db = new SqlDb(_general);
        var columnsByTable = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // Parents before children, so a suggestion is stored before its log.
        var order = PushableTables.Keys.Select((t, i) => (t, i)).ToDictionary(x => x.t, x => x.i, StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows.OrderBy(r => order.TryGetValue(r.TableName, out var i) ? i : int.MaxValue))
        {
            var outcome = new SyncPushRowResult { TableName = row.TableName, Key = row.Key };
            result.Results.Add(outcome);
            try
            {
                outcome.Error = await PushRowAsync(db, vesselId, row, columnsByTable);
                outcome.Stored = outcome.Error == null;
            }
            catch (Exception ex)
            {
                outcome.Error = ex.Message;
            }
        }

        var stored = result.Results.Count(r => r.Stored);
        var firstError = result.Results.FirstOrDefault(r => r.Error != null)?.Error;
        await RecordCallAsync(vesselId, "Up", started, stored, firstError);
        return result;
    }

    /// <summary>Pseudo table: an Other Document of this vessel moved to another folder on board (Row: FLD_FolderID).</summary>
    public const string DocumentFolderRow = "DocumentFolder";

    private async Task<string?> PushRowAsync(SqlDb db, int vesselId, SyncPushRow row, Dictionary<string, Dictionary<string, string>> columnsByTable)
    {
        if (string.Equals(row.TableName, DocumentFolderRow, StringComparison.OrdinalIgnoreCase))
        {
            long? folder = row.Row.TryGetValue("FLD_FolderID", out var f) && f.ValueKind == JsonValueKind.Number ? f.GetInt64() : null;
            var updated = await db.ExecuteAsync(
                "UPDATE dbo.OtherDocumentMaster SET FLD_FolderID = @f WHERE OtherDocumentMasterID = @k AND VesselID = @v AND ISNULL(IsForOfficeOnly, 0) = 0",
                ("@f", folder), ("@k", row.Key), ("@v", vesselId));
            return updated == 1 ? null : $"Document {row.Key} is not this vessel's document at the office.";
        }

        if (!PushableTables.TryGetValue(row.TableName, out var keyColumn)) return $"{row.TableName} cannot be sent from a vessel.";
        var table = PushableTables.Keys.First(k => string.Equals(k, row.TableName, StringComparison.OrdinalIgnoreCase));

        if (!columnsByTable.TryGetValue(table, out var columns))
        {
            columns = (await db.QueryAsync<ColumnRow>(@"
SELECT c.name AS Name, t.name AS TypeName FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
 WHERE c.object_id = OBJECT_ID(@t) AND c.is_computed = 0", ("@t", "dbo." + table)))
                .ToDictionary(c => c.Name, c => c.TypeName, StringComparer.OrdinalIgnoreCase);
            if (columns.Count == 0) return $"{table} is not installed at the office (run Database/012_NewFeatures.sql).";
            columnsByTable[table] = columns;
        }

        // The values, converted to the office column types. Origin and VesselID are always the calling vessel's.
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, element) in row.Row)
        {
            if (!columns.TryGetValue(name, out var type)) continue; // ship-only columns such as IsPendingUpload
            values[columns.Keys.First(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase))] = FromJson(element, type);
        }
        values[keyColumn] = row.Key;
        values["Origin"] = "VSL";
        values["VesselID"] = vesselId;

        if (table == "FLD_Folder" && string.Equals(values.GetValueOrDefault("Scope") as string, "Personal", StringComparison.OrdinalIgnoreCase))
            return "Personal folders stay on board.";

        var existing = await db.FirstAsync<OwnerRow>(
            $"SELECT Origin, VesselID FROM dbo.{Bracket(table)} WHERE {Bracket(keyColumn)} = @k", ("@k", row.Key));
        if (existing != null)
        {
            if (existing.Origin != "VSL" || existing.VesselID != vesselId)
                return $"{table} {row.Key} is already used at the office for another record.";
            if (InsertOnlyTables.Contains(table)) return null; // stored before
            var sets = values.Keys.Where(c => !string.Equals(c, keyColumn, StringComparison.OrdinalIgnoreCase)).ToList();
            await db.ExecuteAsync(
                $"UPDATE dbo.{Bracket(table)} SET {string.Join(", ", sets.Select((c, i) => $"{Bracket(c)} = @p{i}"))} WHERE {Bracket(keyColumn)} = @k",
                sets.Select((c, i) => ($"@p{i}", values[c])).Append(("@k", (object?)row.Key)).ToArray());
            return null;
        }

        var error = await CheckParentAsync(db, table, values, vesselId);
        if (error != null) return error;

        var names = values.Keys.ToList();
        await db.ExecuteAsync(
            $"INSERT dbo.{Bracket(table)} ({string.Join(", ", names.Select(Bracket))}) VALUES ({string.Join(", ", names.Select((_, i) => $"@p{i}"))})",
            names.Select((c, i) => ($"@p{i}", values[c])).ToArray());

        // An answer on board to "needs more information" puts the suggestion back under review.
        if (table == "SUG_Log" && string.Equals(values.GetValueOrDefault("Status") as string, "Answered", StringComparison.OrdinalIgnoreCase))
            await db.ExecuteAsync("UPDATE dbo.SUG_Master SET Status = 'UnderReview', ModifiedOn = GETUTCDATE() WHERE SUG_MasterID = @s AND Status = 'NeedsInfo'",
                ("@s", values.GetValueOrDefault("SUG_MasterID")));
        return null;
    }

    public async Task<(byte[] Bytes, string? FileName)?> GetFeatureFileAsync(int vesselId, string table, long blobId)
    {
        var db = new SqlDb(_general);
        var sql = table switch
        {
            "SUG_Blob" => "SELECT b.VesselID, b.FileName, b.BlobContents FROM dbo.SUG_Blob b WHERE b.SUG_BlobID = @id",
            _ => null,
        };
        if (sql == null) return null;
        var row = await db.FirstAsync<FeatureBlobRow>(sql, ("@id", blobId));
        if (row?.BlobContents == null || (row.VesselID != vesselId && row.VesselID != -1)) return null;
        return (row.BlobContents, row.FileName);
    }

    private sealed class FeatureBlobRow
    {
        public int VesselID { get; set; }
        public string? FileName { get; set; }
        public byte[]? BlobContents { get; set; }
    }

    /// <summary>A child record needs its parent at the office, and the parent must be this vessel's (or for all vessels).</summary>
    private static async Task<string?> CheckParentAsync(SqlDb db, string table, Dictionary<string, object?> values, int vesselId)
    {
        (string Table, string Key, string Column)? parent = table switch
        {
            "SUG_Log" or "SUG_Blob" => ("SUG_Master", "SUG_MasterID", "SUG_MasterID"),
            "DMS_InboxRead" => ("DMS_Inbox", "DMS_InboxID", "DMS_InboxID"),
            _ => null,
        };
        if (parent == null) return null;
        var id = values.GetValueOrDefault(parent.Value.Column);
        if (id == null) return $"{table} has no {parent.Value.Column}.";
        var owner = await db.FirstAsync<OwnerRow>(
            $"SELECT Origin, VesselID FROM dbo.{Bracket(parent.Value.Table)} WHERE {Bracket(parent.Value.Key)} = @id", ("@id", id));
        if (owner == null) return $"{parent.Value.Table} {id} is not at the office yet.";
        if (owner.VesselID != vesselId && owner.VesselID != -1) return $"{parent.Value.Table} {id} belongs to another vessel.";
        return null;
    }

    private static object? FromJson(JsonElement e, string sqlType)
    {
        if (e.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
        switch (sqlType)
        {
            case "bigint": return e.ValueKind == JsonValueKind.String ? long.Parse(e.GetString()!, CultureInfo.InvariantCulture) : e.GetInt64();
            case "int":
            case "smallint":
            case "tinyint":
                if (e.ValueKind is JsonValueKind.True or JsonValueKind.False) return e.GetBoolean() ? 1 : 0;
                return e.ValueKind == JsonValueKind.String ? int.Parse(e.GetString()!, CultureInfo.InvariantCulture) : e.GetInt32();
            case "bit":
                return e.ValueKind is JsonValueKind.True or JsonValueKind.False ? e.GetBoolean() : e.GetInt32() != 0;
            case "decimal":
            case "numeric":
            case "money":
                return e.GetDecimal();
            case "float":
            case "real":
                return e.GetDouble();
            case "datetime":
            case "datetime2":
            case "smalldatetime":
            case "date":
                return DateTime.Parse(e.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            case "varbinary":
            case "binary":
            case "image":
                return e.GetBytesFromBase64();
            default:
                return e.ValueKind == JsonValueKind.String ? e.GetString() : e.GetRawText();
        }
    }

    private sealed class ColumnRow
    {
        public string Name { get; set; } = "";
        public string TypeName { get; set; } = "";
    }

    private sealed class OwnerRow
    {
        public string? Origin { get; set; }
        public int VesselID { get; set; }
    }
}
