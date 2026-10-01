using DMS.Application;
using DMS.Application.DTO.OtherDocument;
using DMS.Application.Interface;
using DMS.Data.EF.Models.OtherDocument;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

[Route("api/[controller]")]
public class OtherDocumentController : DmsControllerBase
{
    private readonly IOtherDocumentService _service;
    private readonly ILogger<OtherDocumentController> _logger;

    public OtherDocumentController(IOtherDocumentService service, ILogger<OtherDocumentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("GetTypes")]
    public async Task<ActionResult<List<OtherDocument_Type>>> GetTypes()
    {
        _logger.LogInformation("Get OtherDocument types");
        var types = await _service.GetDocumentTypesAsync();
        return Ok(types);
    }

    [HttpGet("GetDocuments")]
    public async Task<ActionResult<List<Vw_OtherDocument_MasterBlob>>> GetDocuments(int? vesselId = null, string? documentType = null)
    {
        _logger.LogInformation("Get OtherDocuments (vesselId={vesselId}, documentType={documentType})", vesselId, documentType);
        var list = await _service.GetDocumentsAsync(vesselId, documentType);
        return Ok(list);
    }

    // GET api/OtherDocument/nextnumber/{typeId}
    [HttpGet("nextnumber/{typeId:long}")]
    public async Task<IActionResult> GetNextNumber(long typeId)
    {
        var number = await _service.GetNextDocumentNumberAsync(typeId);
        return Ok(new { documentNumber = number });
    }

    [HttpGet("GetRecentOtherDocuments")]
    public async Task<ActionResult<List<Vw_OtherDocument_MasterBlob>>> GetRecentOtherDocuments(int limit = 10)
    {
        _logger.LogInformation("Get recent OtherDocuments (limit={limit})", limit);
        var list = await _service.GetRecentOtherDocumentsAsync(limit);
        return Ok(list);
    }

    [HttpGet("GetOtherDocumentDetails/{otherDocumentMasterId}")]
    public async Task<ActionResult<Vw_OtherDocument_MasterBlob>> GetOtherDocumentDetails(long otherDocumentMasterId)
    {
        _logger.LogInformation("Get OtherDocument details (id={otherDocumentMasterId})", otherDocumentMasterId);
        var details = await _service.GetOtherDocumentDetailsAsync(otherDocumentMasterId);
        if (details == null)
            return NotFound();
        return Ok(details);
    }

    [HttpPost("AddOtherDocument")]
    public async Task<ActionResult<long>> AddOtherDocument([FromBody] CreateOtherDocumentDto model)
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
    public async Task<IActionResult> RemoveTempOtherDocument(long otherDocumentMasterId)
    {
        await _service.RemoveTempOtherDocumentAsync(otherDocumentMasterId);
        return Ok();
    }
}