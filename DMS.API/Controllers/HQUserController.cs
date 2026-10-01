using DMS.Application.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

[Route("api/[controller]")]
public class HQUserController : DmsControllerBase
{
    private readonly IHQUserService _service;
    private readonly ILogger<HQUserController> _logger;

    public HQUserController(IHQUserService service, ILogger<HQUserController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("GetHQDept")]
    public async Task<ActionResult<string>> GetHQDept()
    {
        _logger.LogInformation("Get HQ Dept List");

        var hqDeptList = await _service.GetHQDeptList();

        return Ok(hqDeptList);
    }

    [HttpGet("GetHQRoles")]
    public async Task<ActionResult<string>> GetHQRoles()
    {
        _logger.LogInformation("Get HQ Role List");

        var hqRoleList = await _service.GetHQUserRoles();

        return Ok(hqRoleList);
    }
}
