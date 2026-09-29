namespace DMS.Application.DTO.Manuals;

public class ManualBaseDto
{
    public required long DM_ManualID { get; set; }

    public required long DM_ManualVersionID { get; set; }

    public string ManualName { get; set; } = string.Empty;

    public string UserID { get; set; } = string.Empty;

    public string UserRole { get; set; } = string.Empty;

    public string Comments { get; set; } = string.Empty;
}
