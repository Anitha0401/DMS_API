using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Circulars;

[Table("CIR_Setting")]
public partial class CIR_Settings
{
    [Key]
    [Column("DM_SettingsID")]
    public long DmSettingsId { get; set; }

    [StringLength(5)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [StringLength(250)]
    [Unicode(false)]
    public string SettingName { get; set; } = null!;

    [StringLength(250)]
    [Unicode(false)]
    public string? SettingValue { get; set; }
}
