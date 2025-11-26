using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.Models;

public class Manual_FilterDto
{
    public long Key { get; set; }

    public string Label { get; set; } = null!;

    [NotMapped]
    public TreeviewDataDto data { get; set; } = null!;
}
