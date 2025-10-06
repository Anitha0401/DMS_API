using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.DTO
{
    [Keyless]
    public class ManualVersionDetailsDto
    {
        public long DM_ManualVersionID { get; set; }

        public required string Version { get; set; }
    }
}
