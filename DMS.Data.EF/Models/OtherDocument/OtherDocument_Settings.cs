using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.OtherDocument;

[Table("OtherDocument_Settings")]
public partial class OtherDocument_Settings
{
    [Key]
    [Column("OtherDocument_SettingsID")]
    public long OtherDocument_SettingsID { get; set; }

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
