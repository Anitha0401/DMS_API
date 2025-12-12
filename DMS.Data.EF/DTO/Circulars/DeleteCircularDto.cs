namespace DMS.Data.EF.DTO.Circulars;

public class DeleteCircularDto
{
    public required long DM_ManualID { get; set; }

    public string UserID { get; set; } = string.Empty;
    
    public string UserRole { get; set; } = string.Empty;
    
    public string Comments { get; set; } = string.Empty;
}
