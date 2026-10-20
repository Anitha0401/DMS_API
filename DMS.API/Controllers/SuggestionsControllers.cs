using DMS.Application.DTO.Features;
using DMS.Application.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

[Route("api/Suggestions")]
public class SuggestionsController : DmsControllerBase
{
    private readonly ISuggestionService _service;

    public SuggestionsController(ISuggestionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(string? status, int? vesselId, bool mine = false)
        => Ok(await _service.GetListAsync(status, vesselId, mine ? CurrentUserId : null));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var detail = await _service.GetAsync(id);
        return detail == null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    [RequestSizeLimit(200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    public async Task<IActionResult> Create([FromForm] string title, [FromForm] string? description, [FromForm] string? itemType,
        [FromForm] long? itemId, [FromForm] string? quotedText, [FromForm] string? priority, [FromForm] List<IFormFile>? files)
    {
        try
        {
            var request = new NewSuggestion(title, description, itemType, itemId, quotedText, priority, await ReadFilesAsync(files));
            return Ok(new { id = await _service.CreateAsync(request, Me) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:long}/Status")]
    public async Task<IActionResult> Status(long id, [FromBody] SuggestionStatusChange request)
        => Done(await _service.ChangeStatusAsync(id, request, Me));

    public sealed record CommentRequest(string Comment);

    [HttpPost("{id:long}/Comment")]
    public async Task<IActionResult> Comment(long id, [FromBody] CommentRequest request)
    {
        try
        {
            await _service.AddCommentAsync(id, request.Comment, Me);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("File/{blobId:long}")]
    public async Task<IActionResult> GetFile(long blobId) => FileOrNotFound(await _service.GetFileAsync(blobId));
}