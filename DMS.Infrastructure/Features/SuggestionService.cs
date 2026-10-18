using DMS.Application.DTO.Features;
using DMS.Application.Interface;

namespace DMS.Data.EF.Features;

/// <summary>
/// Feature 4: the suggestions workflow.
/// Submitted -> UnderReview -> NeedsInfo (answered on board, back to UnderReview) -> Accepted / Rejected -> Implemented -> Closed.
/// A vessel's suggestion keeps that vessel's VesselID, so the office's status changes and comments (SUG_Master, SUG_Log)
/// sync back to the ship; a suggestion raised at the office is office only (VesselID -2).
/// </summary>
public sealed class SuggestionService : ISuggestionService
{
    public static readonly string[] Statuses = { "Submitted", "UnderReview", "NeedsInfo", "Accepted", "Rejected", "Implemented", "Closed" };
    private static readonly string[] Priorities = { "Low", "Medium", "High" };
    private static readonly string[] ItemTypes = { "Manual", "Circular", "Alert", "Document" };

    /// <summary>The moves the office may make from each status.</summary>
    private static readonly Dictionary<string, string[]> Moves = new()
    {
        ["Submitted"] = new[] { "UnderReview", "NeedsInfo", "Accepted", "Rejected" },
        ["UnderReview"] = new[] { "NeedsInfo", "Accepted", "Rejected" },
        ["NeedsInfo"] = new[] { "UnderReview", "Accepted", "Rejected" },
        ["Accepted"] = new[] { "Implemented", "Rejected", "Closed" },
        ["Rejected"] = new[] { "UnderReview", "Closed" },
        ["Implemented"] = new[] { "Closed" },
        ["Closed"] = new[] { "UnderReview" },
    };

    private readonly SqlDb _db;

    public SuggestionService(SqlDb db) => _db = db;

    private const string ListSelect = @"
SELECT s.SUG_MasterID, s.VesselID,
       CASE WHEN s.VesselID > 0 THEN v.VslName ELSE 'Office' END AS VesselName,
       s.SuggestionNo, s.Title, s.ItemType, s.ItemID,
       CASE s.ItemType
            WHEN 'Manual' THEN (SELECT TOP (1) m.ManualNo + ' ' + ISNULL(m.ManualName, '') FROM dbo.DM_Manual m WHERE m.DM_ManualID = s.ItemID)
            WHEN 'Document' THEN (SELECT TOP (1) d.DocumentNumber + ' ' + d.DocumentTitle FROM dbo.OtherDocumentMaster d WHERE d.OtherDocumentMasterID = s.ItemID)
            WHEN 'Circular' THEN (SELECT TOP (1) ISNULL(c.CIR_Number, '') + ' ' + ISNULL(c.Title, '') FROM dbo.CIR_Master c WHERE c.CIR_MasterID = s.ItemID)
            WHEN 'Alert' THEN (SELECT TOP (1) ISNULL(c.CIR_Number, '') + ' ' + ISNULL(c.Title, '') FROM dbo.CIR_Master c WHERE c.CIR_MasterID = s.ItemID)
       END AS ItemTitle,
       s.Priority, s.Status, s.SubmittedBy, s.SubmittedByName, s.SubmittedRank, s.SubmittedOn, s.AssignedTo, s.ModifiedOn";

    public Task<List<SuggestionListItem>> GetListAsync(string? status, int? vesselId, string? submittedBy)
        => _db.QueryAsync<SuggestionListItem>(ListSelect + @"
  FROM dbo.SUG_Master s
  LEFT JOIN dbo.V_Vessel v ON v.VesselID = s.VesselID
 WHERE s.IsDeleted = 0
   AND (@status IS NULL OR s.Status = @status OR (@status = 'Open' AND s.Status NOT IN ('Rejected', 'Implemented', 'Closed')))
   AND (@vessel IS NULL OR s.VesselID = @vessel)
   AND (@by IS NULL OR s.SubmittedBy = @by)
 ORDER BY s.SubmittedOn DESC, s.SUG_MasterID DESC",
            ("@status", string.IsNullOrWhiteSpace(status) ? null : status), ("@vessel", vesselId),
            ("@by", string.IsNullOrWhiteSpace(submittedBy) ? null : submittedBy));

    public async Task<SuggestionDetail?> GetAsync(long id)
    {
        var detail = await _db.FirstAsync<SuggestionDetail>(ListSelect + @",
       s.Description, s.QuotedText, s.ImplementedItemType, s.ImplementedItemID
  FROM dbo.SUG_Master s
  LEFT JOIN dbo.V_Vessel v ON v.VesselID = s.VesselID
 WHERE s.SUG_MasterID = @id AND s.IsDeleted = 0", ("@id", id));
        if (detail == null) return null;
        detail.Log = await _db.QueryAsync<SuggestionLog>(
            "SELECT SUG_LogID, Status, Comment, UserID, UserName, LogDate FROM dbo.SUG_Log WHERE SUG_MasterID = @id ORDER BY LogDate, SUG_LogID",
            ("@id", id));
        detail.Files = await _db.QueryAsync<FileItem>(@"
SELECT SUG_BlobID AS Id, FileName, FileType, BlobSize, AddedOn AS UploadedOn
  FROM dbo.SUG_Blob WHERE SUG_MasterID = @id ORDER BY SUG_BlobID", ("@id", id));
        return detail;
    }

    public async Task<long> CreateAsync(NewSuggestion request, OfficeUser user)
    {
        var title = (request.Title ?? "").Trim();
        if (title.Length == 0) throw new ArgumentException("A title is required.");
        var itemType = ItemTypes.Contains(request.ItemType) ? request.ItemType : null;
        var priority = Priorities.Contains(request.Priority) ? request.Priority! : "Medium";
        var maxBytes = await MaxFileBytesAsync();
        if (request.Files.Any(f => f.Bytes.Length > maxBytes))
            throw new ArgumentException($"Each file must be {maxBytes / 1024 / 1024} MB or smaller.");

        var id = await _db.NextIdAsync("SUG_Master", vesselId: SqlDb.OfficeOnly);
        var logId = await _db.NextIdAsync("SUG_Log", vesselId: SqlDb.OfficeOnly);
        var firstBlob = request.Files.Count > 0 ? await _db.NextIdAsync("SUG_Blob", vesselId: SqlDb.OfficeOnly, count: request.Files.Count) : 0;
        await _db.InTransactionAsync(async () =>
        {
            await _db.ExecuteAsync(@"
INSERT dbo.SUG_Master (SUG_MasterID, Origin, VesselID, SuggestionNo, Title, Description, ItemType, ItemID, QuotedText, Priority, Status,
                       SubmittedBy, SubmittedByName, SubmittedRank, SubmittedOn, IsDeleted)
VALUES (@id, 'HQ', -2, @no, @title, @desc, @type, @item, @quote, @prio, 'Submitted', @by, @name, @rank, GETUTCDATE(), 0)",
                ("@id", id), ("@no", $"SUG-{DateTime.UtcNow:yyyy}-{id}"), ("@title", Cut(title, 250)), ("@desc", request.Description),
                ("@type", itemType), ("@item", itemType == null ? null : request.ItemId), ("@quote", Cut(request.QuotedText, 1000)),
                ("@prio", priority), ("@by", user.UserId), ("@name", user.UserName), ("@rank", user.Role));
            await AddLogAsync(logId, id, -2, "Submitted", null, user);
            var blobId = firstBlob;
            foreach (var f in request.Files)
                await _db.ExecuteAsync(@"
INSERT dbo.SUG_Blob (SUG_BlobID, Origin, VesselID, SUG_MasterID, FileName, FileType, BlobSize, BlobContents, AddedOn)
VALUES (@id, 'HQ', -2, @sug, @name, @type, @size, @bytes, GETUTCDATE())",
                    ("@id", blobId++), ("@sug", id), ("@name", Cut(Path.GetFileName(f.FileName), 250)),
                    ("@type", Cut(Path.GetExtension(f.FileName).TrimStart('.').ToLowerInvariant(), 50)),
                    ("@size", f.Bytes.Length), ("@bytes", f.Bytes));
        });
        return id;
    }

    public async Task<string?> ChangeStatusAsync(long id, SuggestionStatusChange request, OfficeUser user)
    {
        var current = await _db.FirstAsync<SuggestionListItem>(
            "SELECT SUG_MasterID, VesselID, Status, Title FROM dbo.SUG_Master WHERE SUG_MasterID = @id AND IsDeleted = 0", ("@id", id));
        if (current == null) return "Suggestion not found.";
        if (!Statuses.Contains(request.Status)) return "Unknown status.";
        if (request.Status == current.Status) return null;
        if (!Moves.TryGetValue(current.Status, out var allowed) || !allowed.Contains(request.Status))
            return $"A suggestion cannot move from {current.Status} to {request.Status}.";
        if (request.Status is "NeedsInfo" or "Rejected" && string.IsNullOrWhiteSpace(request.Comment))
            return request.Status == "NeedsInfo" ? "Say what information is needed." : "Give a reason for rejecting it.";

        var implType = request.Status == "Implemented" && ItemTypes.Contains(request.ImplementedItemType) ? request.ImplementedItemType : null;
        var logId = await _db.NextIdAsync("SUG_Log", vesselId: current.VesselID > 0 ? current.VesselID : -1);
        await _db.InTransactionAsync(async () =>
        {
            await _db.ExecuteAsync(@"
UPDATE dbo.SUG_Master
   SET Status = @s, ModifiedOn = GETUTCDATE(),
       AssignedTo = CASE WHEN @s = 'UnderReview' AND AssignedTo IS NULL THEN @me ELSE AssignedTo END,
       ImplementedItemType = CASE WHEN @s = 'Implemented' THEN @it ELSE ImplementedItemType END,
       ImplementedItemID = CASE WHEN @s = 'Implemented' THEN @ii ELSE ImplementedItemID END
 WHERE SUG_MasterID = @id",
                ("@s", request.Status), ("@me", user.UserId), ("@it", implType), ("@ii", implType == null ? null : request.ImplementedItemId),
                ("@id", id));
            await AddLogAsync(logId, id, current.VesselID, request.Status, request.Comment, user);
        });
        return null;
    }

    public async Task AddCommentAsync(long id, string comment, OfficeUser user)
    {
        if (string.IsNullOrWhiteSpace(comment)) return;
        var vessel = await _db.ScalarAsync("SELECT VesselID FROM dbo.SUG_Master WHERE SUG_MasterID = @id AND IsDeleted = 0", ("@id", id));
        if (vessel == null) throw new KeyNotFoundException("Suggestion not found.");
        var vesselId = Convert.ToInt32(vessel);
        var logId = await _db.NextIdAsync("SUG_Log", vesselId: vesselId > 0 ? vesselId : -1);
        await AddLogAsync(logId, id, vesselId, null, comment, user);
        await _db.ExecuteAsync("UPDATE dbo.SUG_Master SET ModifiedOn = GETUTCDATE() WHERE SUG_MasterID = @id", ("@id", id));
    }

    public async Task<FileContent?> GetFileAsync(long blobId)
    {
        var row = await _db.FirstAsync<BlobRow>("SELECT FileName, FileType, BlobContents FROM dbo.SUG_Blob WHERE SUG_BlobID = @id", ("@id", blobId));
        return row?.BlobContents == null ? null : new FileContent(row.BlobContents, ContentTypes.For(row.FileName), row.FileName ?? $"file-{blobId}");
    }

    /// <summary>Log rows carry the suggestion's VesselID, so a vessel's suggestion log syncs to that vessel.</summary>
    private Task AddLogAsync(long logId, long sugId, int vesselId, string? status, string? comment, OfficeUser user)
        => _db.ExecuteAsync(@"
INSERT dbo.SUG_Log (SUG_LogID, Origin, VesselID, SUG_MasterID, Status, Comment, UserID, UserName, LogDate)
VALUES (@id, 'HQ', @v, @sug, @s, @c, @u, @n, GETUTCDATE())",
            ("@id", logId), ("@v", vesselId), ("@sug", sugId), ("@s", status), ("@c", Cut(comment, 2000)), ("@u", user.UserId), ("@n", user.UserName));

    private async Task<long> MaxFileBytesAsync()
        => (int.TryParse(await _db.SettingAsync("DM_Settings", "FLD_MaxFileSizeMB"), out var mb) && mb > 0 ? mb : 25) * 1024L * 1024L;

    private static string? Cut(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Length <= max ? s.Trim() : s.Trim()[..max];

    private sealed class BlobRow
    {
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public byte[]? BlobContents { get; set; }
    }
}

/// <summary>Content type from a file name, for downloads.</summary>
public static class ContentTypes
{
    public static string For(string? fileName) => Path.GetExtension(fileName ?? "").ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".txt" => "text/plain",
        ".htm" or ".html" => "text/html",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".ppt" => "application/vnd.ms-powerpoint",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".zip" => "application/zip",
        _ => "application/octet-stream",
    };
}
