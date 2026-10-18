using DMS.Application.DTO.Features;
using DMS.Application.Interface;

namespace DMS.Data.EF.Features;

/// <summary>
/// Feature 6: certificates are Other Documents whose type has IsCertificate = 1, with the certificate columns on
/// OtherDocumentMaster (CertificateNo, IssuedBy, IssueDate, ExpiryDate, NextSurveyDate, ReplacesID). A renewal is a new
/// record that replaces the old one; the old one stays as history. Each type has WarningDays for "expiring soon";
/// the inbox gets an entry when a certificate enters that window (dbo.Inbox_CertificateExpiry).
/// </summary>
public sealed class CertificateService : ICertificateService
{
    private readonly SqlDb _db;

    public CertificateService(SqlDb db) => _db = db;

    public async Task<List<CertificateItem>> GetListAsync(int? vesselId, string? status, bool includeReplaced)
    {
        var rows = await _db.QueryAsync<CertificateItem>(@"
SELECT d.OtherDocumentMasterID, d.VesselID, CASE WHEN d.VesselID > 0 THEN v.VslName ELSE 'Office' END AS VesselName,
       d.OtherDocument_TypeID, t.DocumentType, d.DocumentNumber, d.DocumentTitle, d.CertificateNo, d.IssuedBy, d.IssueDate,
       d.ExpiryDate, d.NextSurveyDate, d.ReplacesID, ISNULL(t.WarningDays, 30) AS WarningDays, d.Comments AS Remarks,
       (SELECT COUNT(*) FROM dbo.OtherDocument_Blob b WHERE b.OtherDocumentMasterID = d.OtherDocumentMasterID) AS FileCount,
       (SELECT MIN(b.OtherDocument_BlobID) FROM dbo.OtherDocument_Blob b WHERE b.OtherDocumentMasterID = d.OtherDocumentMasterID) AS FirstFileID
  FROM dbo.OtherDocumentMaster d
  JOIN dbo.OtherDocument_Type t ON t.OtherDocument_TypeID = d.OtherDocument_TypeID AND t.IsCertificate = 1
  LEFT JOIN dbo.V_Vessel v ON v.VesselID = d.VesselID
 WHERE ISNULL(d.IsDeleted, 0) <> 1
   AND (@vessel IS NULL OR d.VesselID = @vessel)
   AND (@all = 1 OR NOT EXISTS (SELECT 1 FROM dbo.OtherDocumentMaster n
                                 WHERE n.ReplacesID = d.OtherDocumentMasterID AND ISNULL(n.IsDeleted, 0) <> 1))
 ORDER BY CASE WHEN d.ExpiryDate IS NULL THEN 1 ELSE 0 END, d.ExpiryDate, v.VslName, t.DocumentType",
            ("@vessel", vesselId), ("@all", includeReplaced ? 1 : 0));
        return string.IsNullOrWhiteSpace(status) ? rows : rows.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public Task<List<CertificateType>> GetTypesAsync()
        => _db.QueryAsync<CertificateType>(@"
SELECT OtherDocument_TypeID, DocumentCode, DocumentType, CAST(ISNULL(IsCertificate, 0) AS bit) AS IsCertificate, ISNULL(WarningDays, 30) AS WarningDays
  FROM dbo.OtherDocument_Type WHERE ISNULL(IsActive, 1) = 1 ORDER BY SortOrder, DocumentType");

    public Task SaveTypeAsync(long typeId, SaveCertificateType request)
        => _db.ExecuteAsync("UPDATE dbo.OtherDocument_Type SET IsCertificate = @c, WarningDays = @w WHERE OtherDocument_TypeID = @id",
            ("@c", request.IsCertificate ? 1 : 0), ("@w", Math.Clamp(request.WarningDays, 0, 3650)), ("@id", typeId));

    public async Task<(long? Id, string? Error)> CreateAsync(NewCertificate request, OfficeUser user)
    {
        if (request.VesselId <= 0) return (null, "Choose the vessel.");
        if (await _db.ScalarAsync("SELECT 1 FROM dbo.V_Vessel WHERE VesselID = @v", ("@v", request.VesselId)) == null) return (null, "Vessel not found.");
        if (await _db.ScalarAsync("SELECT 1 FROM dbo.OtherDocument_Type WHERE OtherDocument_TypeID = @t AND IsCertificate = 1", ("@t", request.TypeId)) == null)
            return (null, "Choose a certificate type.");
        var title = (request.Title ?? "").Trim();
        if (title.Length == 0) return (null, "A title is required.");
        if (request.IssueDate != null && request.ExpiryDate != null && request.ExpiryDate < request.IssueDate)
            return (null, "The expiry date is before the issue date.");
        if (request.Files.Count == 0) return (null, "Attach the certificate.");
        var maxMb = int.TryParse(await _db.SettingAsync("DM_Settings", "FLD_MaxFileSizeMB"), out var m) && m > 0 ? m : 25;
        if (request.Files.Any(f => f.Bytes.Length > maxMb * 1024L * 1024L)) return (null, $"Each file must be {maxMb} MB or smaller.");
        if (request.ReplacesId != null &&
            await _db.ScalarAsync("SELECT 1 FROM dbo.OtherDocumentMaster WHERE OtherDocumentMasterID = @r AND VesselID = @v AND ISNULL(IsDeleted, 0) <> 1",
                ("@r", request.ReplacesId), ("@v", request.VesselId)) == null)
            return (null, "The certificate being renewed was not found for this vessel.");

        var id = await _db.NextIdAsync("OtherDocumentMaster");
        var firstBlob = await _db.NextIdAsync("OtherDocument_Blob", count: request.Files.Count);
        await _db.InTransactionAsync(async () =>
        {
            await _db.ExecuteAsync(@"
INSERT dbo.OtherDocumentMaster (OtherDocumentMasterID, Origin, VesselID, OtherDocument_TypeID, DocumentNumber, DocumentTitle, Status, Reference,
       Comments, UploadedOn, UploadedBy, IsDeleted, CreatedBy, CreatedDate, CertificateNo, IssuedBy, IssueDate, ExpiryDate, NextSurveyDate, ReplacesID)
VALUES (@id, 'HQ', @v, @t, @no, @title, 'Active', @certno, @rem, GETUTCDATE(), @me, 0, @me, GETUTCDATE(), @certno, @by, @issue, @exp, @survey, @rep)",
                ("@id", id), ("@v", request.VesselId), ("@t", request.TypeId),
                ("@no", Cut(request.DocumentNumber, 100) ?? Cut(request.CertificateNo, 100) ?? $"CERT-{id}"), ("@title", Cut(title, 250)),
                ("@certno", Cut(request.CertificateNo, 50)), ("@rem", request.Remarks), ("@me", user.UserId), ("@by", Cut(request.IssuedBy, 150)),
                ("@issue", request.IssueDate?.Date), ("@exp", request.ExpiryDate?.Date), ("@survey", request.NextSurveyDate?.Date),
                ("@rep", request.ReplacesId));
            var blobId = firstBlob;
            foreach (var f in request.Files)
            {
                var ext = Path.GetExtension(f.FileName).TrimStart('.').ToLowerInvariant();
                await _db.ExecuteAsync(@"
INSERT dbo.OtherDocument_Blob (OtherDocument_BlobID, OtherDocumentMasterID, Origin, VesselID, Description, FileName, FileType, OriginalFileType,
       BlobSize, BlobContents, AddedOn, HQ_Storage_Option, HQ_Local_Folder, HQ_DatabaseName)
VALUES (@id, @doc, 'HQ', @v, @d, @n, @t, @t, @size, @bytes, GETUTCDATE(), 'Database', '', '')",
                    ("@id", blobId++), ("@doc", id), ("@v", request.VesselId), ("@d", Cut(title, 250)), ("@n", Cut(Path.GetFileName(f.FileName), 250)),
                    ("@t", Cut(ext, 50)), ("@size", f.Bytes.Length), ("@bytes", f.Bytes));
            }
        });
        return (id, null);
    }

    public async Task<FileContent?> GetFileAsync(long blobId)
    {
        var row = await _db.FirstAsync<BlobRow>(
            "SELECT FileName, BlobContents FROM dbo.OtherDocument_Blob WHERE OtherDocument_BlobID = @id", ("@id", blobId));
        return row?.BlobContents == null ? null : new FileContent(row.BlobContents, ContentTypes.For(row.FileName), row.FileName ?? $"certificate-{blobId}");
    }

    private static string? Cut(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Length <= max ? s.Trim() : s.Trim()[..max];

    private sealed class BlobRow
    {
        public string? FileName { get; set; }
        public byte[]? BlobContents { get; set; }
    }
}
