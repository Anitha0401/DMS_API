using DMS.Application.DTO;
using DMS.Application.Interface;
using DMS.Data.EF.Context;
using DMS.Data.EF.Query.Manuals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Application;

public class VesselService : IVesselService
{
    private readonly GeneralContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ManualGeneralQueries _queries;
    private readonly DmsContext _dms;
    private readonly CircularsContext _circulars;
    private readonly OtherDocumentsContext _otherDocs;

    public VesselService(GeneralContext context, IMediator mediator, ManualGeneralQueries queries,
                         DmsContext dms, CircularsContext circulars, OtherDocumentsContext otherDocs)
    {
        _dms = dms;
        _circulars = circulars;
        _otherDocs = otherDocs;
        _dbContext = context;
        _mediator = mediator;
        _queries = queries;
    }

    public async Task<List<string>> GetVslTypeList()
    {
        var vslTypeList = await _dbContext.VVessels
                                .Select(x => x.VslType)
                                .Where(vslType => !string.IsNullOrEmpty(vslType))
                                .Distinct()
                                .ToListAsync();

        // Ensure null values are filtered out to match the interface signature
        return vslTypeList.Where(vslType => vslType != null).Cast<string>().ToList();
    }

    public async Task<List<VesselListDto>> GetVesselList()
    {
        var vesselList = await _dbContext.VVessels
                                      .Select(x => new VesselListDto
                                      {
                                          VesselID = x.VesselID,
                                          VslName = x.VslName,
                                          VslCode = x.VslCode,
                                          VslType = x.VslType
                                      }).ToListAsync();
        return vesselList;
    }

    // ---------------------------------------------------------------------------------------------
    // Vessel sync: what has been sent to a vessel, and the sync runs.
    // ---------------------------------------------------------------------------------------------

    public async Task<List<VesselSyncItemDto>> GetVesselSyncItemsAsync(int vesselId)
    {
        var items = new List<VesselSyncItemDto>();

        // Circulars and alerts. VesselID -1 means "all vessels".
        var cirToAck = await _circulars.CIR_ToAck_Vsl.AsNoTracking()
            .Where(a => (a.VesselID == vesselId || a.VesselID == -1) && (a.IsActive ?? 1) == 1)
            .Select(a => new { a.CIR_MasterID, a.UserRank })
            .ToListAsync();
        var cirIds = cirToAck.Select(a => a.CIR_MasterID).Distinct().ToList();

        if (cirIds.Count > 0)
        {
            var masters = await _circulars.CIR_Master.AsNoTracking()
                .Where(m => cirIds.Contains(m.CIR_MasterID) && (m.IsDeleted ?? 0) == 0 && m.Status != "Temp")
                .ToListAsync();
            var acks = await _circulars.Vw_CIR_VesselAck.AsNoTracking()
                .Where(x => x.VesselID == vesselId && x.CIR_MasterID != null && cirIds.Contains(x.CIR_MasterID.Value))
                .ToListAsync();

            foreach (var m in masters)
            {
                var ranks = RankSet(cirToAck.Where(a => a.CIR_MasterID == m.CIR_MasterID).Select(a => a.UserRank));
                var read = acks.Where(a => a.CIR_MasterID == m.CIR_MasterID && a.DateRead != null).ToList();
                items.Add(Item(
                    m.CircularType != null && m.CircularType.Trim().ToLower().StartsWith("alert") ? "Alert" : "Circular",
                    m.CIR_MasterID, m.CIR_Number, m.Title, null, null,
                    m.ReleasedDate ?? m.DateIssued, ranks, read.Select(a => a.UserRank), read.Max(a => a.DateRead)));
            }
        }

        // Manuals.
        var manToAck = await _dms.DmManualToAckVsls.AsNoTracking()
            .Where(a => (a.VesselID == vesselId || a.VesselID == -1) && (a.IsActive ?? 1) == 1)
            .Select(a => new { a.DM_ManualID, a.UserRank })
            .ToListAsync();
        var manIds = manToAck.Select(a => a.DM_ManualID).Distinct().ToList();

        if (manIds.Count > 0)
        {
            var manuals = await _dms.DmManuals.AsNoTracking()
                .Where(m => manIds.Contains(m.DM_ManualID) && (m.IsDeleted ?? 0) == 0)
                .ToListAsync();
            var versions = await _dms.DmManualVersions.AsNoTracking()
                .Where(v => manIds.Contains(v.DM_ManualID) && v.IsActive == 1)
                .Select(v => new { v.DM_ManualID, v.DM_ManualVersionID, v.Version, v.ReleasedDate, v.PublishedDate })
                .ToListAsync();
            var acks = await _dms.DmManualUserAcks.AsNoTracking()
                .Where(a => a.VesselID == vesselId && manIds.Contains(a.DM_ManualID))
                .ToListAsync();

            foreach (var m in manuals)
            {
                var latest = versions.Where(v => v.DM_ManualID == m.DM_ManualID)
                                     .OrderByDescending(v => v.DM_ManualVersionID).FirstOrDefault();
                var ranks = RankSet(manToAck.Where(a => a.DM_ManualID == m.DM_ManualID).Select(a => a.UserRank));
                // Only acknowledgements of the current version count.
                var read = acks.Where(a => a.DM_ManualID == m.DM_ManualID && a.DateRead != null &&
                                           (latest == null || a.DM_ManualVersionID == latest.DM_ManualVersionID)).ToList();
                items.Add(Item("Manual", m.DM_ManualID, m.ManualNo, m.ManualName, latest?.Version, null,
                    latest?.ReleasedDate ?? latest?.PublishedDate ?? m.CreatedDate,
                    ranks, read.Select(a => a.UserRank), read.Max(a => a.DateRead)));
            }
        }

        // Other documents uploaded for this vessel (no acknowledgement).
        var types = await _otherDocs.OtherDocument_Type.AsNoTracking()
            .ToDictionaryAsync(t => t.OtherDocument_TypeID, t => t.DocumentType);
        var docs = await _otherDocs.OtherDocumentMaster.AsNoTracking()
            .Where(d => d.VesselID == vesselId && (d.IsDeleted ?? 0) == 0)
            .ToListAsync();
        foreach (var d in docs)
        {
            types.TryGetValue(d.OtherDocument_TypeID ?? 0, out var typeName);
            items.Add(Item("Other document", d.OtherDocumentMasterID, d.DocumentNumber, d.DocumentTitle, null, typeName,
                d.UploadedOn ?? d.CreatedDate, new HashSet<string>(), Enumerable.Empty<string?>(), null));
        }

        return items.OrderByDescending(i => i.IssuedOn ?? DateTime.MinValue).ToList();
    }

    public async Task<VesselSyncHistoryDto> GetVesselSyncHistoryAsync(int vesselId)
    {
        var result = new VesselSyncHistoryDto();
        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            using (var check = connection.CreateCommand())
            {
                check.CommandText = "SELECT CASE WHEN OBJECT_ID('dbo.Sync_Package', 'U') IS NULL THEN 0 ELSE 1 END";
                result.SyncInstalled = Convert.ToInt32(await check.ExecuteScalarAsync()) == 1;
            }
            if (!result.SyncInstalled) return result;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT TOP 200 PackageId, Direction, FromVersion, ToVersion, RowsCount, Status, CreatedUtc, AppliedUtc, Error
                                FROM dbo.Sync_Package WHERE VesselID = @vesselId ORDER BY CreatedUtc DESC";
            var p = cmd.CreateParameter();
            p.ParameterName = "@vesselId";
            p.Value = vesselId;
            cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Packages.Add(new VesselSyncPackageDto
                {
                    PackageId = reader.GetGuid(0),
                    Direction = reader.GetString(1),
                    FromVersion = reader.GetInt64(2),
                    ToVersion = reader.GetInt64(3),
                    RowsCount = reader.GetInt32(4),
                    Status = reader.GetString(5),
                    CreatedUtc = reader.GetDateTime(6),
                    AppliedUtc = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    Error = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }
            return result;
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
    }

    private static HashSet<string> RankSet(IEnumerable<string?> ranks) =>
        new(ranks.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r!.Trim()), StringComparer.OrdinalIgnoreCase);

    private static VesselSyncItemDto Item(string type, long id, string? number, string? title, string? version, string? category,
                                          DateTime? issuedOn, HashSet<string> requiredRanks, IEnumerable<string?> readRanks, DateTime? lastRead)
    {
        var acknowledged = requiredRanks.Count == 0
            ? 0
            : RankSet(readRanks).Count(r => requiredRanks.Contains(r));
        return new VesselSyncItemDto
        {
            ItemType = type,
            ItemID = id,
            Number = number,
            Title = title,
            Version = version,
            Category = category,
            IssuedOn = issuedOn,
            RanksRequired = requiredRanks.Count,
            RanksAcknowledged = acknowledged,
            LastReadOn = lastRead,
            Status = requiredRanks.Count == 0 ? "Sent"
                   : acknowledged >= requiredRanks.Count ? "Acknowledged"
                   : acknowledged > 0 ? "Partly acknowledged"
                   : "Pending"
        };
    }
}
