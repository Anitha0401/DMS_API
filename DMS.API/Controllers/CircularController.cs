using DMS.Application.Interface;
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

    // Main Page - treeview
    [HttpGet("GetTreeViewCircularList")]
    public async Task<ActionResult<List<Manual_TreeviewDto>>> GetTreeViewManualList(string userId, string circularType)
    {
        _logger.LogInformation("Get Treeview Circular list");
        var manualList = await _service.GetTreeViewCircularsListAsync(userId, circularType);
        
        return Ok(manualList);
    }

    [HttpPost("AddToFavourite")]
    public ActionResult AddToFavourite(long dm_ManualID, string circularType, string userID) // Mark method as async
    {
        // Validation

        _service.AddToFavouriteAsync(dm_ManualID, circularType, userID); // Await the async method

        return Ok();
    }

    // Get user notifications
    [HttpGet("GetCircularsNotifications")]
    public async Task<IActionResult> GetCircularsNotifications(int limit = 10)
    {
        var notifications = await _service.GetCIRNotificationListAsync(limit);
        return Ok(notifications);
    }

    // Mark notification as read
    [HttpPost("MarkNotificationRead")]
    public async Task<IActionResult> MarkNotificationRead(long notificationID)
    {
        // await _service.MarkAsRead(notificationID);
        return Ok();
    }
}
