namespace DMS.Data.EF.DTO.Circulars;

public class EditCircularDto : CircularsBaseDto
{
    public required long CIR_CategoryID { get; set; }

    public string? CIR_Number { get; set; }

    public string AllowedUserToAck { get; set; } = string.Empty;
   
    public string Reference { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    
    public int Priority { get; set; }

    public DateTime? DateIssued { get; set; }

    public DateTime? ReleasedDate { get; set; }

    public string? Remarks { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public string OriginalFileType { get; set; } = string.Empty;

    public int BlobSize { get; set; }

    public byte[]? BlobContents { get; set; }

    public string CalledMode { get; set; } = string.Empty;
}
