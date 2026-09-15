namespace DMS.Data.EF.DTO.Circulars;
public class CircularUserAckDto
{
    public long CIR_UserAckID { get; set; }
    
    public int VesselID { get; set; }
    
    public string VslCode { get; set; } = string.Empty;
    
    public string? VslName { get; set; }
    
    public string? AcknowledgedBy { get; set; }

    public string? DateRead { get; set; }
    
    public string? Status { get; set; }

    public string? Comments { get; set; }
}