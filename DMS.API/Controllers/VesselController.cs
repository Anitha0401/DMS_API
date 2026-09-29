using DMS.Application.Interface;
using DMS.Application.DTO;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VesselController : ControllerBase
{
    private readonly IVesselService _service;
    private readonly ILogger<VesselController> _logger;
  
    public VesselController(IVesselService service, ILogger<VesselController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("GetVslType")]
    public async Task<ActionResult<string>> GetVslType()
    {
        _logger.LogInformation("Get Vsl Type List");

        var hqUsertList = await _service.GetVslTypeList();

        return Ok(hqUsertList);
    }

    [HttpGet("GetVessels")]
    public async Task<ActionResult<List<VesselListDto>>> GetVessels()
    {
        _logger.LogInformation("Get Vessel List");

        var hqUsertList = await _service.GetVesselList();

        return Ok(hqUsertList);
    }
}
