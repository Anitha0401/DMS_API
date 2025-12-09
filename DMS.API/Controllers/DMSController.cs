using DMS.Application.Interface;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.Models.DTO;
using DMS.Data.EF.Models.Manuals;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

//[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DMSController : ControllerBase
{
    private readonly IDMSService _service;
    private readonly ILogger<DMSController> _logger;

    public DMSController(IDMSService service, ILogger<DMSController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // Main Page - treeview
    [HttpGet("GetTreeViewManualList")]
    public async Task<ActionResult<List<Manual_TreeviewDto>>> GetTreeViewManualList(string userId)
    {
        _logger.LogInformation("Get Treeview Manual list");
        var manualList = await _service.GetTreeViewManualListAsync(userId);
        
        return Ok(manualList);
    }

    // Main Page - called mode
    [HttpGet("GetManualListBasedOnCalledMode")]
    public async Task<ActionResult<List<Manual_TreeviewDto>>> GetManualListBasedOnFilter(string calledMode, string userId)
    {
        _logger.LogInformation("Get Manual list based on calledMode");
        var manualList = await _service.GetManualListBasedOnCalledModeAsync(calledMode, userId);

        return Ok(manualList);
    }

    // Main Page - Right side manual content
    [HttpGet("GetManualContent")]
    public async Task<ActionResult<string?>> GetManualContent(long dm_ManualID, bool includeSubManuals)
    {
        _logger.LogInformation("Get Manual Content");
        var manualContent = await _service.GetManualContent(dm_ManualID, includeSubManuals);

        return Ok(manualContent);
    }

    // Used for edit and view manual
    [HttpGet("GetManualDetailsByVersionId/{DM_ManualVersionID}")]
    public async Task<ActionResult<ManualDetails>> GetEditManualDetailsByVersion(long DM_ManualVersionID)
    {
        var manualList = await _service.GetManualDetailsByVersionAsync(DM_ManualVersionID); 

        return Ok(manualList);
    }


    // Dashboard - Manual count
    [HttpGet("GetDashboardManualCounts")]
    public async Task<ActionResult<ManualCount_Dashboard>> GetDashboardManualCounts(string userId)
    {
        _logger.LogInformation("Get Dashboard Manual count");
        var manualCount = await _service.GetDashboardManualCountsAsync(userId);

        return Ok(manualCount);
    }

    // Dashboard/Main Page - User manual count
    [HttpGet("GetUserManualCounts")]
    public async Task<ActionResult<ManualCount_User>> GetUserManualCounts(string userId)
    {
        _logger.LogInformation("Get User Manual count");
        var manualCount = await _service.GetUserManualCountAsync(userId);

        return Ok(manualCount);
    }

    // User specific Manual List
    [HttpGet("GetUserManualListForCalledMode")]
    public async Task<ActionResult<List<ManualDetails>>> GetUserManualListForCalledMode(string userID, string calledMode)
    {
        var manualList = await _service.GetUserManualListBasedOnCalledModeAsync(userID, calledMode);

        return Ok(manualList);
    }

    // User Favouite Manual List
    [HttpGet("GetUserFavouiteManualList")]
    public async Task<ActionResult<List<ManualDetails>>> GetUserFavouiteManualList(string userID)
    {
        _logger.LogInformation("Get User Favouite Manual list");

        var result = await _service.GetUserFavouiteManualListAsync(userID);

        return Ok(result);
    }

    // Compare Version
    [HttpGet("GetManualVersionList")]
    public async Task<ActionResult<List<ManualVersionDetailsDto>>> GetManualVersionList(long dm_ManualID)
    {
        _logger.LogInformation("Get Compare Version List");
        var versionList = await _service.GetManualVersionDetailsAsync(dm_ManualID);

        return Ok(versionList);
    }

    // Compare Version - get manual basic details
    [HttpGet("GetManualSimpleDetails/{dm_ManualID}")]
    public async Task<ActionResult<Manual_SimpleDetails>> GetManualSimpleDetails(long dm_ManualID) 
    {
        var manualList = await _service.GetManualSimpleDetailsAsync(dm_ManualID); 

        return Ok(manualList);
    }

    // Compare Version - both manual contents
    [HttpGet("GetCompareVersion_Content")]
    public async Task<ActionResult<CompareVersionDetailsDto>> GetCompareVersion_Content(long dm_ManualID, long compare_ManualVersionID)
    {
        _logger.LogInformation("Get Compare Version Manual Content");
        var manualContent = await _service.GetCompareVersionDetailsAsync(dm_ManualID, compare_ManualVersionID);

        return Ok(manualContent);
    }

    // Get manual attachment details
    [HttpGet("GetManualAttachments")]
    public async Task<ActionResult<List<DmManualAttachment>>> GetManualAttachments(long dm_ManualID)
    {
        _logger.LogInformation("Get Manual Attachments");
        var manualAttachments = await _service.GetManualAttachmentListAsync(dm_ManualID); // Await the async method

        return Ok(manualAttachments);
    }

    // Add/Edit - Category list
    [HttpGet("GetManualCategory")]
    public async Task<ActionResult<ManualCategoryDto>> GetManualCategory()
    {
        _logger.LogInformation("Get Manual Category");

        var manualCategory = await _service.GetManualCategory();

        return Ok(manualCategory);
    }

    // Add/Edit - Approval Flow list
    [HttpGet("GetApprovalFlowList")]
    public async Task<ActionResult<ManualApprovalFlowListDto>> GetApprovalFlowList()
    {
        _logger.LogInformation("Get Approval Flow List");

        var flowList = await _service.GetApprovalFlowList();

        return Ok(flowList);
    }

    // Add/Edit - Approval Flow details
    [HttpGet("GetApprovalDetailsForFlowID/{FlowID}")]
    public async Task<ActionResult<ManualApprovalRoleListDto>> GetApprovalDetailsForFlowID(int flowID)
    {
        _logger.LogInformation("Get Approval Flow Details");

        var roleList = await _service.GetApprovalDetailsForFlowID(flowID);

        return Ok(roleList);
    }

    // Add/Edit - Manual Rights user list
    [HttpGet("GetManualRightsUsersAsync")]
    public async Task<ActionResult<AssignManualRightsDto>> GetManualRightsList()
    {
        _logger.LogInformation("Get Vessel and Rank List");

        var result = await _service.GetManualRightsUsersAsync();

        return Ok(result);
    }

    // Add/Edit - Selected Manual Rights user list
    [HttpGet("GetSelectedManualRightsList/{DM_ManualID}")]
    public async Task<ActionResult<AssignManualRightsDto>> GetSelectedManualRightsList(long dm_ManualID)
    {
        _logger.LogInformation("Get Manual assigned rights list");

        var result = await _service.GetDmManualAssignedRightsAsync(dm_ManualID);

        return Ok(result);
    }

    // Add/Edit - Selected Manual vessel ack list
    [HttpGet("GetManualUserAckList/{DM_ManualID}")]
    public async Task<ActionResult<List<ManualUserAckList>>> GetManualUserAckList(long dm_ManualID)
    {
        _logger.LogInformation("Get Manual user ack list");

        var result = await _service.GetManualUserAckListAsync(dm_ManualID);

        return Ok(result);
    }

    [HttpPost("AddManual")]
    public async Task<ActionResult<CreateManualResultDto>> AddManual(CreateManualDto manual) // Mark method as async
    {
        // Validation

        var DM_ManualID = await _service.AddDmManualAsync(manual); // Await the async method

        return Ok(DM_ManualID);
    }

    [HttpPost("EditManual")]
    public async Task<ActionResult> EditManual(EditManualDto manual) // Mark method as async
    {
        // Validation
        //Call Edit
        await _service.UpdateDmManualAsync(manual); // Await the async method

        return Ok();
    }

    [HttpPost("ApproveManual")]
    public async Task<ActionResult> ApproveManual(ManualApproveDto request) // Mark method as async
    {
        // Validation

        var DM_ManualVersionID = await _service.ApproveManualAsync(request, false); // Await the async method

        return Ok(DM_ManualVersionID);
    }

    [HttpPost("PublishManual")]
    public async Task<ActionResult> PublishManual(ManualApproveDto request) // Mark method as async
    {
        // Validation

        var DM_ManualVersionID = await _service.ApproveManualAsync(request, false); // Await the async method

        return Ok(DM_ManualVersionID);
    }

    [HttpPost("SendBackManual")]
    public async Task<ActionResult> SendBackManual(ManualApproveDto request) // Mark method as async
    {
        // Validation

        var DM_ManualVersionID = await _service.ApproveManualAsync(request, true); // Await the async method

        return Ok(DM_ManualVersionID);
    }

    [HttpDelete("DeleteManual/{DM_ManualID}")]
    public async Task<ActionResult> DeleteManual(long dm_ManualID) // Mark method as async
    {
        // object
        // Validation

        await _service.DeleteDmManualAsync(dm_ManualID); // Await the async method

        return Ok();
    }

    [HttpDelete("DeleteManualVersion/{DM_ManualID}")]
    public async Task<ActionResult> DeleteManualVersion(long dm_ManualID) // Mark method as async
    {
        // Validation

        await _service.DeleteDmManualVersionAsync(dm_ManualID); // Await the async method

        return Ok();
    }


    // Edit Manual rights details
    [HttpPost("UpdateManualRights")]
    public async Task<ActionResult> UpdateManualRights(ManualRightsDto manual) // Mark method as async
    {
        // Validation

        await _service.UpdateManualRights(manual); // Await the async method

        return Ok();
    }

    [HttpPost("AddToFavourite")]
    public ActionResult AddToFavourite(long dm_ManualID, string userID) // Mark method as async
    {
        // Validation

        _service.AddToFavouriteAsync(dm_ManualID, userID); // Await the async method

        return Ok();
    }

    // Get user notifications
    [HttpGet("GetManualNotifications")]
    public async Task<IActionResult> GetManualNotifications(int limit = 10)
    {
        var notifications = await _service.GetManualNotificationListAsync(limit);
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
