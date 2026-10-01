using DMS.Application.Interface;
using DMS.Data.EF.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public async Task<HqLoginUser?> GetUserForLoginAsync(string userId)
    {
        return await _dbContext.HqUsers
            .AsNoTracking()
            .Where(u => u.UserId == userId && (u.IsDisabled == null || u.IsDisabled == 0))
            .Select(u => new HqLoginUser(u.UserId, u.UserName, u.Role, u.Email, u.PasswordHash))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SetPasswordHashAsync(string userId, string passwordHash)
    {
        var user = await _dbContext.HqUsers.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;

        user.PasswordHash = passwordHash;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
