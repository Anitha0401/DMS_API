using DMS.Application.DTO.Features;

namespace DMS.Application.DTO.Features
{
    // ---- Who is asking (from the login token; never from the request) ----

    public sealed record OfficeUser(string UserId, string UserName, string Role, string? Dept);

    // ---- 1. Pending acknowledgements ----

    public class AckPendingRow
    {
        public int VesselID { get; set; }               // -1 for office users
        public string? VesselName { get; set; }
        public string ItemType { get; set; } = "";      // Manual | Circular | Alert
        public long ItemID { get; set; }
        public string? Number { get; set; }
        public string? Title { get; set; }
        public string? Version { get; set; }
        public string? Rank { get; set; }               // vessel rank, or office role
        public string? UserID { get; set; }
        public string? UserName { get; set; }
        public string? Dept { get; set; }
        public DateTime? ReleasedOn { get; set; }
        public DateTime? DueOn { get; set; }
        public int AckDueDays { get; set; }
        public bool Overdue => DueOn.HasValue && DueOn.Value < DateTime.Now;
        public int DaysWaiting => ReleasedOn.HasValue ? Math.Max(0, (int)(DateTime.Now - ReleasedOn.Value).TotalDays) : 0;
    }

    public class AckVesselSummary
    {
        public int VesselID { get; set; }
        public string? VesselName { get; set; }
        public DateTime? LastSyncUtc { get; set; }
        public int ManualsPending { get; set; }
        public int ManualsOverdue { get; set; }
        public int CircularsPending { get; set; }
        public int CircularsOverdue { get; set; }
        public int AlertsPending { get; set; }
        public int AlertsOverdue { get; set; }
    }

    public class FeatureSettingsDto
    {
        public string? AllowedRolesToViewAllRankPendingItems { get; set; }
        public int DefaultAckDueDaysManual { get; set; }
        public int DefaultAckDueDaysCircular { get; set; }
        public int DefaultAckDueDaysAlert { get; set; }
        public int FolderMaxFileSizeMB { get; set; }
        public int FolderMaxFiles { get; set; }
    }

    // ---- 2. Inbox ----

    public class InboxItem
    {
        public long DMS_InboxID { get; set; }
        public int VesselID { get; set; }
        public string ItemType { get; set; } = "";
        public long ItemID { get; set; }
        public long? VersionID { get; set; }
        public string Action { get; set; } = "";
        public string? Title { get; set; }
        public int? Priority { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsRead { get; set; }
    }

    // ---- 3 / 7. Circulars: manual links, validity, cancel ----

    public class CircularManualLink
    {
        public long CIR_ManualLinkID { get; set; }
        public long CIR_MasterID { get; set; }
        public string? CircularNumber { get; set; }
        public string? CircularTitle { get; set; }
        public string? CircularType { get; set; }
        public string? CircularStatus { get; set; }
        public long DM_ManualID { get; set; }
        public string? ManualNo { get; set; }
        public string? ManualName { get; set; }
        public long? DM_ManualVersionID { get; set; }
        public string LinkType { get; set; } = "";
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ClosedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
    }

    public class CircularExtras
    {
        public long CIR_MasterID { get; set; }
        public string? Status { get; set; }
        public int? AckDueDays { get; set; }
        public DateTime? ValidUntil { get; set; }
        public long? SupersededByID { get; set; }
        public string? SupersededByNumber { get; set; }
        public bool RequiresValidUntil { get; set; }
        public List<CircularManualLink> Links { get; set; } = new();
    }

    public sealed record SaveCircularExtras(int? AckDueDays, DateTime? ValidUntil);
    public sealed record AddManualLink(long ManualId, long? ManualVersionId, string LinkType, string? Remarks);
    public sealed record CancelCircular(long? SupersededById, string? Reason);

    public class CircularCategorySetting
    {
        public long CIR_CategoryID { get; set; }
        public string? CircularType { get; set; }
        public string? CategoryCode { get; set; }
        public string? Category { get; set; }
        public bool RequiresValidUntil { get; set; }
    }

    public class ManualExtras
    {
        public long DM_ManualID { get; set; }
        public int? AckDueDays { get; set; }
    }

    // ---- 4. Suggestions ----

    public class SuggestionListItem
    {
        public long SUG_MasterID { get; set; }
        public int VesselID { get; set; }
        public string? VesselName { get; set; }
        public string? SuggestionNo { get; set; }
        public string Title { get; set; } = "";
        public string? ItemType { get; set; }
        public long? ItemID { get; set; }
        public string? ItemTitle { get; set; }
        public string Priority { get; set; } = "";
        public string Status { get; set; } = "";
        public string SubmittedBy { get; set; } = "";
        public string? SubmittedByName { get; set; }
        public string? SubmittedRank { get; set; }
        public DateTime SubmittedOn { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }

    public class SuggestionDetail : SuggestionListItem
    {
        public string? Description { get; set; }
        public string? QuotedText { get; set; }
        public string? ImplementedItemType { get; set; }
        public long? ImplementedItemID { get; set; }
        public List<SuggestionLog> Log { get; set; } = new();
        public List<FileItem> Files { get; set; } = new();
    }

    public class SuggestionLog
    {
        public long SUG_LogID { get; set; }
        public string? Status { get; set; }
        public string? Comment { get; set; }
        public string? UserID { get; set; }
        public string? UserName { get; set; }
        public DateTime LogDate { get; set; }
    }

    public sealed record NewSuggestion(string Title, string? Description, string? ItemType, long? ItemId, string? QuotedText,
        string? Priority, List<UploadedFile> Files);

    public sealed record SuggestionStatusChange(string Status, string? Comment, string? ImplementedItemType, long? ImplementedItemId);

    public class FileItem
    {
        public long Id { get; set; }
        public long? FolderID { get; set; }
        public string FileName { get; set; } = "";
        public string? FileType { get; set; }
        public int BlobSize { get; set; }
        public int Version { get; set; } = 1;
        public string? UploadedBy { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime? UploadedOn { get; set; }
    }

    public sealed record UploadedFile(string FileName, string? ContentType, byte[] Bytes);
    public sealed record FileContent(byte[] Bytes, string ContentType, string FileName);

    // ---- 6. Certificates (Other Documents whose type IsCertificate = 1) ----

    public class CertificateItem
    {
        public long OtherDocumentMasterID { get; set; }
        public int VesselID { get; set; }
        public string? VesselName { get; set; }
        public long? OtherDocument_TypeID { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public string? DocumentTitle { get; set; }
        public string? CertificateNo { get; set; }
        public string? IssuedBy { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? NextSurveyDate { get; set; }
        public long? ReplacesID { get; set; }
        public int WarningDays { get; set; }
        public string? Remarks { get; set; }
        public int FileCount { get; set; }
        public long? FirstFileID { get; set; }
        /// <summary>Valid | Expiring | Expired | NoExpiry</summary>
        public string Status => ExpiryDate == null ? "NoExpiry"
            : ExpiryDate.Value.Date < DateTime.Today ? "Expired"
            : ExpiryDate.Value.Date < DateTime.Today.AddDays(WarningDays) ? "Expiring" : "Valid";
    }

    public class CertificateType
    {
        public long OtherDocument_TypeID { get; set; }
        public string? DocumentCode { get; set; }
        public string? DocumentType { get; set; }
        public bool IsCertificate { get; set; }
        public int WarningDays { get; set; }
    }

    public sealed record SaveCertificateType(bool IsCertificate, int WarningDays);

    public sealed record NewCertificate(int VesselId, long TypeId, string Title, string? DocumentNumber, string? CertificateNo,
        string? IssuedBy, DateTime? IssueDate, DateTime? ExpiryDate, DateTime? NextSurveyDate, long? ReplacesId,
        string? Remarks, List<UploadedFile> Files);

    // ---- 8. Search ----

    public class SearchHit
    {
        public string ItemType { get; set; } = "";
        public long ItemID { get; set; }
        public string? Number { get; set; }
        public string? Title { get; set; }
        public string? Snippet { get; set; }
        public int? VesselID { get; set; }
        public DateTime? Date { get; set; }
    }
}

namespace DMS.Application.Interface
{
    public interface IAckDashboardService
    {
        Task<List<AckVesselSummary>> GetVesselSummaryAsync();
        Task<List<AckPendingRow>> GetVesselPendingAsync(int? vesselId, string? itemType, string? rank);
        Task<List<AckPendingRow>> GetOfficePendingAsync(string? userId);
        Task<FeatureSettingsDto> GetSettingsAsync();
        Task SaveSettingsAsync(FeatureSettingsDto settings);
    }

    public interface IInboxService
    {
        Task<List<InboxItem>> GetAsync(OfficeUser user, bool unreadOnly, string? itemType, string? search, int take);
        Task<int> UnreadCountAsync(OfficeUser user);
        Task MarkReadAsync(OfficeUser user, long inboxId, bool read);
        Task MarkAllReadAsync(OfficeUser user);
    }

    public interface ICircularExtrasService
    {
        Task<CircularExtras?> GetAsync(long circularId);
        Task<string?> SaveAsync(long circularId, SaveCircularExtras request);
        Task<string?> AddLinkAsync(long circularId, AddManualLink request, OfficeUser user);
        Task RemoveLinkAsync(long linkId);
        Task<string?> CancelAsync(long circularId, CancelCircular request, OfficeUser user);
        Task<List<CircularManualLink>> GetForManualAsync(long manualId);
        Task<int> CloseLinksForManualAsync(long manualId);
        Task<List<CircularCategorySetting>> GetCategoriesAsync();
        Task SaveCategoryAsync(long categoryId, bool requiresValidUntil);
        Task<ManualExtras?> GetManualAsync(long manualId);
        Task SaveManualAsync(long manualId, int? ackDueDays);
    }

    public interface ISuggestionService
    {
        Task<List<SuggestionListItem>> GetListAsync(string? status, int? vesselId, string? submittedBy);
        Task<SuggestionDetail?> GetAsync(long id);
        Task<long> CreateAsync(NewSuggestion request, OfficeUser user);
        Task<string?> ChangeStatusAsync(long id, SuggestionStatusChange request, OfficeUser user);
        Task AddCommentAsync(long id, string comment, OfficeUser user);
        Task<FileContent?> GetFileAsync(long blobId);
    }

    public interface ICertificateService
    {
        Task<List<CertificateItem>> GetListAsync(int? vesselId, string? status, bool includeReplaced);
        Task<List<CertificateType>> GetTypesAsync();
        Task SaveTypeAsync(long typeId, SaveCertificateType request);
        Task<(long? Id, string? Error)> CreateAsync(NewCertificate request, OfficeUser user);
        Task<FileContent?> GetFileAsync(long blobId);
    }

    public interface ISearchService
    {
        Task<List<SearchHit>> SearchAsync(string query, string? types, OfficeUser user);
    }
}
