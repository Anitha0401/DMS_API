using Microsoft.EntityFrameworkCore;
using DMS.Application.Interface;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;
using DMS.Data.EF.DTO;

namespace DMS.Application;

public class HQUserService : IHQUserService
{
    private readonly GeneralContext _dbContext;
    private readonly IMediator _mediator;

    public HQUserService(GeneralContext context, IMediator mediator)
    {
        _dbContext = context;
        _mediator = mediator;
    }

    public async Task<List<string>> GetHQDeptList()
    {
        var result = await _dbContext.HqUserRoles
                                .Select(x => x.Hqdept)
                                .Where(dept => !string.IsNullOrEmpty(dept))
                                .Distinct()
                                .ToListAsync();
        return result;
    }

    public async Task<List<string>> GetHQUserRoles()
    {
        var result = await _dbContext.HqUserRoles
                               .Where(x => !string.IsNullOrEmpty(x.UserRole))
                               .OrderBy(x => x.ApprovalOrder)
                               .Select(x => x.UserRole)
                               .Distinct()
                               .ToListAsync();
        return result;
    }
}