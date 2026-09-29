using Microsoft.EntityFrameworkCore;

namespace DMS.Application.DTO.Manuals
{
    [Keyless]
    public class ManualVersionDetailsDto
    {
        public long DM_ManualVersionID { get; set; }

        public required string Version { get; set; }
    }
}
