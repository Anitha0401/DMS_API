using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.DTO
{
    public class AssignManualRightsDto
    {
        public List<VesselListDto> VesselList { get; set; } = null!;
     
        public List<VslRankListDto> VslRankList { get; set; } = null!;

        public List<HQUserListDto> HqUserList { get; set; } = null!;
    }
}
