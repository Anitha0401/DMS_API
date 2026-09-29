namespace DMS.Application.DTO.OtherDocument;

public partial class OtherDocument_LogDto
{
    public long OtherDocument_LogID { get; set; }

    public long OtherDocumentMasterID { get; set; }

    public DateTime? Logdate { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Activity { get; set; }

    public string? Remarks { get; set; }
}
