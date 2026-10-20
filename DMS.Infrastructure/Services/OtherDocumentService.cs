using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DMS.Application.DTO.Features;
using DMS.Application.DTO.OtherDocument;
using DMS.Application.Interface;
using DMS.Data.EF;
using DMS.Data.EF.Context;
using DMS.Data.EF.Features;
using DMS.Data.EF.Models.OtherDocument;
using DMS.Data.EF.Query.Circulars;

namespace DMS.Application;

public class OtherDocumentService : IOtherDocumentService
{
    private readonly OtherDocumentsContext _dbContext;
    private readonly ILogger<OtherDocumentService> _logger;
    private readonly IMediator _mediator;
    private readonly SqlDb _db;
    private readonly GeneralContext _generalContext;

    public OtherDocumentService(OtherDocumentsContext context, ILogger<OtherDocumentService> logger, IMediator mediator, SqlDb db, GeneralContext generalContext)
    {
        _dbContext = context;
        _logger = logger;
        _mediator = mediator;
        _db = db;
        _generalContext = generalContext;
    }

    public async Task<List<OtherDocument_Type>> GetDocumentTypesAsync()
    {
        var query = _dbContext.OtherDocument_Type.AsQueryable();

        return await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.DocumentType)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Vw_OtherDocument_MasterBlob>> GetRecentOtherDocumentsAsync(int limit = 10)
    {
        return await _dbContext.Vw_OtherDocument
            .OrderByDescending(v => v.UploadedOn)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Vw_OtherDocument_MasterBlob>> GetDocumentsAsync(int? vesselId = null, string? documentType = null)
    {
        var query = _dbContext.Vw_OtherDocument.AsQueryable();

        if (vesselId.HasValue)
        {
            query = query.Where(v => v.VesselID == vesselId.Value || (vesselId.Value == 0 && v.VesselID == 0));
        }

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            query = query.Where(v => v.DocumentType == documentType.Trim());
        }

        return await query
            .OrderByDescending(v => v.UploadedOn)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Vw_OtherDocument_MasterBlob?> GetOtherDocumentDetailsAsync(long otherDocumentMasterID)
    {
        return await _dbContext.Vw_OtherDocument
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.OtherDocumentMasterID == otherDocumentMasterID);
    }

    public async Task<long> InsertOtherDocumentRecordAsync()
    {
        try
        {
            var nextId = await _dbContext.NextIdAsync<OtherDocumentMaster>();

            var entity = new OtherDocumentDto
            {
                OtherDocumentMasterID = nextId,
                DocumentTitle = string.Empty,
                DocumentNumber = string.Empty,
                VesselID = -1,
                UploadedOn = DateTime.UtcNow
            };

            // _dbContext.OtherDocument.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity.OtherDocumentMasterID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert OtherDocument record.");
            throw;
        }
    }

    public async Task<long> AddOtherDocumentAsync(CreateOtherDocumentDto model)
    {
        if (model is null)
            throw new ArgumentNullException(nameof(model));

        var result = await _mediator.Send(new CreateOtherDocumentCommand(model));
        return result;
    }

    public async Task RemoveTempOtherDocumentAsync(long otherDocumentMasterID)
    {
        try
        {
            var entity = await _dbContext.OtherDocumentMaster
                            .Where(e => e.OtherDocumentMasterID == otherDocumentMasterID)
                            .FirstOrDefaultAsync();

            if (entity != null)
            {
                _dbContext.OtherDocumentMaster.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove OtherDocument record with id {OtherDocumentMasterID}", otherDocumentMasterID);
        }
    }

    public async Task<string> GetNextDocumentNumberAsync(long typeId)
    {
        var nextDocumentNumber = "0.0";
        var maxDocumentNumber = await _dbContext.OtherDocumentMaster
                             .Where(x => x.OtherDocument_TypeID == typeId)
                             .OrderByDescending(x => x.DocumentNumber)
                             .Select(m => m.DocumentNumber)
                             .FirstOrDefaultAsync();
        if (maxDocumentNumber != null)
        {
            string[] parts = maxDocumentNumber.Split('.');
            int major = int.Parse(parts[0]) ;
            nextDocumentNumber = $"{major}.0";
        }
        else
        {
            nextDocumentNumber = $"1.0";
        }

        var docType = await _dbContext.Set<OtherDocument_Type>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.OtherDocument_TypeID == typeId);

        if (docType == null) return "";

        var prefix = (docType.DocumentCode ?? string.Empty).Trim();
        nextDocumentNumber = $"{prefix}{nextDocumentNumber}";

        return nextDocumentNumber;
    }

    // ================================================================== Folders
    //
    // Folders for Other Documents (FLD_Folder); a folder holds Other Documents only (OtherDocumentMaster.FLD_FolderID).
    //   Company    - VesselID -1 (every vessel) or one vessel; synced down; on board ViewRoles / EditRoles (ranks) apply.
    //   Vessel     - one vessel's own folders (VesselID > 0), made at the office or on board; synced both ways.
    //   Department - office: VesselID -2, OwnerUserID = the office department; on board: the vessel's department folders.
    //   Personal   - VesselID -2 at the office, owner only. Personal folders made on board never leave the ship.
    // A sub-folder takes its scope, vessel and owner from its parent.

    public static readonly string[] FolderScopes = { "Company", "Vessel", "Department", "Personal" };

    private sealed class FolderRow : FolderItem
    {
        public string? Origin { get; set; }
        public string? CreatedBy { get; set; }
        public int? SortOrder { get; set; }
        public bool CanView { get; set; }
    }

    private static bool IsAdmin(OfficeUser user) => string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);

    private static bool InList(string? csv, string? value)
        => !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(csv) &&
           csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Any(r => string.Equals(r, value, StringComparison.OrdinalIgnoreCase));

    /// <summary>Who may open / change a folder, from the office.</summary>
    private static T Rights<T>(T f, OfficeUser user) where T : FolderItem
    {
        switch (f.Scope)
        {
            case "Personal":
                if (f is FolderRow fr1)
                    fr1.CanView = fr1.CanEdit = fr1.VesselID == SqlDb.OfficeOnly && fr1.OwnerUserID == user.UserId;
                break;
            case "Department" when f.VesselID == SqlDb.OfficeOnly:
                if (f is FolderRow fr2)
                {
                    fr2.CanView = IsAdmin(user) || string.Equals(fr2.OwnerUserID, user.Dept, StringComparison.OrdinalIgnoreCase);
                    fr2.CanEdit = fr2.CanView;
                }
                break;
            default: // Company, Vessel, and a vessel's department folders: every office user may look
                if (f is FolderRow fr3)
                {
                    fr3.CanView = true;
                    // A vessel's folders: every office user may file documents in them (office documents stay at the office).
                    fr3.CanEdit = IsAdmin(user) || fr3.Scope == "Vessel"
                                  || (fr3.Origin == SqlDb.Office && (fr3.CreatedBy == user.UserId || InList(fr3.EditRoles, user.Role)));
                }
                break;
        }
        return f;
    }

    private async Task<FolderRow?> FolderAsync(long id, OfficeUser user)
    {
        var f = (await LoadFolderRowsAsync()).FirstOrDefault(x => x.FLD_FolderID == id);
        return f == null ? null : Rights(f, user);
    }

    public async Task<List<FolderItem>> GetFolderTreeAsync(OfficeUser user)
    {
        var rows = await LoadFolderRowsAsync();
        return rows
            .Where(f => f.Scope is "Company" or "Vessel"
                || (f.Scope == "Department" && (f.VesselID > 0 || f.OwnerUserID == user.Dept || IsAdmin(user)))
                || (f.Scope == "Personal" && f.VesselID == SqlDb.OfficeOnly && f.OwnerUserID == user.UserId))
            .OrderBy(f => f.Scope == "Company" ? 0 : f.Scope == "Vessel" ? 1 : f.Scope == "Department" ? 2 : 3)
            .ThenBy(f => f.SortOrder).ThenBy(f => f.FolderName)
            .Select(f => Rights(f, user)).Where(f => f.CanView).Cast<FolderItem>().ToList();
    }

    private async Task<List<FolderRow>> LoadFolderRowsAsync()
    {
        var folders = await _dbContext.FldFolders.AsNoTracking().Where(f => f.IsDeleted == 0).ToListAsync();
        var folderIds = folders.Select(f => f.FLD_FolderID).ToList();
        var fileCounts = await _dbContext.OtherDocumentMaster.AsNoTracking()
            .Where(d => d.FLD_FolderID.HasValue && folderIds.Contains(d.FLD_FolderID.Value) && (d.IsDeleted == null || d.IsDeleted != 1))
            .GroupBy(d => d.FLD_FolderID!.Value).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        var vessels = await _generalContext.VVessels.AsNoTracking().ToDictionaryAsync(v => v.VesselID, v => v.VslName);
        return folders.Select(f => new FolderRow
        {
            FLD_FolderID = f.FLD_FolderID, Origin = f.Origin, VesselID = f.VesselID,
            VesselName = f.VesselID > 0 ? vessels.GetValueOrDefault(f.VesselID) : f.VesselID == -1 ? "All vessels" : "Office",
            ParentID = f.ParentID, FolderName = f.FolderName, Scope = f.Scope, OwnerUserID = f.OwnerUserID,
            ViewRoles = f.ViewRoles, EditRoles = f.EditRoles, CreatedBy = f.CreatedBy,
            SortOrder = f.SortOrder, FileCount = fileCounts.GetValueOrDefault(f.FLD_FolderID)
        }).ToList();
    }

    public async Task<(long? Id, string? Error)> SaveFolderAsync(long? folderId, SaveFolder request, OfficeUser user)
    {
        var name = (request.Name ?? "").Trim();
        if (name.Length == 0 || name.Length > 150) return (null, "A folder name of up to 150 characters is required.");
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return (null, "The folder name has characters that are not allowed.");

        FolderRow? parent = null;
        if (request.ParentId != null)
        {
            parent = await FolderAsync(request.ParentId.Value, user);

            if (parent == null) return (null, "The parent folder was not found.");
            if (!parent.CanEdit) return (null, "You cannot add folders here.");
        }

        if (folderId != null)
        {
            var current = await FolderAsync(folderId.Value, user);
            if (current == null) return (null, "Folder not found.");
            if (!current.CanEdit) return (null, "You cannot change this folder.");

            if (parent != null && (parent.Scope != current.Scope || parent.VesselID != current.VesselID))
                return (null, "A folder can only move under a folder of the same kind and vessel.");

            if (parent != null && await IsBelowAsync(parent.FLD_FolderID, current.FLD_FolderID))
                return (null, "A folder cannot move into itself.");

            if (await NameTakenAsync(request.ParentId, name, current.Scope, current.VesselID, current.OwnerUserID, current.FLD_FolderID))
                return (null, "There is already a folder with this name here.");

            await _db.ExecuteAsync(@"
                    UPDATE dbo.FLD_Folder SET FolderName = @n, ParentID = @p, ViewRoles = @vr, EditRoles = @er WHERE FLD_FolderID = @id",
                        ("@n", name), ("@p", request.ParentId), ("@vr", Roles(request.ViewRoles)), ("@er", Roles(request.EditRoles)),
                        ("@id", current.FLD_FolderID));
            return (current.FLD_FolderID, null);
        }

        string scope;
        int vesselId;
        string? owner;
        if (parent != null)
        {
            (scope, vesselId, owner) = (parent.Scope, parent.VesselID, parent.OwnerUserID);
        }
        else
        {
            scope = FolderScopes.FirstOrDefault(s => string.Equals(s, request.Scope, StringComparison.OrdinalIgnoreCase)) ?? "";
            switch (scope)
            {
                case "Company":
                    if (!IsAdmin(user)) return (null, "Only an administrator can add company folders.");

                    vesselId = request.VesselId is > 0 ? request.VesselId.Value : SqlDb.AllVessels;
                    owner = null;
                    break;
                case "Vessel":
                    if (request.VesselId is not > 0) return (null, "Choose the vessel.");

                    vesselId = request.VesselId.Value;
                    owner = null;
                    break;
                case "Department":
                    if (string.IsNullOrWhiteSpace(user.Dept)) return (null, "Your user has no department.");

                    vesselId = SqlDb.OfficeOnly;
                    owner = user.Dept;
                    break;
                case "Personal":
                    vesselId = SqlDb.OfficeOnly;
                    owner = user.UserId;
                    break;
                default:
                    return (null, "Scope must be Company, Vessel, Department or Personal.");
            }

            if (vesselId > 0 && await _db.ScalarAsync("SELECT 1 FROM dbo.V_Vessel WHERE VesselID = @v", ("@v", vesselId)) == null)
                return (null, "Vessel not found.");
        }

        if (await NameTakenAsync(request.ParentId, name, scope, vesselId, owner, null))
            return (null, "There is already a folder with this name here.");

        var id = await _db.NextIdAsync("FLD_Folder");
        await _db.ExecuteAsync(@"
                INSERT dbo.FLD_Folder (FLD_FolderID, Origin, VesselID, ParentID, FolderName, Scope, OwnerUserID, ViewRoles, EditRoles, SortOrder, CreatedBy, CreatedOn, IsDeleted)
                VALUES (@id, 'HQ', @v, @p, @n, @s, @o, @vr, @er, NULL, @me, GETUTCDATE(), 0)",
            ("@id", id), ("@v", vesselId), ("@p", request.ParentId), ("@n", name), ("@s", scope), ("@o", owner),
            ("@vr", Roles(request.ViewRoles)), ("@er", Roles(request.EditRoles)), ("@me", user.UserId));
        return (id, null);
    }

    public async Task<string?> DeleteFolderAsync(long folderId, OfficeUser user)
    {
        var f = await FolderAsync(folderId, user);
        if (f == null) return "Folder not found.";
        if (!f.CanEdit) return "You cannot delete this folder.";
        if (await _db.ScalarAsync("SELECT TOP (1) 1 FROM dbo.FLD_Folder WHERE ParentID = @id AND IsDeleted = 0", ("@id", folderId)) != null)
            return "Delete or move the sub-folders first.";
        if (f.FileCount > 0) return "Move or delete the documents in it first.";
        await _db.ExecuteAsync("UPDATE dbo.FLD_Folder SET IsDeleted = 1 WHERE FLD_FolderID = @id", ("@id", folderId));
        return null;
    }

    // ------------------------------------------------------------------ Other Documents in folders
    //
    // A document added at the office is office only: IsForOfficeOnly = 1, never sent to a vessel. It keeps the vessel it is
    // about as its VesselID (-2 when added without a vessel), so it is listed under that vessel with the vessel's own documents.

    /// <summary>Documents that belong under this vessel: its own, those for all vessels, and office-only ones about it.</summary>
    private const string ForVessel = "d.VesselID IN (@v, -1)";

    /// <summary>A folder's documents; folderId null = documents not in a folder (or in a folder that no longer exists).</summary>
    public async Task<List<FolderDocument>> GetFolderDocumentsAsync(long? folderId, int? vesselId, OfficeUser user)
    {
        if (folderId != null)
        {
            var folder = await FolderAsync(folderId.Value, user);
            if (folder is not { CanView: true }) return new List<FolderDocument>();
            var documents = await _dbContext.OtherDocumentMaster.AsNoTracking()
                .Where(d => (d.IsDeleted == null || d.IsDeleted != 1) && d.FLD_FolderID == folderId)
                .ToListAsync();
            return await ProjectDocumentsAsync(documents, vesselId, orderByType: true);
        }
        var unfiled = await _dbContext.OtherDocumentMaster.AsNoTracking()
            .Where(d => (d.IsDeleted == null || d.IsDeleted != 1) && d.FLD_FolderID == null && (vesselId.HasValue || d.VesselID != SqlDb.OfficeOnly))
            .ToListAsync();
        return await ProjectDocumentsAsync(unfiled, vesselId, orderByType: false);
    }

    /// <summary>Every document under a vessel, in any folder or none, without those in folders this user may not open.</summary>
    public async Task<List<FolderDocument>> GetVesselDocumentsAsync(int vesselId, OfficeUser user)
    {
        var visible = (await GetFolderTreeAsync(user)).Select(f => f.FLD_FolderID).ToHashSet();
        var entities = await _dbContext.OtherDocumentMaster.AsNoTracking()
            .Where(d => (d.IsDeleted == null || d.IsDeleted != 1) && (d.VesselID == vesselId || d.VesselID == -1))
            .ToListAsync();
        var docs = await ProjectDocumentsAsync(entities, vesselId, orderByType: false);
        foreach (var d in docs)
            if (d.FLD_FolderID != null && d.FolderName == null) d.FLD_FolderID = null; // its folder was deleted
        return docs.Where(d => d.FLD_FolderID == null || visible.Contains(d.FLD_FolderID.Value)).ToList();
    }

    private async Task<List<FolderDocument>> ProjectDocumentsAsync(List<OtherDocumentMaster> entities, int? vesselId, bool orderByType)
    {
        if (vesselId.HasValue)
            entities = entities.Where(d => d.VesselID == vesselId || d.VesselID == -1).ToList();

        var typeIds = entities.Where(d => d.OtherDocument_TypeID.HasValue).Select(d => d.OtherDocument_TypeID!.Value).Distinct().ToList();
        var folderIds = entities.Where(d => d.FLD_FolderID.HasValue).Select(d => d.FLD_FolderID!.Value).Distinct().ToList();
        var types = await _dbContext.OtherDocument_Type.AsNoTracking().Where(t => typeIds.Contains(t.OtherDocument_TypeID)).ToDictionaryAsync(t => t.OtherDocument_TypeID);
        var folders = await _dbContext.FldFolders.AsNoTracking().Where(f => folderIds.Contains(f.FLD_FolderID) && f.IsDeleted == 0).ToDictionaryAsync(f => f.FLD_FolderID);
        var vesselNames = await _generalContext.VVessels.AsNoTracking().ToDictionaryAsync(v => v.VesselID, v => v.VslName);
        var documentIds = entities.Select(d => d.OtherDocumentMasterID).ToList();
        var blobs = await _dbContext.OtherDocument_Blob.AsNoTracking().Where(b => documentIds.Contains(b.OtherDocumentMasterID)).GroupBy(b => b.OtherDocumentMasterID).ToDictionaryAsync(g => g.Key, g => new { Count = g.Count(), First = g.Min(b => b.OtherDocument_BlobID) });

        var result = entities.Take(2000).Select(d =>
        {
            types.TryGetValue(d.OtherDocument_TypeID ?? 0, out var type);
            folders.TryGetValue(d.FLD_FolderID ?? 0, out var folder);
            blobs.TryGetValue(d.OtherDocumentMasterID, out var blob);
            return new FolderDocument { OtherDocumentMasterID = d.OtherDocumentMasterID, VesselID = d.VesselID,
                IsForOfficeOnly = d.IsForOfficeOnly == 1,
                VesselName = d.VesselID > 0 ? vesselNames.GetValueOrDefault(d.VesselID) : d.VesselID == -1 ? "All vessels" : "Office",
                FLD_FolderID = d.FLD_FolderID, FolderName = folder?.FolderName, OtherDocument_TypeID = d.OtherDocument_TypeID,
                DocumentType = type?.DocumentType, DocumentNumber = d.DocumentNumber, DocumentTitle = d.DocumentTitle,
                Status = d.Status, Reference = d.Reference, Comments = d.Comments, UploadedOn = d.UploadedOn,
                UploadedBy = d.UploadedBy, Origin = d.Origin, IsCertificate = type?.IsCertificate == 1,
                ExpiryDate = d.ExpiryDate, FileCount = blob?.Count ?? 0, FirstFileID = blob?.First };
        });
        return (orderByType ? result.OrderBy(d => d.DocumentType).ThenBy(d => d.DocumentNumber) : result.OrderByDescending(d => d.UploadedOn)).ToList();
    }

    /// <summary>
    /// A document added at the office, in a folder or not. Always office only (IsForOfficeOnly = 1): it is never sent to a
    /// vessel. VesselID = the selected vessel (request.VesselId), or -2 when none was chosen.
    /// </summary>
    public async Task<(long? Id, string? Error)> CreateDocumentAsync(long? folderId, NewFolderDocument request, OfficeUser user)
    {
        if (folderId != null)
        {
            var folder = await FolderAsync(folderId.Value, user);
            if (folder == null) return (null, "Folder not found.");
            if (!folder.CanEdit) return (null, "You cannot add documents to this folder.");
        }
        var number = (request.DocumentNumber ?? "").Trim();
        var title = (request.Title ?? "").Trim();
        if (number.Length == 0 || number.Length > 50) return (null, "Enter a document number of up to 50 characters.");
        if (title.Length == 0 || title.Length > 500) return (null, "Enter a title of up to 500 characters.");
        if (await _db.ScalarAsync("SELECT 1 FROM dbo.OtherDocument_Type WHERE OtherDocument_TypeID = @t AND ISNULL(IsActive, 1) = 1", ("@t", request.TypeId)) == null)
            return (null, "Choose a document type.");

        // The vessel the document is about: the selected vessel, else -2 (office, no vessel).
        var vesselId = request.VesselId is > 0 ? request.VesselId.Value : SqlDb.OfficeOnly;
        if (vesselId > 0 && await _db.ScalarAsync("SELECT 1 FROM dbo.V_Vessel WHERE VesselID = @v", ("@v", vesselId)) == null)
            return (null, "Choose a vessel that exists.");
        if (folderId != null && (await FolderAsync(folderId.Value, user))?.VesselID is > 0 and var folderVessel && folderVessel != vesselId)
            return (null, "That folder belongs to another vessel.");

        if (await _db.ScalarAsync(@"SELECT TOP (1) 1 FROM dbo.OtherDocumentMaster WHERE ISNULL(IsDeleted, 0) <> 1
                                      AND OtherDocument_TypeID = @t AND DocumentNumber = @n
                                      AND VesselID = @v AND IsForOfficeOnly = 1",
                ("@t", request.TypeId), ("@n", number), ("@v", vesselId)) != null)
            return (null, $"A document numbered {number} of this type is already there.");

        var maxBytes = await MaxFileBytesAsync();
        var files = request.Files.Where(f => f.Bytes.Length > 0).ToList();
        if (files.Any(f => f.Bytes.Length > maxBytes)) return (null, $"Files must be {maxBytes / 1024 / 1024} MB or smaller.");

        var id = await _db.NextIdAsync("OtherDocumentMaster");
        var blobId = files.Count > 0 ? await _db.NextIdAsync("OtherDocument_Blob", count: files.Count) : 0;
        await _db.InTransactionAsync(async () =>
        {
            await _db.ExecuteAsync(@"
INSERT dbo.OtherDocumentMaster (OtherDocumentMasterID, Origin, VesselID, IsForOfficeOnly, OtherDocument_TypeID, DocumentNumber, DocumentTitle,
       Status, Reference, Comments, UploadedOn, UploadedBy, IsDeleted, CreatedBy, CreatedDate, FLD_FolderID)
VALUES (@id, 'HQ', @v, 1, @t, @n, @title, @s, @r, @c, GETUTCDATE(), @me, 0, @me, GETUTCDATE(), @f)",
                ("@id", id), ("@v", vesselId), ("@t", request.TypeId), ("@n", number), ("@title", title),
                ("@s", Cut(request.Status, 50) ?? "Active"), ("@r", Cut(request.Reference, 150)), ("@c", Cut(request.Comments, 500)),
                ("@me", user.UserId), ("@f", folderId));
            foreach (var f in files)
            {
                var name = Path.GetFileName(f.FileName);
                var ext = Path.GetExtension(name).TrimStart('.').ToLowerInvariant();
                await _db.ExecuteAsync(@"
INSERT dbo.OtherDocument_Blob (OtherDocument_BlobID, OtherDocumentMasterID, Origin, VesselID, IsForOfficeOnly, Description, FileName, FileType, OriginalFileType,
       BlobSize, BlobContents, AddedOn, HQ_Storage_Option, HQ_Local_Folder, HQ_DatabaseName)
VALUES (@id, @doc, 'HQ', @v, 1, @d, @n, @t, @t, @size, @bytes, GETUTCDATE(), 'Database', '', '')",
                    ("@id", blobId++), ("@doc", id), ("@v", vesselId), ("@d", Cut(name, 150)), ("@n", Cut(name, 150)), ("@t", Cut(ext, 50)),
                    ("@size", f.Bytes.Length), ("@bytes", f.Bytes));
            }
        });
        return (id, null);
    }

    /// <summary>Files a document in a folder (null = take it out of its folder).</summary>
    public async Task<string?> MoveDocumentAsync(long documentId, long? folderId, OfficeUser user)
    {
        var doc = (await ProjectDocumentsAsync(await _dbContext.OtherDocumentMaster.AsNoTracking()
            .Where(d => d.OtherDocumentMasterID == documentId && (d.IsDeleted == null || d.IsDeleted != 1)).ToListAsync(), null, false)).FirstOrDefault();
        if (doc == null) return "Document not found.";
        if (doc.FLD_FolderID != null && await FolderAsync(doc.FLD_FolderID.Value, user) is { CanEdit: false })
            return "You cannot move documents out of its folder.";
        if (folderId != null)
        {
            var target = await FolderAsync(folderId.Value, user);
            if (target == null) return "Folder not found.";
            if (!target.CanEdit) return "You cannot add documents to that folder.";
            if (target.VesselID > 0 && doc.VesselID > 0 && doc.VesselID != target.VesselID)
                return "That folder belongs to another vessel.";
            // A vessel's document in an office-only folder would vanish from the vessel's folders.
            if (!doc.IsForOfficeOnly && target.VesselID == SqlDb.OfficeOnly)
                return "A vessel's document cannot go into an office-only folder.";
        }
        await _db.ExecuteAsync("UPDATE dbo.OtherDocumentMaster SET FLD_FolderID = @f, ModifiedBy = @me, ModifiedDate = GETUTCDATE() WHERE OtherDocumentMasterID = @id",
            ("@f", folderId), ("@me", user.UserId), ("@id", documentId));
        return null;
    }

    /// <summary>Deletes a document added at the office (office only). Documents from or for a vessel are not deleted here.</summary>
    public async Task<string?> DeleteDocumentAsync(long documentId, OfficeUser user)
    {
        var doc = (await ProjectDocumentsAsync(await _dbContext.OtherDocumentMaster.AsNoTracking()
            .Where(d => d.OtherDocumentMasterID == documentId && (d.IsDeleted == null || d.IsDeleted != 1)).ToListAsync(), null, false)).FirstOrDefault();
        if (doc == null) return "Document not found.";
        if (!doc.IsForOfficeOnly) return "Only documents added at the office can be deleted here.";
        if (doc.FLD_FolderID != null && await FolderAsync(doc.FLD_FolderID.Value, user) is { CanEdit: false })
            return "You cannot delete documents in this folder.";
        await _db.ExecuteAsync("UPDATE dbo.OtherDocumentMaster SET IsDeleted = 1, ModifiedBy = @me, ModifiedDate = GETUTCDATE() WHERE OtherDocumentMasterID = @id",
            ("@me", user.UserId), ("@id", documentId));
        return null;
    }

    private sealed class DocumentBlobRow
    {
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public byte[]? BlobContents { get; set; }
    }

    public async Task<FileContent?> GetDocumentFileAsync(long blobId)
    {
        var row = await _db.FirstAsync<DocumentBlobRow>(@"
SELECT b.FileName, b.FileType, b.BlobContents
  FROM dbo.OtherDocument_Blob b
  JOIN dbo.OtherDocumentMaster d ON d.OtherDocumentMasterID = b.OtherDocumentMasterID
 WHERE b.OtherDocument_BlobID = @id AND ISNULL(d.IsDeleted, 0) <> 1", ("@id", blobId));
        if (row?.BlobContents == null) return null;
        var fileName = string.IsNullOrWhiteSpace(row.FileName) ? $"file-{blobId}" : row.FileName;
        // FileType holds a MIME type or an extension; the file name decides when it is not a MIME type.
        var contentType = row.FileType != null && row.FileType.Contains('/') ? row.FileType : DMS.Infrastructure.Services.ContentTypes.For(fileName);
        return new FileContent(row.BlobContents, contentType, fileName);
    }

    public Task<List<FileItem>> GetDocumentFilesAsync(long documentId)
        => _db.QueryAsync<FileItem>(@"
SELECT OtherDocument_BlobID AS Id, OtherDocumentMasterID AS FolderID, COALESCE(NULLIF(FileName, ''), Description, 'File') AS FileName,
       FileType, BlobSize, AddedOn AS UploadedOn
  FROM dbo.OtherDocument_Blob WHERE OtherDocumentMasterID = @id ORDER BY OtherDocument_BlobID", ("@id", documentId));

    private async Task<bool> IsBelowAsync(long folderId, long ancestorId)
    {
        var found = await _db.ScalarAsync(@"
WITH up AS (
    SELECT FLD_FolderID, ParentID, 0 AS Depth FROM dbo.FLD_Folder WHERE FLD_FolderID = @f
    UNION ALL
    SELECT p.FLD_FolderID, p.ParentID, up.Depth + 1 FROM dbo.FLD_Folder p JOIN up ON p.FLD_FolderID = up.ParentID WHERE up.Depth < 50
)
SELECT TOP (1) 1 FROM up WHERE FLD_FolderID = @a", ("@f", folderId), ("@a", ancestorId));
        return found != null;
    }

    private async Task<bool> NameTakenAsync(long? parentId, string name, string scope, int vesselId, string? owner, long? exceptId)
        => await _db.ScalarAsync(@"
SELECT TOP (1) 1 FROM dbo.FLD_Folder
 WHERE IsDeleted = 0 AND FolderName = @n AND Scope = @s AND VesselID = @v
   AND ((@p IS NULL AND ParentID IS NULL) OR ParentID = @p)
   AND ((@o IS NULL AND OwnerUserID IS NULL) OR OwnerUserID = @o)
   AND (@x IS NULL OR FLD_FolderID <> @x)",
            ("@n", name), ("@s", scope), ("@v", vesselId), ("@p", parentId), ("@o", owner), ("@x", exceptId)) != null;

    /// <summary>Largest file allowed on a document (DM_Settings FLD_MaxFileSizeMB, default 25 MB).</summary>
    private async Task<long> MaxFileBytesAsync()
    {
        var mb = int.TryParse(await _db.SettingAsync("DM_Settings", "FLD_MaxFileSizeMB"), out var m) && m > 0 ? m : 25;
        return mb * 1024L * 1024L;
    }

    private static string? Roles(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return null;
        var list = string.Join(",", csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase));
        return list.Length == 0 ? null : list.Length <= 500 ? list : list[..500];
    }

    private static string? Cut(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Length <= max ? s.Trim() : s.Trim()[..max];
}