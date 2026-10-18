using DMS.Application.DTO.Features;
using DMS.Application.Interface;

namespace DMS.Data.EF.Features;

/// <summary>
/// Features 3 and 7: circular / alert links to manuals, "valid until" (required in categories with
/// RequiresValidUntil), the acknowledge-by days, cancel / supersede; and a manual's AckDueDays.
/// All of these sync to the vessels through the row triggers (CIR_ManualLink, CIR_Master, CIR_Category, DM_Manual).
/// </summary>
public sealed class CircularExtrasService : ICircularExtrasService
{
    private static readonly string[] LinkTypes = { "Amends", "Supersedes", "RefersTo" };
    private readonly SqlDb _db;

    public CircularExtrasService(SqlDb db) => _db = db;

    private const string LinkSelect = @"
SELECT l.CIR_ManualLinkID, l.CIR_MasterID, c.CIR_Number AS CircularNumber, c.Title AS CircularTitle, c.CircularType,
       c.Status AS CircularStatus, l.DM_ManualID, m.ManualNo, m.ManualName, l.DM_ManualVersionID, l.LinkType, l.Remarks,
       CAST(l.IsActive AS bit) AS IsActive, l.ClosedOn, l.CreatedOn
  FROM dbo.CIR_ManualLink l
  JOIN dbo.CIR_Master c ON c.CIR_MasterID = l.CIR_MasterID
  JOIN dbo.DM_Manual m ON m.DM_ManualID = l.DM_ManualID";

    public async Task<CircularExtras?> GetAsync(long circularId)
    {
        var extras = await _db.FirstAsync<CircularExtras>(@"
SELECT c.CIR_MasterID, c.Status, c.AckDueDays, c.ValidUntil, c.SupersededByID, s.CIR_Number AS SupersededByNumber,
       CAST(ISNULL(cat.RequiresValidUntil, 0) AS bit) AS RequiresValidUntil
  FROM dbo.CIR_Master c
  LEFT JOIN dbo.CIR_Master s ON s.CIR_MasterID = c.SupersededByID
  LEFT JOIN dbo.CIR_Category cat ON cat.CIR_CategoryID = c.CIR_CategoryID
 WHERE c.CIR_MasterID = @id", ("@id", circularId));
        if (extras == null) return null;
        extras.Links = await _db.QueryAsync<CircularManualLink>(LinkSelect + " WHERE l.CIR_MasterID = @id ORDER BY l.IsActive DESC, m.ManualNo",
            ("@id", circularId));
        return extras;
    }

    public async Task<string?> SaveAsync(long circularId, SaveCircularExtras request)
    {
        var current = await GetAsync(circularId);
        if (current == null) return "Circular not found.";
        if (request.AckDueDays is < 0 or > 3650) return "Acknowledge-by days must be between 0 and 3650.";
        if (current.RequiresValidUntil && request.ValidUntil == null) return "This category needs a \"valid until\" date.";
        await _db.ExecuteAsync("UPDATE dbo.CIR_Master SET AckDueDays = @d, ValidUntil = @v WHERE CIR_MasterID = @id",
            ("@d", request.AckDueDays), ("@v", request.ValidUntil?.Date), ("@id", circularId));
        return null;
    }

    public async Task<string?> AddLinkAsync(long circularId, AddManualLink request, OfficeUser user)
    {
        if (!LinkTypes.Contains(request.LinkType)) return "Link type must be Amends, Supersedes or RefersTo.";
        if (await _db.ScalarAsync("SELECT 1 FROM dbo.CIR_Master WHERE CIR_MasterID = @id", ("@id", circularId)) == null)
            return "Circular not found.";
        if (await _db.ScalarAsync("SELECT 1 FROM dbo.DM_Manual WHERE DM_ManualID = @id AND ISNULL(IsDeleted, 0) <> 1", ("@id", request.ManualId)) == null)
            return "Manual not found.";
        if (await _db.ScalarAsync("SELECT 1 FROM dbo.CIR_ManualLink WHERE CIR_MasterID = @c AND DM_ManualID = @m AND IsActive = 1",
                ("@c", circularId), ("@m", request.ManualId)) != null)
            return "This manual is already linked.";
        var id = await _db.NextIdAsync("CIR_ManualLink");
        await _db.ExecuteAsync(@"
INSERT dbo.CIR_ManualLink (CIR_ManualLinkID, Origin, VesselID, CIR_MasterID, DM_ManualID, DM_ManualVersionID, LinkType, Remarks, IsActive, CreatedBy, CreatedOn)
VALUES (@id, 'HQ', -1, @c, @m, @v, @t, @r, 1, @by, GETUTCDATE())",
            ("@id", id), ("@c", circularId), ("@m", request.ManualId), ("@v", request.ManualVersionId), ("@t", request.LinkType),
            ("@r", Cut(request.Remarks, 500)), ("@by", user.UserId));
        return null;
    }

    public Task RemoveLinkAsync(long linkId)
        => _db.ExecuteAsync("DELETE FROM dbo.CIR_ManualLink WHERE CIR_ManualLinkID = @id", ("@id", linkId));

    public async Task<string?> CancelAsync(long circularId, CancelCircular request, OfficeUser user)
    {
        var status = (string?)await _db.ScalarAsync("SELECT Status FROM dbo.CIR_Master WHERE CIR_MasterID = @id", ("@id", circularId));
        if (status == null) return "Circular not found.";
        if (status == "Cancelled") return "This circular is already cancelled.";
        if (status is "Temp" or "Draft") return "A draft cannot be cancelled; delete it instead.";
        if (request.SupersededById == circularId) return "A circular cannot supersede itself.";
        if (request.SupersededById != null &&
            await _db.ScalarAsync("SELECT 1 FROM dbo.CIR_Master WHERE CIR_MasterID = @id AND ISNULL(IsDeleted, 0) <> 1", ("@id", request.SupersededById)) == null)
            return "The superseding circular was not found.";
        var remarks = string.IsNullOrWhiteSpace(request.Reason) ? null : $"Cancelled by {user.UserName}: {request.Reason.Trim()}";
        await _db.ExecuteAsync(@"
UPDATE dbo.CIR_Master
   SET Status = 'Cancelled', SupersededByID = @s, ModifiedBy = @by, ModifiedDate = GETUTCDATE(),
       Remarks = CASE WHEN @r IS NULL THEN Remarks ELSE LEFT(ISNULL(Remarks + CHAR(13) + CHAR(10), '') + @r, 4000) END
 WHERE CIR_MasterID = @id",
            ("@s", request.SupersededById), ("@by", user.UserId), ("@r", remarks), ("@id", circularId));
        return null;
    }

    public Task<List<CircularManualLink>> GetForManualAsync(long manualId)
        => _db.QueryAsync<CircularManualLink>(LinkSelect + @"
 WHERE l.DM_ManualID = @id AND ISNULL(c.IsDeleted, 0) <> 1 AND ISNULL(c.Status, '') NOT IN ('Temp', 'Draft')
 ORDER BY l.IsActive DESC, COALESCE(c.ReleasedDate, c.DateIssued) DESC", ("@id", manualId));

    public Task<int> CloseLinksForManualAsync(long manualId)
        => _db.ExecuteAsync(@"UPDATE dbo.CIR_ManualLink SET IsActive = 0, ClosedOn = GETUTCDATE()
                              WHERE DM_ManualID = @id AND IsActive = 1 AND LinkType IN ('Amends', 'Supersedes')", ("@id", manualId));

    public Task<List<CircularCategorySetting>> GetCategoriesAsync()
        => _db.QueryAsync<CircularCategorySetting>(@"
SELECT CIR_CategoryID, CircularType, CategoryCode, Category, CAST(ISNULL(RequiresValidUntil, 0) AS bit) AS RequiresValidUntil
  FROM dbo.CIR_Category WHERE ISNULL(isActive, 1) = 1 ORDER BY CircularType, Sortorder, Category");

    public Task SaveCategoryAsync(long categoryId, bool requiresValidUntil)
        => _db.ExecuteAsync("UPDATE dbo.CIR_Category SET RequiresValidUntil = @r WHERE CIR_CategoryID = @id",
            ("@r", requiresValidUntil ? 1 : 0), ("@id", categoryId));

    public Task<ManualExtras?> GetManualAsync(long manualId)
        => _db.FirstAsync<ManualExtras>("SELECT DM_ManualID, AckDueDays FROM dbo.DM_Manual WHERE DM_ManualID = @id", ("@id", manualId));

    public Task SaveManualAsync(long manualId, int? ackDueDays)
        => _db.ExecuteAsync("UPDATE dbo.DM_Manual SET AckDueDays = @d WHERE DM_ManualID = @id",
            ("@d", ackDueDays is null ? null : Math.Clamp(ackDueDays.Value, 0, 3650)), ("@id", manualId));

    private static string? Cut(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Length <= max ? s.Trim() : s.Trim()[..max];
}
