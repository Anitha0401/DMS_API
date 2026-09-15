using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.Models.OtherDocument;

[Table("OtherDocument_UserFavourite")]
public partial class OtherDocument_UserFavourite
{
    [Key]
    public long OtherDocument_UserFavouriteID { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public long OtherDocumentMasterID { get; set; }

    [StringLength(50)]
    public string UserId { get; set; } = null!;

    public int? IsDisabled { get; set; }
}
