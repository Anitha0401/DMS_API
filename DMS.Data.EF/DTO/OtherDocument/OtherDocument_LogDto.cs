using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMS.Data.EF.DTO.OtherDocument;

public partial class OtherDocument_LogDto
{
    public long CIR_OtherDocument_LogID { get; set; }

    public long CIR_OtherDocumentID { get; set; }

    public DateTime? Logdate { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Activity { get; set; }

    public string? Remarks { get; set; }
}
