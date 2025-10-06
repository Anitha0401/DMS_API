using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.DTO
{
    public class VslRankListDto
    {
        public string userRank { get; set; } = null!;   

        public string userDeptDisplay { get; set; } = null!;

        public string userDept { get; set; } = null!;
    }
}
