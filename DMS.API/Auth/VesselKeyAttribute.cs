using DMS.Application.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DMS.API.Auth;

/// <summary>
/// Vessel sync calls are made by the ship server, not by a logged-in user. Each call must carry
///   X-Vessel-Id:  the vessel's VesselID
///   X-Vessel-Key: the key generated for that vessel in the office app (Sync monitor > vessel > Sync key)
/// The key is checked against the hash in dbo.Sync_VesselKey. The checked vessel ID is put in
/// HttpContext.Items so actions never trust a vessel ID sent in the URL or body.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class VesselKeyAttribute : TypeFilterAttribute
{
    public const string VesselIdHeader = "X-Vessel-Id";
    public const string VesselKeyHeader = "X-Vessel-Key";
    public const string ItemKey = "SyncVesselId";

    public VesselKeyAttribute() : base(typeof(VesselKeyFilter)) { }

    /// <summary>The vessel the key belongs to (set by the filter).</summary>
    public static int VesselIdOf(HttpContext context) => (int)context.Items[ItemKey]!;

    private sealed class VesselKeyFilter : IAsyncActionFilter
    {
        private readonly ISyncService _sync;
        private readonly ILogger<VesselKeyFilter> _logger;

        public VesselKeyFilter(ISyncService sync, ILogger<VesselKeyFilter> logger)
        {
            _sync = sync;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;

            if (!await _sync.IsInstalledAsync())
            {
                context.Result = new ObjectResult("Vessel sync is not set up in the office database (run Database/005_VesselSync.sql).")
                { StatusCode = StatusCodes.Status503ServiceUnavailable };
                return;
            }

            var idText = http.Request.Headers[VesselIdHeader].ToString();
            var key = http.Request.Headers[VesselKeyHeader].ToString();
            if (!int.TryParse(idText, out var vesselId) || string.IsNullOrWhiteSpace(key) ||
                !await _sync.ValidateVesselKeyAsync(vesselId, key))
            {
                _logger.LogWarning("Sync call refused: missing or wrong vessel key (vessel '{VesselId}', {Ip})",
                    idText, http.Connection.RemoteIpAddress);
                context.Result = new UnauthorizedObjectResult("Missing or wrong vessel sync key.");
                return;
            }

            // A vessel ID in the query string or body must be the vessel the key belongs to.
            foreach (var value in context.ActionArguments.Values)
            {
                if (value == null || value.GetType().IsPrimitive) continue;
                if (value.GetType().GetProperty("VesselID")?.GetValue(value) is int other && other != 0 && other != vesselId)
                {
                    context.Result = new ObjectResult($"This key belongs to vessel {vesselId}, not vessel {other}.")
                    { StatusCode = StatusCodes.Status403Forbidden };
                    return;
                }
            }
            if (context.ActionArguments.TryGetValue("vesselId", out var q) && q is int queryVessel && queryVessel != 0 && queryVessel != vesselId)
            {
                context.Result = new ObjectResult($"This key belongs to vessel {vesselId}, not vessel {queryVessel}.")
                { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }

            http.Items[ItemKey] = vesselId;
            await next();
        }
    }
}
