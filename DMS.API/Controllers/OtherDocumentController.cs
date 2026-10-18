using DMS.Application;
using DMS.Application.DTO.OtherDocument;
using DMS.Application.DTO.Features;
using DMS.Application.Interface;
using DMS.Data.EF.Features;
using DMS.Data.EF.Models.OtherDocument;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

/// <summary>
/// Other Documents, and the folders they are filed in (FLD_Folder). A folder holds Other Documents only.
/// </summary>
[Route("api/[controller]")]
public class OtherDocumentController : FeatureControllerBase
{
    private readonly IOtherDocumentService _service;
    private readonly ILogger<OtherDocumentController> _logger;

    public OtherDocumentController(SqlDb db, IOtherDocumentService service, ILogger<OtherDocumentController> logger) : base(db)
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

    // ---------------------------------------------------------------- Folders

    /// <summary>GET api/OtherDocument/Folders - the folders this user may open.</summary>
    [HttpGet("Folders")]
    public async Task<IActionResult> FolderTree() => Ok(await _service.GetFolderTreeAsync(await MeAsync()));

    /// <summary>POST api/OtherDocument/Folders - a new folder.</summary>
    [HttpPost("Folders")]
    public async Task<IActionResult> CreateFolder([FromBody] SaveFolder request)
    {
        var (id, error) = await _service.SaveFolderAsync(null, request, await MeAsync());
        return error == null ? Ok(new { id }) : BadRequest(new { message = error });
    }

    /// <summary>PUT api/OtherDocument/Folders/{id} - rename or move a folder.</summary>
    [HttpPut("Folders/{id:long}")]
    public async Task<IActionResult> UpdateFolder(long id, [FromBody] SaveFolder request)
    {
        var (_, error) = await _service.SaveFolderAsync(id, request, await MeAsync());
        return Done(error);
    }

    /// <summary>DELETE api/OtherDocument/Folders/{id} - an empty folder.</summary>
    [HttpDelete("Folders/{id:long}")]
    public async Task<IActionResult> DeleteFolder(long id) => Done(await _service.DeleteFolderAsync(id, await MeAsync()));

    // ---------------------------------------------------------------- Documents in folders

    /// <summary>GET api/OtherDocument/Folders/{id}/Documents - the documents filed in this folder.</summary>
    [HttpGet("Folders/{id:long}/Documents")]
    public async Task<IActionResult> FolderDocuments(long id, int? vesselId)
        => Ok(await _service.GetFolderDocumentsAsync(id, vesselId, await MeAsync()));

    /// <summary>GET api/OtherDocument/Folders/Unfiled/Documents?vesselId= - documents not in any folder.</summary>
    [HttpGet("Folders/Unfiled/Documents")]
    public async Task<IActionResult> UnfiledDocuments(int? vesselId)
        => Ok(await _service.GetFolderDocumentsAsync(null, vesselId, await MeAsync()));

    /// <summary>GET api/OtherDocument/Vessel/{vesselId}/Documents - every document under a vessel, with its folder.</summary>
    [HttpGet("Vessel/{vesselId:int}/Documents")]
    public async Task<IActionResult> VesselDocuments(int vesselId) => Ok(await _service.GetVesselDocumentsAsync(vesselId, await MeAsync()));

    /// <summary>POST api/OtherDocument/Folders/{id}/Documents (multipart) - a new office-only document in this folder.</summary>
    [HttpPost("Folders/{id:long}/Documents")]
    [RequestSizeLimit(200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    public Task<IActionResult> CreateFolderDocument(long id, [FromForm] long typeId, [FromForm] string documentNumber, [FromForm] string title,
        [FromForm] string? status, [FromForm] string? reference, [FromForm] string? comments, [FromForm] int? vesselId,
        [FromForm] List<IFormFile>? files)
        => CreateDocumentIn(id, typeId, documentNumber, title, status, reference, comments, vesselId, files);

    /// <summary>POST api/OtherDocument/Folders/Unfiled/Documents (multipart) - a new office-only document, not in a folder.</summary>
    [HttpPost("Folders/Unfiled/Documents")]
    [RequestSizeLimit(200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    public Task<IActionResult> CreateUnfiledDocument([FromForm] long typeId, [FromForm] string documentNumber, [FromForm] string title,
        [FromForm] string? status, [FromForm] string? reference, [FromForm] string? comments, [FromForm] int? vesselId,
        [FromForm] List<IFormFile>? files)
        => CreateDocumentIn(null, typeId, documentNumber, title, status, reference, comments, vesselId, files);

    private async Task<IActionResult> CreateDocumentIn(long? folderId, long typeId, string documentNumber, string title, string? status,
        string? reference, string? comments, int? vesselId, List<IFormFile>? files)
    {
        var request = new NewFolderDocument(typeId, documentNumber, title, status, reference, comments, vesselId, await ReadFilesAsync(files));
        var (newId, error) = await _service.CreateDocumentAsync(folderId, request, await MeAsync());
        return error == null ? Ok(new { id = newId }) : BadRequest(new { message = error });
    }

    /// <summary>PUT api/OtherDocument/Documents/{documentId}/Folder { folderId } - file a document (null = out of its folder).</summary>
    [HttpPut("Documents/{documentId:long}/Folder")]
    public async Task<IActionResult> MoveDocument(long documentId, [FromBody] MoveDocument request)
        => Done(await _service.MoveDocumentAsync(documentId, request.FolderId, await MeAsync()));

    /// <summary>DELETE api/OtherDocument/Documents/{documentId} - a document added at the office (office only).</summary>
    [HttpDelete("Documents/{documentId:long}")]
    public async Task<IActionResult> DeleteDocument(long documentId) => Done(await _service.DeleteDocumentAsync(documentId, await MeAsync()));

    /// <summary>GET api/OtherDocument/Documents/{documentId}/Files - the files attached to a document.</summary>
    [HttpGet("Documents/{documentId:long}/Files")]
    public async Task<IActionResult> DocumentFiles(long documentId) => Ok(await _service.GetDocumentFilesAsync(documentId));
}