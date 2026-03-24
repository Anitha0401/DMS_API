using DMS.Application.Interface;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.OtherDocument;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMS.Application;

public class OtherDocumentService : IOtherDocumentService
{
    private readonly OtherDocumentsContext _dbContext;
    private readonly ILogger<OtherDocumentService> _logger;

    public OtherDocumentService(OtherDocumentsContext context, ILogger<OtherDocumentService> logger)
    {
        _dbContext = context;
        _logger = logger;
    }

    public async Task<List<CIR_OtherDocument_Type>> GetTypesAsync(bool includeInactive = false)
    {
        var query = _dbContext.CIR_OtherDocument_Type.AsQueryable();

        if (!includeInactive)
            query = query.Where(t => (t.IsActive ?? 1) == 1);

        return await query
            .OrderBy(t => t.DocumentType)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Vw_CIR_OtherDocument>> GetRecentOtherDocumentsAsync(int limit = 10)
    {
        return await _dbContext.Vw_CIR_OtherDocument
            .OrderByDescending(v => v.UploadedOn)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Vw_CIR_OtherDocument?> GetOtherDocumentDetailsAsync(long otherDocumentId)
    {
        return await _dbContext.Vw_CIR_OtherDocument
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.CIR_OtherDocumentID == otherDocumentId);
    }

    public async Task<long> InsertOtherDocumentRecordAsync()
    {
        try
        {
            var nextId = (_dbContext.CIR_OtherDocument
                             .Select(m => (long?)m.CIR_OtherDocumentID)
                             .Max() ?? 0) + 1;

            var entity = new OtherDocumentDto
            {
                CIR_OtherDocumentID = nextId,
                DocumentTitle = string.Empty,
                DocumentNumber = string.Empty,
                VesselID = -1,
                UploadedOn = DateTime.UtcNow
            };

            // _dbContext.CIR_OtherDocument.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity.CIR_OtherDocumentID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert CIR_OtherDocument record.");
            throw;
        }
    }

    public async Task RemoveTempOtherDocumentAsync(long otherDocumentId)
    {
        try
        {
            var entity = await _dbContext.CIR_OtherDocument
                            .Where(e => e.CIR_OtherDocumentID == otherDocumentId)
                            .FirstOrDefaultAsync();

            if (entity != null)
            {
                _dbContext.CIR_OtherDocument.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove CIR_OtherDocument record with id {OtherDocumentId}", otherDocumentId);
            // swallow intentionally to match existing pattern, rethrow if you prefer
        }
    }

    public async Task<long> AddOtherDocumentAsync(OtherDocumentDto model)
    {
        if (model is null)
            throw new ArgumentNullException(nameof(model));

        try
        {
            if (model.CIR_OtherDocumentID == 0)
            {
                model.CIR_OtherDocumentID = (_dbContext.CIR_OtherDocument
                                                .Select(m => (long?)m.CIR_OtherDocumentID)
                                                .Max() ?? 0) + 1;
            }

            model.UploadedOn ??= DateTime.UtcNow;

            //_dbContext.CIR_OtherDocument.Add(model);
            await _dbContext.SaveChangesAsync();

            return model.CIR_OtherDocumentID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add CIR_OtherDocument.");
            throw;
        }
    }
}