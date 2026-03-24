using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.OtherDocument;

namespace DMS.Application.Interface;

public interface IOtherDocumentService
{
    Task<List<CIR_OtherDocument_Type>> GetTypesAsync(bool includeInactive = false);
    Task<List<DMS.Data.EF.Models.OtherDocument.Vw_CIR_OtherDocument>> GetRecentOtherDocumentsAsync(int limit = 10);
    Task<DMS.Data.EF.Models.OtherDocument.Vw_CIR_OtherDocument?> GetOtherDocumentDetailsAsync(long otherDocumentId);
    Task<long> InsertOtherDocumentRecordAsync();
    Task RemoveTempOtherDocumentAsync(long otherDocumentId);
    Task<long> AddOtherDocumentAsync(OtherDocumentDto model);
}