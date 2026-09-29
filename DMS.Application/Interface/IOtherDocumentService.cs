using DMS.Application.DTO.OtherDocument;

namespace DMS.Application.Interface;

public interface IOtherDocumentService
{
    Task<List<OtherDocument_Type>> GetDocumentTypesAsync();

    Task<List<Vw_OtherDocument_MasterBlob>> GetRecentOtherDocumentsAsync(int limit = 10);

    Task<List<Vw_OtherDocument_MasterBlob>> GetDocumentsAsync(int? vesselId = null, string? documentType = null);

    Task<Vw_OtherDocument_MasterBlob?> GetOtherDocumentDetailsAsync(long otherDocumentMasterId);

    Task<long> InsertOtherDocumentRecordAsync();

    Task RemoveTempOtherDocumentAsync(long otherDocumentMasterId);

    Task<long> AddOtherDocumentAsync(CreateOtherDocumentDto model);

    Task<string> GetNextDocumentNumberAsync(long typeId);
}