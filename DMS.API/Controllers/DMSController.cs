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
        public async Task<ActionResult<string?>> GetManualContent(long dm_ManualID)
        {
            _logger.LogInformation("Get Manual Content");
            var manualContent = await _service.GetManualContent(dm_ManualID); // Await the async method

            return Ok(manualContent);
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
        public async Task<ActionResult> EditManual(CreateManualVersionDto manual) // Mark method as async
        {
            // Validation

            var dm_ManualVersionID = await _service.AddDmManualVersionAsync(manual); // Await the async method

            return Ok(dm_ManualVersionID);
        }

        [HttpPost("EditManualVersion")]
        public async Task<ActionResult> EditManualVersion(CreateManualVersionDto manual) // Mark method as async
        {
            // Validation

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

        [HttpGet("GetDM_Manual")]
        public async Task<ActionResult<List<DM_Manual>>> GetDM_ManualList() // Mark method as async
        {
            _logger.LogInformation("Get DM_Manual List");

            var manualList = await _service.GetDmManualsAsync(); // Await the async method

            foreach (var s in manualList)
            {
                Console.WriteLine($"Name: {s.ManualName}, Id: {s.DM_ManualID}");
                Console.WriteLine(new string('-', 30));
            }

            return Ok(manualList);
        }
    }
}
