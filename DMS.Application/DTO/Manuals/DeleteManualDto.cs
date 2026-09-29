namespace DMS.Application.DTO.Manuals;

public class DeleteManualDto
{
    public required long DM_ManualID { get; set; }

    public string UserID { get; set; } = string.Empty;
    
    public string UserRole { get; set; } = string.Empty;
    
    public string Comments { get; set; } = string.Empty;
}
