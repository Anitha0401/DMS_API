namespace DMS.Application.DTO.Circulars;

public class CIR_LogDto
{
    public long CIR_LogID { get; set; }

    public long? CIR_MasterID { get; set; }

    public DateTime? Logdate { get; set; }
    
    public string? UserId { get; set; }
    
    public string? UserName { get; set; }
    
    public string? Activity { get; set; }
    
    public string? Remarks { get; set; }
}