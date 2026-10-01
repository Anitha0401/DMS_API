using DMS.API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

/// <summary>
/// Every DMS controller requires a logged-in user. The user ID always comes from the token,
/// never from a userId sent in the request (those parameters are kept only for compatibility).
/// </summary>
[Authorize]
[ApiController]
public abstract class DmsControllerBase : ControllerBase
{
    protected string CurrentUserId =>
        User.FindFirst(DmsClaims.UserId)?.Value
        ?? throw new UnauthorizedAccessException("The token has no user ID.");

    protected string CurrentUserRole => User.FindFirst(DmsClaims.Role)?.Value ?? "";
}
