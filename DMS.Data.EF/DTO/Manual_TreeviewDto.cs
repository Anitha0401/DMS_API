using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.Models;

public class Manual_TreeviewDto
{
    public long Key { get; set; }

    public string Label { get; set; } = null!;

    public List<Manual_TreeviewDto> Children { get; set; } = new List<Manual_TreeviewDto>();
}
