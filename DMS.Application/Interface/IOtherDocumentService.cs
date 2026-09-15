using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.OtherDocument;

namespace DMS.Application.Interface;

public interface IOtherDocumentService
{
    Task<List<OtherDocument_Type>> GetDocumentTypesAsync(bool includeInactive = false);

    Task<List<Vw_OtherDocument_MasterBlob>> GetRecentOtherDocumentsAsync(int limit = 10);

    Task<Vw_OtherDocument_MasterBlob?> GetOtherDocumentDetailsAsync(long otherDocumentMasterId);

    Task<long> InsertOtherDocumentRecordAsync();

    Task RemoveTempOtherDocumentAsync(long otherDocumentMasterId);

    Task<long> AddOtherDocumentAsync(CreateOtherDocumentDto model);
}