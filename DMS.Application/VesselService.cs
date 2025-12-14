using DMS.Application.Interface;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO;
using DMS.Data.EF.Query.Manuals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Application;

public class VesselService : IVesselService
{
    private readonly GeneralContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ManualGeneralQueries _queries;

    public VesselService(GeneralContext context, IMediator mediator, ManualGeneralQueries queries)
    {
        _dbContext = context;
        _mediator = mediator;
        _queries = queries;
    }

    public async Task<List<string>> GetVslTypeList()
    {
        var vslTypeList = await _dbContext.VVessels
                                .Select(x => x.VslType)
                                .Where(vslType => !string.IsNullOrEmpty(vslType))
                                .Distinct()
                                .ToListAsync();

        // Ensure null values are filtered out to match the interface signature
        return vslTypeList.Where(vslType => vslType != null).Cast<string>().ToList();
    }

    public async Task<List<VesselListDto>> GetVesselList()
    {
        var vesselList = await _dbContext.VVessels
                                      .Select(x => new VesselListDto
                                      {
                                          VesselID = x.VesselID,
                                          VslName = x.VslName,
                                          VslCode = x.VslCode,
                                          VslType = x.VslType
                                      }).ToListAsync();
        return vesselList;
    }
}
