using DMS.Application.Interface;
using DMS.Data.EF.Context;
using DMS.Application.DTO.Circulars;
using DMS.Application.DTO.OtherDocument;
using DMS.Data.EF.Models.OtherDocument;
using DMS.Data.EF.Query.Circulars;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMS.Application;

public class OtherDocumentService : IOtherDocumentService
{
    private readonly OtherDocumentsContext _dbContext;
    private readonly ILogger<OtherDocumentService> _logger;
    private readonly IMediator _mediator;

    public OtherDocumentService(OtherDocumentsContext context, ILogger<OtherDocumentService> logger, IMediator mediator)
    {
        _dbContext = context;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<List<OtherDocument_Type>> GetDocumentTypesAsync()
    {
        var query = _dbContext.OtherDocument_Type.AsQueryable();

        return await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.DocumentType)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Vw_OtherDocument_MasterBlob>> GetRecentOtherDocumentsAsync(int limit = 10)
    {
        return await _dbContext.Vw_OtherDocument
            .OrderByDescending(v => v.UploadedOn)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Vw_OtherDocument_MasterBlob>> GetDocumentsAsync(int? vesselId = null, string? documentType = null)
    {
        var query = _dbContext.Vw_OtherDocument.AsQueryable();

        if (vesselId.HasValue)
        {
            query = query.Where(v => v.VesselID == vesselId.Value || (vesselId.Value == 0 && v.VesselID == 0));
        }

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            query = query.Where(v => v.DocumentType == documentType.Trim());
        }

        return await query
            .OrderByDescending(v => v.UploadedOn)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Vw_OtherDocument_MasterBlob?> GetOtherDocumentDetailsAsync(long otherDocumentMasterID)
    {
        return await _dbContext.Vw_OtherDocument
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.OtherDocumentMasterID == otherDocumentMasterID);
    }

    public async Task<long> InsertOtherDocumentRecordAsync()
    {
        try
        {
            var nextId = (_dbContext.OtherDocumentMaster
                             .Select(m => (long?)m.OtherDocumentMasterID)
                             .Max() ?? 0) + 1;

            var entity = new OtherDocumentDto
            {
                OtherDocumentMasterID = nextId,
                DocumentTitle = string.Empty,
                DocumentNumber = string.Empty,
                VesselID = -1,
                UploadedOn = DateTime.UtcNow
            };

            // _dbContext.OtherDocument.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity.OtherDocumentMasterID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert OtherDocument record.");
            throw;
        }
    }

    public async Task<long> AddOtherDocumentAsync(CreateOtherDocumentDto model)
    {
        if (model is null)
            throw new ArgumentNullException(nameof(model));

        var result = await _mediator.Send(new CreateOtherDocumentCommand(model));
        return result;
    }

    public async Task RemoveTempOtherDocumentAsync(long otherDocumentMasterID)
    {
        try
        {
            var entity = await _dbContext.OtherDocumentMaster
                            .Where(e => e.OtherDocumentMasterID == otherDocumentMasterID)
                            .FirstOrDefaultAsync();

            if (entity != null)
            {
                _dbContext.OtherDocumentMaster.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove OtherDocument record with id {OtherDocumentMasterID}", otherDocumentMasterID);
        }
    }

    public async Task<string> GetNextDocumentNumberAsync(long typeId)
    {
        var nextDocumentNumber = "0.0";
        var maxDocumentNumber = await _dbContext.OtherDocumentMaster
                             .Where(x => x.OtherDocument_TypeID == typeId)
                             .OrderByDescending(x => x.DocumentNumber)
                             .Select(m => m.DocumentNumber)
                             .FirstOrDefaultAsync();
        if (maxDocumentNumber != null)
        {
            string[] parts = maxDocumentNumber.Split('.');
            int major = int.Parse(parts[0]) ;
            nextDocumentNumber = $"{major}.0";
        }
        else
        {
            nextDocumentNumber = $"1.0";
        }

        var docType = await _dbContext.Set<OtherDocument_Type>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.OtherDocument_TypeID == typeId);

        if (docType == null) return "";

        var prefix = (docType.DocumentCode ?? string.Empty).Trim();
        nextDocumentNumber = $"{prefix}{nextDocumentNumber}";

        return nextDocumentNumber;
    }
}