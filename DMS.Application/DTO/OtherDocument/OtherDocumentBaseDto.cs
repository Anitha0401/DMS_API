namespace DMS.Application.DTO.OtherDocument;

public class OtherDocumentBaseDto
{
    public required long CIR_MasterID { get; set; }

    public required long CIR_BlobID { get; set; }

    public string CircularType { get; set; } = string.Empty;
}
