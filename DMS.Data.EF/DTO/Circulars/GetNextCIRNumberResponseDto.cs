namespace DMS.Data.EF.DTO.Circulars;

public class GetNextCIRNumberResponseDto
{
    public string CirNumber { get; set; } = string.Empty;
    
    public string AllowedUserToAck { get; set; }
}