using DMS.Application.DTO.Features;
using DMS.Application.DTO.OtherDocument;

namespace DMS.Application.DTO.OtherDocument
{
    // ---- Folders of Other Documents (FLD_Folder; documents are OtherDocumentMaster rows with FLD_FolderID) ----

    public class FolderItem
    {
        public long FLD_FolderID { get; set; }
        public int VesselID { get; set; }
        public string? VesselName { get; set; }
        public long? ParentID { get; set; }
        public string FolderName { get; set; } = "";
        public string Scope { get; set; } = "";
        public string? OwnerUserID { get; set; }
        public string? ViewRoles { get; set; }
        public string? EditRoles { get; set; }
        /// <summary>The Other Documents filed in this folder.</summary>
        public int FileCount { get; set; }
        public bool CanEdit { get; set; }
    }

    public sealed record SaveFolder(long? ParentId, string Name, string Scope, int? VesselId, string? ViewRoles, string? EditRoles);

    /// <summary>An Other Document as shown in a folder (OtherDocumentMaster.FLD_FolderID).</summary>
    public class FolderDocument
    {
        public long OtherDocumentMasterID { get; set; }
        public int VesselID { get; set; }
        /// <summary>For an office-only document (VesselID -2): the vessel it is about.</summary>
        public int? RelatedVesselID { get; set; }
        public string? VesselName { get; set; }
        public long? FLD_FolderID { get; set; }
        public string? FolderName { get; set; }
        /// <summary>Added at the office and kept there: never sent to a vessel.</summary>
        public bool IsOfficeOnly => VesselID == -2;
        public long? OtherDocument_TypeID { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public string? DocumentTitle { get; set; }
        public string? Status { get; set; }
        public string? Reference { get; set; }
        public string? Comments { get; set; }
        public DateTime? UploadedOn { get; set; }
        public string? UploadedBy { get; set; }
        public string? Origin { get; set; }
        public int FileCount { get; set; }
        public long? FirstFileID { get; set; }
        public bool IsCertificate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>A new document added straight into a folder. VesselId only for a company folder of all vessels.</summary>
    public sealed record NewFolderDocument(long TypeId, string DocumentNumber, string Title, string? Status, string? Reference,
        string? Comments, int? VesselId, List<UploadedFile> Files);

    public sealed record MoveDocument(long? FolderId);
}

namespace DMS.Application.Interface
{
    public interface IOtherDocumentService
    {
        Task<List<OtherDocument_Type>> GetDocumentTypesAsync();

        Task<List<Vw_OtherDocument_MasterBlob>> GetRecentOtherDocumentsAsync(int limit = 10);

        Task<List<Vw_OtherDocument_MasterBlob>> GetDocumentsAsync(int? vesselId = null, string? documentType = null);

        Task<Vw_OtherDocument_MasterBlob?> GetOtherDocumentDetailsAsync(long otherDocumentMasterId);

        Task<long> InsertOtherDocumentRecordAsync();

        Task RemoveTempOtherDocumentAsync(long otherDocumentMasterId);

        Task<long> AddOtherDocumentAsync(CreateOtherDocumentDto model);

        Task<string> GetNextDocumentNumberAsync(long typeId);

        // ---- Folders ----
        Task<List<FolderItem>> GetFolderTreeAsync(OfficeUser user);
        Task<(long? Id, string? Error)> SaveFolderAsync(long? folderId, SaveFolder request, OfficeUser user);
        Task<string?> DeleteFolderAsync(long folderId, OfficeUser user);

        // ---- Documents in folders ----
        Task<List<FolderDocument>> GetFolderDocumentsAsync(long? folderId, int? vesselId, OfficeUser user);
        Task<List<FolderDocument>> GetVesselDocumentsAsync(int vesselId, OfficeUser user);
        Task<(long? Id, string? Error)> CreateDocumentAsync(long? folderId, NewFolderDocument request, OfficeUser user);
        Task<string?> MoveDocumentAsync(long documentId, long? folderId, OfficeUser user);
        Task<string?> DeleteDocumentAsync(long documentId, OfficeUser user);
        Task<List<FileItem>> GetDocumentFilesAsync(long documentId);
    }
}
