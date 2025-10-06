using Microsoft.EntityFrameworkCore;
using DMS.Application.Interface;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;
using DMS.Data.EF.DTO;

namespace DMS.Application;

public class VesselService : IVesselService
{
    private readonly DmsContext _dbContext;
    private readonly IMediator _mediator;
    private readonly GeneralQueries _queries;

    public VesselService(DmsContext context, IMediator mediator, GeneralQueries queries)
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
}
