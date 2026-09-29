using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models;

[Table("HQ_UserActionVsls")]
public partial class HqUserActionVsl
{
    [Key]
    [Column("HQ_UserActionVslsID")]
    public long HqUserActionVslsId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("UserID")]
    [StringLength(50)]
    [Unicode(false)]
    public string UserId { get; set; } = null!;

    public int? IsActionVsls { get; set; }

    public int? IsPrimaryVsl { get; set; }

    public int? IsSecdonaryVsl { get; set; }
}
