using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.DTO
{
    public class ApprovalFlowListDto
    {
        public int FlowID { get; set; }

        public string FlowDesc { get; set; } = null!;
    }
}
