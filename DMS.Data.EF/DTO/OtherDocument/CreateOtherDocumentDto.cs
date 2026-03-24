namespace DMS.Data.EF.DTO.OtherDocument;

public class CreateOtherDocumentDto
{
    public long CIR_OtherDocumentID { get; set; }

    public long CIR_OtherDocument_TypeID { get; set; }

    public string? DocumentNumber { get; set; }

    public string? Title { get; set; }

    public string? DocumentTitle { get; set; }

    public string? Status { get; set; }

    public string? Reference { get; set; }

    public string? Comments { get; set; }

    public DateTime? UploadedOn { get; set; }
    
    public string? UploadedBy { get; set; }

    public string Description { get; set; } = null!;
    
    public string? FileName { get; set; }
    
    public string? FileType { get; set; }
    
    public string? OriginalFileType { get; set; }
    
    public int BlobSize { get; set; }
    
    public byte[]? BlobContents { get; set; }

    public string CalledMode { get; set; } = string.Empty;
}
