using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.Manuals;

[Table("DM_Settings")]
public partial class DmSettings
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
