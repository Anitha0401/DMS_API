namespace DMS.Application.DTO.Circulars;

public class CreateCircularDto
{
    public long CIR_MasterID { get; set; }

    public string CircularType { get; set; } = null!;

    public long CIR_CategoryID { get; set; }

    public string? CIR_Number { get; set; }

    public string? AllowedUserToAck { get; set; }

    public string? Reference { get; set; }

    public string? Title { get; set; }

    public string? Status { get; set; }

    public int? Priority { get; set; }

    public DateTime? DateIssued { get; set; }
    
    public DateTime? ReleasedDate { get; set; }

    public string? Remarks { get; set; }

    public string? CreatedBy { get; set; }
    
    public DateTime? CreatedDate { get; set; }

    public string Description { get; set; } = null!;
    
    public string? FileName { get; set; }
    
    public string? FileType { get; set; }
    
    public string? OriginalFileType { get; set; }
    
    public int BlobSize { get; set; }
    
    public byte[]? BlobContents { get; set; }

    public string CalledMode { get; set; } = string.Empty;

    public List<int> VesselIdList { get; set; } = new List<int>();
}
