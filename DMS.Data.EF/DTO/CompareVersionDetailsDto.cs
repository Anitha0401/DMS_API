using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.DTO
{
    public class CompareVersionDetailsDto
    {
        public long current_DmManualVersionID { get; set; }

        public string? current_Version { get; set; }

        public string? current_ManualContent { get; set; }

        public long compare_DmManualVersionID { get; set; }

        public string? compare_Version { get; set; }

        public string? compare_ManualContent { get; set; }
    }
}
