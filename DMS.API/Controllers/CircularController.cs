using DMS.Application.Interface;
using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.DTO;
using DMS.Data.EF.Models.Manuals;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

//[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CircularController : ControllerBase
{
    private readonly ICIRService _service;
    private readonly ILogger<DMSController> _logger;

    public CircularController(ICIRService service, ILogger<DMSController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("GetCategories")]
    public async Task<ActionResult<List<CircularCategoryDto>>> GetCategories(bool isToIncludeAll, string? circularType = null)
    {
        _logger.LogInformation("Get CIR categories (circularType={circularType})", circularType);
        var categories = await _service.GetCategoriesAsync(isToIncludeAll, circularType);
        return Ok(categories);
    }

    [HttpGet("GetCIR_CategoryChartData")]
    public async Task<ActionResult<List<CircularsChart>>> GetCIR_CategoryChartData(string circularType)
    {
        _logger.LogInformation("Get Circulars chart data (circularType={circularType})", circularType);
        var chartData = await _service.GetCIR_CategoryChartDataAsync(circularType);
        return Ok(chartData);
    }

    [HttpGet("GetRecentCirculars")]
    public async Task<ActionResult<List<CIRRecentList>>> GetRecentCirculars(string circularType, int limit = 10)
    {
        _logger.LogInformation("Get Recent Circulars (circularType={circularType}, limit={limit})", circularType, limit);
        var recent = await _service.GetRecentCircularsAsync(circularType, limit);
        return Ok(recent);
    }

    [HttpGet("GetDashboardCircularCounts")]
    public async Task<ActionResult<CircularCount_Dashboard>> GetDashboardCircularCounts()
    {
        _logger.LogInformation("Get Dashboard Circular counts");
        var counts = await _service.GetDashboardCircularCountsAsync();
        return Ok(counts);
    }

    [HttpGet("GetCircularsNotifications")]
    public async Task<IActionResult> GetCircularsNotifications(int limit = 10)
    {
        var notifications = await _service.GetCIRNotificationListAsync(limit);
        return Ok(notifications);
    }


    [HttpGet("GetTreeViewCircularList")]
    public async Task<ActionResult<List<Circulars_TreeviewDto>>> GetTreeViewManualList(string userId, string circularType)
    {
        _logger.LogInformation("Get Treeview Circular list");
        var manualList = await _service.GetTreeViewCircularsListAsync(userId, circularType);

        return Ok(manualList);
    }

    [HttpGet("GetCIRMasterList")]
    public async Task<ActionResult<List<CIRMasterList>>> GetCIRMasterList(string userId, long categoryId, string circularType)
    {
        _logger.LogInformation("Get CIR master list (categoryId={categoryId}, circularType={circularType})", categoryId, circularType);
        var masters = await _service.GetCIRMasterListAsync(userId, categoryId, circularType);
        return Ok(masters);
    }

    [HttpGet("GetVesselAckList/{cir_MasterID}")]
    public async Task<ActionResult<List<CircularUserAckDto>>> GetVesselAckList(long cir_MasterID)
    {
        _logger.LogInformation("Get Vessel ack list (cir_MasterID={cir_MasterID})", cir_MasterID);
        var ackList = await _service.GetVesselAckListAsync(cir_MasterID);
        return Ok(ackList);
    }

    [HttpGet("GetNextCIRNumbersByCategory")]
    public async Task<ActionResult<string>> GetNextCIRNumbersByCategory(long categoryId, long cir_MasterID)
    {
        _logger.LogInformation("Get CIR numbers by category (categoryId={categoryId})", categoryId);
        var numbers = await _service.GetNextCIRNumbersByCategoryAsync(categoryId, cir_MasterID);
        return Ok(numbers);
    }

    [HttpGet("GetCIRDetails/{ciR_MasterID}")]
    public async Task<IActionResult> GetCIRDetails(long ciR_MasterID)
    {
        _logger.LogInformation("Get Circular details (cirMasterId={ciR_MasterID})", ciR_MasterID);
        var result = await _service.GetCIRDetailsAsync(ciR_MasterID);
        return Ok(result);
    }

    [HttpGet("GetCircularLogs")]
    public async Task<IActionResult> GetCircularLogs(long ciR_MasterID)
    {
        _logger.LogInformation("Get Circular logs (cirMasterId={ciR_MasterID})", ciR_MasterID);
        var logs = await _service.GetCircularLogsAsync(ciR_MasterID);
        return Ok(logs);
    }

    [HttpPost("AddCirMaster")]
    public async Task<ActionResult<long>> AddCIR_MasterRecord(string circularType)
    {
        var result = await _service.InsertCIR_MasterRecord(circularType);

        return result;
    }


    [HttpDelete("RemoveTempCIR")]
    public async Task<ActionResult<long>> RemoveTempCIR(long cir_MasterID)
    {
       await _service.RemoveTempCIRAsync(cir_MasterID);

        return Ok();
    }

    [HttpPost("AddCircular")]
    public async Task<ActionResult<CIR_Master>> CreateCircular([FromBody] CreateCircularDto createdDto)
    {
        if (createdDto is null)
            return BadRequest("Circular payload is required.");

        var created = await _service.CreateCircularAsync(createdDto);

        return Created(string.Empty, created);
    }

    [HttpPost("AddToFavourite")]
    public ActionResult AddToFavourite(long cir_MasterID, string circularType, string userID)
    {
        // Validation

        _service.AddToFavouriteAsync(cir_MasterID, circularType, userID);

        return Ok();
    }

    // Mark notification as read
    [HttpPost("MarkNotificationRead")]
    public async Task<IActionResult> MarkNotificationRead(long notificationID)
    {
        // await _service.MarkAsRead(notificationID);
        return Ok();
    }
}
