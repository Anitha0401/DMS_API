namespace DMS.Application.DTO.Circulars;

public class CircularsBaseDto
{
    public required long CIR_MasterID { get; set; }

    public required long CIR_BlobID { get; set; }

    public string CircularType { get; set; } = string.Empty;
}
