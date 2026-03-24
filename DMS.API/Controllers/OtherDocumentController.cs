using DMS.Application.Interface;
using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.OtherDocument;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OtherDocumentController : ControllerBase
{
    private readonly IOtherDocumentService _service;
    private readonly ILogger<OtherDocumentController> _logger;

    public OtherDocumentController(IOtherDocumentService service, ILogger<OtherDocumentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("GetTypes")]
    public async Task<ActionResult<List<CIR_OtherDocument_Type>>> GetTypes(bool includeInactive = false)
    {
        _logger.LogInformation("Get OtherDocument types (includeInactive={includeInactive})", includeInactive);
        var types = await _service.GetTypesAsync(includeInactive);
        return Ok(types);
    }

    [HttpGet("GetRecentOtherDocuments")]
    public async Task<ActionResult<List<Vw_CIR_OtherDocument>>> GetRecentOtherDocuments(int limit = 10)
    {
        _logger.LogInformation("Get recent OtherDocuments (limit={limit})", limit);
        var list = await _service.GetRecentOtherDocumentsAsync(limit);
        return Ok(list);
    }

    [HttpGet("GetOtherDocumentDetails/{otherDocumentId}")]
    public async Task<ActionResult<Vw_CIR_OtherDocument>> GetOtherDocumentDetails(long otherDocumentId)
    {
        _logger.LogInformation("Get OtherDocument details (id={otherDocumentId})", otherDocumentId);
        var details = await _service.GetOtherDocumentDetailsAsync(otherDocumentId);
        if (details == null)
            return NotFound();
        return Ok(details);
    }

    [HttpPost("AddOtherDocument")]
    public async Task<ActionResult<long>> AddOtherDocument([FromBody] OtherDocumentDto model)
    {
        if (model is null)
            return BadRequest("Payload is required.");

        var id = await _service.AddOtherDocumentAsync(model);
        return Created(string.Empty, id);
    }

    [HttpPost("AddOtherDocumentRecord")]
    public async Task<ActionResult<long>> AddOtherDocumentRecord()
    {
        var id = await _service.InsertOtherDocumentRecordAsync();
        return Ok(id);
    }

    [HttpDelete("RemoveTempOtherDocument")]
    public async Task<IActionResult> RemoveTempOtherDocument(long otherDocumentId)
    {
        await _service.RemoveTempOtherDocumentAsync(otherDocumentId);
        return Ok();
    }
}