using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models;

[Table("HQ_Users")]
public partial class HqUser
{
    [Key]
    [Column("HQ_UsersID")]
    public long HqUsersId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Column("VesselID")]
    public int VesselID { get; set; }

    [Column("HQDept")]
    [StringLength(25)]
    [Unicode(false)]
    public string? HQDept { get; set; }

    [Column("UserID")]
    [StringLength(50)]
    [Unicode(false)]
    public string UserId { get; set; } = null!;

    [StringLength(250)]
    [Unicode(false)]
    public string UserName { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string Role { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string? Designation { get; set; }

    [Column("EMail")]
    [StringLength(250)]
    [Unicode(false)]
    public string? Email { get; set; }

    [Column("EmpID")]
    [StringLength(25)]
    [Unicode(false)]
    public string? EmpId { get; set; }

    [StringLength(250)]
    [Unicode(false)]
    public string? PhoneNo { get; set; }

    public int? IsDisabled { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DateAdded { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DateDisabled { get; set; }

    /// <summary>ASP.NET Core PasswordHasher hash. Added by Database/001_HQ_Users_PasswordHash.sql.</summary>
    [StringLength(200)]
    public string? PasswordHash { get; set; }
}
