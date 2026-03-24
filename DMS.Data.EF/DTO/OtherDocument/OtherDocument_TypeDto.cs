namespace DMS.Data.EF.DTO.OtherDocument;

public partial class OtherDocument_TypeDto
{
    public int CIR_OtherDocument_TypeID { get; set; }

    public string DocumentCode { get; set; } = null!;

    public string? DocumentType { get; set; }

    public int SortOrder { get; set; }
}