using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models;

[PrimaryKey("GenErrorVslLogId", "VesselID")]
[Table("Gen_Error_VSL_Log")]
public partial class GenErrorVslLog
{
    [Key]
    [Column("Gen_Error_VSL_LogID")]
    public long GenErrorVslLogId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    [Key]
    [Column("VesselID")]
    public int VesselID { get; set; }

    [StringLength(80)]
    [Unicode(false)]
    public string ApplicationName { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string PageName { get; set; } = null!;

    [StringLength(150)]
    [Unicode(false)]
    public string FunctionName { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? TextContents { get; set; }

    [Column("LoggedInUserID")]
    [StringLength(50)]
    [Unicode(false)]
    public string LoggedInUserId { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime AddedOn { get; set; }
}
