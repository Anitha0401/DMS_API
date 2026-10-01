using DMS.Application.Interface;
using DMS.Application.DTO;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

[Route("api/[controller]")]
public class VesselController : DmsControllerBase
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

    /// <summary>Everything sent to one vessel (manuals, circulars/alerts, other documents) and how far it is acknowledged.</summary>
    [HttpGet("GetVesselSyncItems")]
    public async Task<ActionResult<List<VesselSyncItemDto>>> GetVesselSyncItems(int vesselId)
    {
        _logger.LogInformation("Get sync items for vessel {VesselId}", vesselId);
        return Ok(await _service.GetVesselSyncItemsAsync(vesselId));
    }

    /// <summary>Sync runs for one vessel, newest first.</summary>
    [HttpGet("GetVesselSyncHistory")]
    public async Task<ActionResult<VesselSyncHistoryDto>> GetVesselSyncHistory(int vesselId)
    {
        _logger.LogInformation("Get sync history for vessel {VesselId}", vesselId);
        return Ok(await _service.GetVesselSyncHistoryAsync(vesselId));
    }
}
