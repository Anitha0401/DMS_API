using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Data.EF.Models;

public class ManualUserAckList
{
    public long DM_ManualID { get; set; }

    public long DM_ManualVersionID { get; set; }

    public string VslCode { get; set; } = null!;

    public string? VslName { get; set; }

    public string UserId { get; set; } = null!;

    public string? UserName { get; set; }
    
    public string? UserRank { get; set; }

    public DateTime DateRead{ get; set; }

    public string? Comments { get; set; }

    public int? IsActive { get; set; }
}
