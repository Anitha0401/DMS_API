using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.DTO.Manuals
{
    [Keyless]
    public class ManualVersionDetailsDto
    {
        public long DM_ManualVersionID { get; set; }

        public required string Version { get; set; }
    }
}
