using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Circulars;

[Keyless]
public partial class VwCIRVesselAck
{
    public long CIR_UserAckID { get; set; }

    public long? CIR_MasterID { get; set; }

    public int VesselID { get; set; }

    public long? Fleet_CrewID { get; set; }

    public string? VslCode { get; set; }

    public string? VslName { get; set; }

    public DateTime? DateRead { get; set; }

    public string? Comments { get; set; }

    public int? IsActive { get; set; }

    public string? UserName { get; set; }

    public string? UserRank { get; set; }
}
