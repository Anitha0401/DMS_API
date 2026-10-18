using DMS.API.Auth;
using DMS.Application.DTO.Features;
using DMS.Application.Interface;
using DMS.Data.EF.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.API.Controllers;

/// <summary>
/// The new features (Database/012_NewFeatures.sql): pending acknowledgements, inbox, circular extras, suggestions,
/// certificates and search (folders are in OtherDocumentController). The user always comes from the token; the department from HQ_Users.
/// </summary>
public abstract class FeatureControllerBase : DmsControllerBase
{
    protected readonly SqlDb Db;

    protected FeatureControllerBase(SqlDb db) => Db = db;

    private OfficeUser? _me;

    protected async Task<OfficeUser> MeAsync()
    {
        if (_me != null) return _me;
        var id = CurrentUserId;
        var row = await Db.FirstAsync<UserRow>("SELECT TOP (1) UserName, HQDept FROM dbo.HQ_Users WHERE UserId = @id", ("@id", id));
        _me = new OfficeUser(id, row?.UserName ?? User.FindFirst(DmsClaims.Name)?.Value ?? id, CurrentUserRole, row?.HQDept);
        return _me;
    }

    protected bool IsAdmin => string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

    protected static async Task<List<UploadedFile>> ReadFilesAsync(IEnumerable<IFormFile>? files)
    {
        var list = new List<UploadedFile>();
        foreach (var f in files ?? Enumerable.Empty<IFormFile>())
        {
            if (f.Length == 0) continue;
            using var ms = new MemoryStream();
            await f.CopyToAsync(ms);
            list.Add(new UploadedFile(Path.GetFileName(f.FileName), f.ContentType, ms.ToArray()));
        }
        return list;
    }

    protected IActionResult FileOrNotFound(FileContent? file)
        => file == null ? NotFound() : File(file.Bytes, file.ContentType, file.FileName);

    protected IActionResult Done(string? error) => error == null ? Ok() : BadRequest(new { message = error });

    private sealed class UserRow
    {
        public string? UserName { get; set; }
        public string? HQDept { get; set; }
    }
}

// ---------------------------------------------------------------- 1. Pending acknowledgements

[Route("api/Pending")]
public class PendingAckController : FeatureControllerBase
{
    private readonly IAckDashboardService _service;

    public PendingAckController(SqlDb db, IAckDashboardService service) : base(db) => _service = service;

    [HttpGet("Vessels")]
    public async Task<IActionResult> Vessels() => Ok(await _service.GetVesselSummaryAsync());

    [HttpGet("Vessel")]
    public async Task<IActionResult> Vessel(int? vesselId, string? itemType, string? rank)
        => Ok(await _service.GetVesselPendingAsync(vesselId, itemType, rank));

    /// <summary>Office users' pending items; anyone but an administrator sees only their own.</summary>
    [HttpGet("Office")]
    public async Task<IActionResult> Office(string? userId)
        => Ok(await _service.GetOfficePendingAsync(IsAdmin ? userId : CurrentUserId));

    [HttpGet("Mine")]
    public async Task<IActionResult> Mine() => Ok(await _service.GetOfficePendingAsync(CurrentUserId));

    [HttpGet("Settings")]
    public async Task<IActionResult> GetSettings() => Ok(await _service.GetSettingsAsync());

    [HttpPut("Settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveSettings([FromBody] FeatureSettingsDto settings)
    {
        await _service.SaveSettingsAsync(settings);
        return Ok();
    }
}

// ---------------------------------------------------------------- 3 / 7. Circular extras and manual links

[Route("api/CircularExtras")]
public class CircularExtrasController : FeatureControllerBase
{
    private readonly ICircularExtrasService _service;

    public CircularExtrasController(SqlDb db, ICircularExtrasService service) : base(db) => _service = service;

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var extras = await _service.GetAsync(id);
        return extras == null ? NotFound() : Ok(extras);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Save(long id, [FromBody] SaveCircularExtras request) => Done(await _service.SaveAsync(id, request));

    [HttpPost("{id:long}/Links")]
    public async Task<IActionResult> AddLink(long id, [FromBody] AddManualLink request)
        => Done(await _service.AddLinkAsync(id, request, await MeAsync()));

    [HttpDelete("Links/{linkId:long}")]
    public async Task<IActionResult> RemoveLink(long linkId)
    {
        await _service.RemoveLinkAsync(linkId);
        return Ok();
    }

    [HttpPost("{id:long}/Cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelCircular request)
        => Done(await _service.CancelAsync(id, request, await MeAsync()));

    [HttpGet("Manual/{manualId:long}/Circulars")]
    public async Task<IActionResult> ForManual(long manualId) => Ok(await _service.GetForManualAsync(manualId));

    /// <summary>After a manual revision takes in the amendments: close its Amends / Supersedes links.</summary>
    [HttpPost("Manual/{manualId:long}/CloseLinks")]
    public async Task<IActionResult> CloseLinks(long manualId) => Ok(new { closed = await _service.CloseLinksForManualAsync(manualId) });

    [HttpGet("Manual/{manualId:long}")]
    public async Task<IActionResult> GetManual(long manualId)
    {
        var extras = await _service.GetManualAsync(manualId);
        return extras == null ? NotFound() : Ok(extras);
    }

    [HttpPut("Manual/{manualId:long}")]
    public async Task<IActionResult> SaveManual(long manualId, [FromBody] ManualExtras request)
    {
        await _service.SaveManualAsync(manualId, request.AckDueDays);
        return Ok();
    }

    [HttpGet("Categories")]
    public async Task<IActionResult> Categories() => Ok(await _service.GetCategoriesAsync());

    [HttpPut("Categories/{categoryId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveCategory(long categoryId, [FromBody] CircularCategorySetting request)
    {
        await _service.SaveCategoryAsync(categoryId, request.RequiresValidUntil);
        return Ok();
    }
}

// ---------------------------------------------------------------- 4. Suggestions

[Route("api/Suggestions")]
public class SuggestionsController : FeatureControllerBase
{
    private readonly ISuggestionService _service;

    public SuggestionsController(SqlDb db, ISuggestionService service) : base(db) => _service = service;

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
            return Ok(new { id = await _service.CreateAsync(request, await MeAsync()) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:long}/Status")]
    public async Task<IActionResult> Status(long id, [FromBody] SuggestionStatusChange request)
        => Done(await _service.ChangeStatusAsync(id, request, await MeAsync()));

    public sealed record CommentRequest(string Comment);

    [HttpPost("{id:long}/Comment")]
    public async Task<IActionResult> Comment(long id, [FromBody] CommentRequest request)
    {
        try
        {
            await _service.AddCommentAsync(id, request.Comment, await MeAsync());
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

// ---------------------------------------------------------------- 6. Certificates

[Route("api/Certificates")]
public class CertificatesController : FeatureControllerBase
{
    private readonly ICertificateService _service;

    public CertificatesController(SqlDb db, ICertificateService service) : base(db) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(int? vesselId, string? status, bool includeReplaced = false)
        => Ok(await _service.GetListAsync(vesselId, status, includeReplaced));

    [HttpGet("Types")]
    public async Task<IActionResult> Types() => Ok(await _service.GetTypesAsync());

    [HttpPut("Types/{typeId:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveType(long typeId, [FromBody] SaveCertificateType request)
    {
        await _service.SaveTypeAsync(typeId, request);
        return Ok();
    }

    [HttpPost]
    [RequestSizeLimit(200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    public async Task<IActionResult> Create([FromForm] int vesselId, [FromForm] long typeId, [FromForm] string title,
        [FromForm] string? documentNumber, [FromForm] string? certificateNo, [FromForm] string? issuedBy, [FromForm] DateTime? issueDate,
        [FromForm] DateTime? expiryDate, [FromForm] DateTime? nextSurveyDate, [FromForm] long? replacesId, [FromForm] string? remarks,
        [FromForm] List<IFormFile>? files)
    {
        var request = new NewCertificate(vesselId, typeId, title, documentNumber, certificateNo, issuedBy, issueDate, expiryDate,
            nextSurveyDate, replacesId, remarks, await ReadFilesAsync(files));
        var (id, error) = await _service.CreateAsync(request, await MeAsync());
        return error == null ? Ok(new { id }) : BadRequest(new { message = error });
    }

    [HttpGet("File/{blobId:long}")]
    public async Task<IActionResult> GetFile(long blobId) => FileOrNotFound(await _service.GetFileAsync(blobId));
}

// ---------------------------------------------------------------- 8. Search

[Route("api/Search")]
public class SearchController : FeatureControllerBase
{
    private readonly ISearchService _service;

    public SearchController(SqlDb db, ISearchService service) : base(db) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Search(string q, string? types) => Ok(await _service.SearchAsync(q ?? "", types, await MeAsync()));
}
