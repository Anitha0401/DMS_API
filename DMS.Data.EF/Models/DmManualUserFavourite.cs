using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Table("DM_ManualUserFavourite")]
public partial class DmManualUserFavourite
{
    [Key]
    public long DM_ManualUserFavouriteID { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long DM_ManualID { get; set; }

    [StringLength(50)]
    public string UserId { get; set; } = null!;

    public int? IsDisabled { get; set; }
}
