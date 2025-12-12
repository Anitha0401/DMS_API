using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models.Circulars;

[Keyless]
public partial class VwCIRMasterBlob
{
    public long CIR_MasterID { get; set; }

    public long? CIR_BlobID { get; set; }

    public string Master_Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string Blob_Origin { get; set; } = null!;

    public int Blob_VesselID { get; set; }

    public string CircularType { get; set; } = null!;

    public long? CIR_CategoryID { get; set; }

    public string Category { get; set; } = null!;

    public string CIR_Number { get; set; } = null!;

    public string? Status { get; set; }

    public string? Title { get; set; }

    public string? Priority { get; set; }

    public string? Reference { get; set; }

    public DateTime? DateIssued { get; set; }

    public DateTime? ReleasedDate { get; set; }

    public string? Remarks { get; set; }

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

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? IsDeleted { get; set; }
}
