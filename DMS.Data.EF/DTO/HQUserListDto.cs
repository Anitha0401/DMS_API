using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.DTO
{
    public class HQUserListDto
    {
        public string HQDept { get; set; } = null!;   

        public string UserRole { get; set; } = null!;

        public string RoleDescription { get; set; } = null!;
    }
}
