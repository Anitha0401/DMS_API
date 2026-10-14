namespace DMS.Application.DTO.OtherDocument;

public partial class OtherDocumentDto
{
    public long OtherDocumentMasterID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long? OtherDocument_TypeID { get; set; }

    public string DocumentNumber { get; set; } = null!;

    public string DocumentTitle { get; set; } = null!;

    public string? Status { get; set; }

    public string? Reference { get; set; }

    public string? Comments { get; set; }

    public DateTime? UploadedOn { get; set; }

    public string? UploadedBy { get; set; }

    public int? IsDeleted { get; set; }

    public string? FileName { get; set; }

    public string? FileType { get; set; }

    public string? OriginalFileType { get; set; }

    public int BlobSize { get; set; }

    public byte[]? BlobContents { get; set; }
}