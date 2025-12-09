namespace DMS.Data.EF.DTO;

public class VesselListDto
{
    public int VesselID { get; set; }

    public string VslName { get; set; } = null!;
    
    public string VslCode { get; set; } = null!;

    public string? VslType { get; set; } = null!;
}
