namespace DMS.Application.DTO.OtherDocument;

public class DeleteOtherDocumentDto
{
    public required long DM_ManualID { get; set; }

    public string UserID { get; set; } = string.Empty;
    
    public string UserRole { get; set; } = string.Empty;
    
    public string Comments { get; set; } = string.Empty;
}
