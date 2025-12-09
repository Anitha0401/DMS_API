using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models;

[PrimaryKey("VslStaffId", "VesselID")]
[Table("Vsl_Staff")]
public partial class VslStaff
{
    [Key]
    [Column("Vsl_StaffID")]
    public long VslStaffId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Key]
    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("UserID")]
    [StringLength(50)]
    [Unicode(false)]
    public string UserId { get; set; } = null!;

    [StringLength(250)]
    [Unicode(false)]
    public string UserName { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string UserRank { get; set; } = null!;

    [StringLength(25)]
    [Unicode(false)]
    public string? UserDept { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? JoiningDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LeavingDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Password { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? SortOrder { get; set; }

    public int? IsDisabled { get; set; }
}
