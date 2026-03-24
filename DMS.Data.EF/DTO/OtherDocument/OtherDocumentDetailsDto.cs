namespace DMS.Data.EF.DTO.OtherDocument;

public partial class OtherDocumentDetailsDto
{
    public long CIR_OtherDocumentID { get; set; }

    public long? CIR_OtherDocument_BlobID { get; set; }

    public string Master_Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string Blob_Origin { get; set; } = null!;

    public int Blob_VesselID { get; set; }

    public string DocumentType { get; set; } = null!;

    public long? CIR_OtherDocument_TypeID { get; set; }

    public string DocumentNumber { get; set; } = null!;

    public string? Status { get; set; }

    public string? DocumentTitle { get; set; }

    public string? Reference { get; set; }

    public string? Comments { get; set; }

    public DateTime? UploadedOn { get; set; }

    public string? UploadedBy { get; set; }

    public int? BlobSize { get; set; }

    public byte[]? BlobContents { get; set; }

    public string? FileName { get; set; }

    public string? FileType { get; set; }

    public string? OriginalFileType { get; set; }

    public string? Description { get; set; }

    public string? Cloud_ContainerName { get; set; }

    public string? Cloud_Directory { get; set; }

    public string? HQ_DatabaseName { get; set; }

    public string? HQ_Local_Folder { get; set; }

    public string? HQ_Storage_Option { get; set; }

    public string? VSL_DatabaseName { get; set; }

    public string? VSL_Local_Folder { get; set; }

    public string? VSL_Storage_Option { get; set; }

    public int? IsDeleted { get; set; }
}
