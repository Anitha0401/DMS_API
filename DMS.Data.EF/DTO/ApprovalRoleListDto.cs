using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.DTO
{
    public class ApprovalDetailsDto
    {
        public int FlowID { get; set; }

        public string ApprovalFlow { get; set; } = null!;
    }
}
