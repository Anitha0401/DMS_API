using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Circulars;

[Table("CIR_UserFavourite")]
public partial class CIR_UserFavourite
{
    [Key]
    public long CIR_UserFavouriteID { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long DM_ManualID { get; set; }

    [StringLength(50)]
    public string UserId { get; set; } = null!;

    public int? IsDisabled { get; set; }
}
