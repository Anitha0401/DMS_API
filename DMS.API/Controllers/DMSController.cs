using Microsoft.AspNetCore.Mvc;
using DMS.Application;
using DMS.Data.EF.Models;
using DMS.Application.Interface;
using Microsoft.AspNetCore.Authorization;
using DMS.Data.EF.Query;
using DMS.Data.EF.DTO;

namespace DMS.API.Controllers
{
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

        [HttpGet("GetTreeViewManualList")]
        public async Task<ActionResult<List<Manual_TreeviewDto>>> GetTreeViewManualList()
        {
            _logger.LogInformation("Get Treeview Manual list");
            var manualList = await _service.GetTreeViewManualListAsync(); // Await the async method
            
            return Ok(manualList);
        }

        [HttpGet("GetManualContent")]
        public async Task<ActionResult<string?>> GetManualContent(long dm_ManualID, bool includeSubManuals)
        {
            _logger.LogInformation("Get Manual Content");
            var manualContent = await _service.GetManualContent(dm_ManualID, includeSubManuals); // Await the async method

            return Ok(manualContent);
        }

        [HttpGet("GetManualCategory")]
        public async Task<ActionResult<ManualCategoryDto>> GetManualCategory()
        {
            _logger.LogInformation("Get Manual Category");

            var manualCategory = await _service.GetManualCategory();

            return Ok(manualCategory);
        }

        [HttpGet("GetVesselList")]
        public async Task<ActionResult<VesselListDto>> GetVesselList()
        {
            _logger.LogInformation("Get Vessel List");

            var vesselList = await _service.GetVesselList();

            return Ok(vesselList);
        }

        [HttpGet("GetVslRankList")]
        public async Task<ActionResult<VslRankListDto>> GetVslRankList()
        {
            _logger.LogInformation("Get Vessel Rank List");

            var vslRankList = await _service.GetVslRankList();

            return Ok(vslRankList);
        }


        [HttpGet("GetManualById/{dm_ManualID}")]
        public async Task<ActionResult<Dm_ManualDetails>> GetManualDetails(long dm_ManualID) // Mark method as async
        {
            var manualList = await _service.GetDmManualDetailsAsync(dm_ManualID); // Await the async method

            return Ok(manualList);
        }

        [HttpGet("GetManualAttachments")]
        public async Task<ActionResult<List<DM_Manual_Attachments>>> GetManualAttachments(long dm_ManualID)
        {
            _logger.LogInformation("Get Manual Attachments");
            var manualAttachments = await _service.GetManualAttachmentListAsync(dm_ManualID); // Await the async method

            return Ok(manualAttachments);
        }

        [HttpPost("AddManual")]
        public async Task<ActionResult> AddManual(CreateManualDto manual) // Mark method as async
        {
            // Validation

            var dm_ManualID = await _service.AddDmManualAsync(manual); // Await the async method

            return Ok(dm_ManualID);
        }

        [HttpPost("AddManualVersion")]
        public async Task<ActionResult> AddManualVersion(CreateManualVersionDto manual) // Mark method as async
        {
            // Validation

            var dm_ManualVersionID = await _service.AddDmManualVersionAsync(manual); // Await the async method

            return Ok(dm_ManualVersionID);
        }

        [HttpPost("EditManual")]
        public async Task<ActionResult> EditManual(EditManualDto manual) // Mark method as async
        {
            // Validation
            //Call Edit
            await _service.UpdateDmManualAsync(manual); // Await the async method

            return Ok();
        }

        [HttpPost("EditManualVersion")]
        public async Task<ActionResult> EditManualVersion(CreateManualVersionDto manual) // Mark method as async
        {
            // Validation
            // Edit need to apply logi
            var dm_ManualVersionID = await _service.AddDmManualVersionAsync(manual); // Await the async method

            return Ok(dm_ManualVersionID);
        }

        [HttpPost("ApproveManual")]
        public async Task<ActionResult> ApproveManual(ApproveManualDto request) // Mark method as async
        {
            // Validation

            var dm_ManualVersionID = await _service.ApproveManualAsync(request); // Await the async method

            return Ok(dm_ManualVersionID);
        }

        [HttpPost("PublishManual")]
        public async Task<ActionResult> PublishManual(ApproveManualDto request) // Mark method as async
        {
            // Validation

            var dm_ManualVersionID = await _service.ApproveManualAsync(request); // Await the async method

            return Ok(dm_ManualVersionID);
        }

        [HttpPost("SendBackManual")]
        public async Task<ActionResult> SendBackManual(ApproveManualDto request) // Mark method as async
        {
            // Validation

            var dm_ManualVersionID = await _service.ApproveManualAsync(request); // Await the async method

            return Ok(dm_ManualVersionID);
        }

        [HttpDelete("DeleteManual/{dm_ManualID}")]
        public async Task<ActionResult> DeleteManual(long dm_ManualID) // Mark method as async
        {
            // object
            // Validation

            await _service.DeleteDmManualAsync(dm_ManualID); // Await the async method

            return Ok();
        }

        [HttpDelete("DeleteManualVersion/{dm_ManualID}")]
        public async Task<ActionResult> DeleteManualVersion(long dm_ManualID) // Mark method as async
        {
            // Validation

            await _service.DeleteDmManualVersionAsync(dm_ManualID); // Await the async method

            return Ok();
        }
    }
}
