using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models;

[Table("Gen_Error_HQ_Log")]
public partial class GenErrorHqLog
{
    [Key]
    [Column("Gen_Error_HQ_LogID")]
    public long GenErrorHqLogId { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

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
