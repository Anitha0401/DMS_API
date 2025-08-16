using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.DTO
{
    public class ManualCategoryDto
    {
        public long DM_CategoryID { get; set; }

        public string CategoryName { get; set; } = null!;
    }
}
