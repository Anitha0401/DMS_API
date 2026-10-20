using AutoMapper;
using DMS.Application.DTO.Circulars;
using DMS.Application.DTO.Features;
using DMS.Application.Interface;
using DMS.Data.EF;
using DMS.Data.EF.Constant;
using DMS.Data.EF.Context;
using DMS.Data.EF.Features;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Query.Circulars;
using DMS.Infrastructure.Query.Circulars;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Data;

namespace DMS.Application;

public class CIRService : ICIRService
{
    private static readonly string[] LinkTypes = { "Amends", "Supersedes", "RefersTo" };

    private const string LinkSelect = @"
SELECT l.CIR_ManualLinkID, l.CIR_MasterID, c.CIR_Number AS CircularNumber, c.Title AS CircularTitle, c.CircularType,
       c.Status AS CircularStatus, l.DM_ManualID, m.ManualNo, m.ManualName, l.DM_ManualVersionID, l.LinkType, l.Remarks,
       CAST(l.IsActive AS bit) AS IsActive, l.ClosedOn, l.CreatedOn
  FROM dbo.CIR_ManualLink l
  JOIN dbo.CIR_Master c ON c.CIR_MasterID = l.CIR_MasterID
  JOIN dbo.DM_Manual m ON m.DM_ManualID = l.DM_ManualID";

    private readonly CircularsContext _dbContext;
    private readonly IMediator _mediator;
    private readonly CIRGeneralQueries _queries;
    private readonly IMapper _mapper;

    public CIRService(CircularsContext context, IMediator mediator, CIRGeneralQueries queries, IMapper mapper)
    {
        _dbContext = context;
        _mediator = mediator;
        _queries = queries;
        _mapper = mapper;
    }

    public async Task<List<Circulars_TreeviewDto>> GetTreeViewCircularsListAsync(string userID, string circularType)
    {
        var cirList = await _queries.GetCIRTreeviewAsync(userID, circularType);

        if (cirList == null || !cirList.Any())
            throw new InvalidOperationException("No cir found in the database.");

        var uniqueList = cirList
           .GroupBy(m => new { m.CIR_MasterID, m.CIR_CategoryID })
           .Select(g => g.First())
           .ToList();

        var lookup = uniqueList.ToDictionary(
            m => $"{m.CIR_CategoryID}-{m.CIR_MasterID}",
            m => new Circulars_TreeviewDto
            {
                Key = $"{m.CIR_CategoryID}-{m.CIR_MasterID}",
                Label = (m.CIRLevel == 0 ? m.Category ?? string.Empty : m.CIR_Number + " - " + m.Title),
                data = new CircularsTreeviewDataDto
                {
                    CIR_MasterID = m.CIR_MasterID,
                    CIR_BlobID = m.CIR_BlobId,
                    CIR_Number = m.CIR_Number,
                    Title = m.Title,
                    StatusString = m.StatusString,
                    Category = m.Category,
                    DateIssued = m.DateIssued ?? string.Empty,
                    Priority = m.Priority,
                    ParentID = m.ParentID,
                    CIRLevel = m.CIRLevel,
                    Reference = m.Reference,
                    IsActive = m.IsActive,
                    IsFavourite = m.IsFavourite,
                    IsCategory = m.IsCategory,
                }
            });

        var root = new List<Circulars_TreeviewDto>();

        // Initialize children for each CIR
        foreach (var cir in uniqueList)
        {
            var currentKey = $"{cir.CIR_CategoryID}-{cir.CIR_MasterID}";

            if (cir.ParentID == -1)
            {
                if (lookup.TryGetValue(currentKey, out var node))
                    root.Add(node);
                continue;
            }

            var parentEntry = uniqueList.FirstOrDefault(u => u.CIR_CategoryID == cir.ParentID);
            if (parentEntry != null)
            {
                var parentKey = $"{parentEntry.CIR_CategoryID}-{parentEntry.CIR_MasterID}";

                if (lookup.TryGetValue(parentKey, out var parent))
                {
                    parent.Children ??= new List<Circulars_TreeviewDto>();

                    if (lookup.TryGetValue(currentKey, out var child))
                        parent.Children.Add(child);
                }
            }

        }

        return root;
    }

    public async Task<List<CIRMasterList>> GetCIRMasterListAsync(string userID, long categoryId, string circularType)
    {
        var cirList = await _queries.GetCIRListAsync(userID, categoryId, circularType);

        return cirList;
    }

    public async Task<List<CircularCategoryDto>> GetCategoriesAsync(bool isToIncludeAll, string? circularType = null)
    {
        var query = _dbContext.CIR_Category
            .Where(c =>
                (c.isActive ?? 0) == 1 &&
                c.CircularType != null &&
                c.CircularType != string.Empty);

        if (!string.IsNullOrWhiteSpace(circularType))
        {
            var normalizedCircularType = circularType.Trim();

            query = query.Where(c =>
                c.CircularType == normalizedCircularType);
        }

        var categories = await query
            .OrderBy(c => c.Sortorder)
            .ThenBy(c => c.Category)
            .Select(c => new CircularCategoryDto
            {
                CIR_CategoryID = c.CIR_CategoryID,
                Category = c.Category ?? string.Empty
            })
            .ToListAsync();

        if (isToIncludeAll &&
            !categories.Any(c => c.CIR_CategoryID == -1))
        {
            categories.Insert(0, new CircularCategoryDto
            {
                CIR_CategoryID = -1,
                Category = "All Categories"
            });
        }

        return categories;
    }

    public async Task<List<CircularsChart>> GetCIR_CategoryChartDataAsync(string circularType)
    {
        var data = await _queries.GetCIRCategoryBasedChartDataAsync(circularType);
        return data;
    }

    public async Task<List<CIRRecentList>> GetRecentCircularsAsync(string circularType, int limit = 10)
    {
        var data = await _queries.GetRecentCircularsAsync(circularType, limit);
        return data;
    }

    public async Task<CircularCount_Dashboard> GetDashboardCircularCountsAsync()
    {
        var counts = await _queries.GetCIRDashboardCountsAsync();
        return counts;
    }

    public async Task<List<CircularsNotificationDto>> GetCIRNotificationListAsync(int limit, string userID)
    {
        var data = await _queries.GetNotificationList(userID, limit);

        var notificationList = _mapper.Map<List<CircularsNotificationDto>>(data);

        // Read/unread is kept per user (Notification_Read); before that table exists, the shared flag is shown.
        if (await NotificationReads.IsInstalledAsync(_dbContext))
        {
            var read = await NotificationReads.ReadIdsAsync(_dbContext, NotificationReads.Circular, userID);
            foreach (var n in notificationList)
                n.IsRead = read.Contains(n.CIRNotificationId) ? 1 : 0;
        }

        return notificationList;
    }

    public async Task<List<CircularUserAckDto>> GetVesselAckListAsync(long cir_MasterID)
    {
        var data = await _queries.GetVesselAckListAsync(cir_MasterID);
        return data;
    }

    public async Task<GetNextCIRNumberResponseDto> GetNextCIRNumbersByCategoryAsync(long categoryId, long cir_MasterID)
    {
        // Validate category
        var cat = await _dbContext.CIR_Category
                   .Where(c => c.CIR_CategoryID == categoryId)
                   .Select(c => new { CategoryCode = c.CategoryCode ?? string.Empty, CircularType = c.CircularType ?? string.Empty })
                   .FirstOrDefaultAsync();

        if (cat == null)
            throw new KeyNotFoundException($"Category with ID {categoryId} not found.");

        var year = DateTime.UtcNow.Year;
        var circularTypePrefix = cat.CircularType == "Circulars" ? "CIR" : "ALT";
        var prefix = $"{circularTypePrefix}-{cat.CategoryCode}-{year}-";

        // Get existing numbers with the same prefix
        var existingNumbers = await _dbContext.CIR_Master
                                    .Where(m => m.CIR_CategoryID == categoryId
                                             && m.CIR_Number != null
                                             && Microsoft.EntityFrameworkCore.EF.Functions.Like(m.CIR_Number!, $"{prefix}%"))
                                    .Select(m => m.CIR_Number!)
                                    .ToListAsync();

        // Find max sequence used
        var maxSeq = 0;
        foreach (var num in existingNumbers)
        {
            var parts = num.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4)
            {
                var last = parts[^1];
                if (int.TryParse(last, out var seq))
                    maxSeq = Math.Max(maxSeq, seq);
            }
        }

        var nextSeq = maxSeq + 1;
        var cir_Number = $"{prefix}{nextSeq:D4}";

        var allowedUserToAck = await _dbContext.CIR_CategoryExt
                                       .Where(c => c.CIR_CategoryID == categoryId)
                                       .Select(c => c.AllowedUsersToAck)
                                       .FirstOrDefaultAsync();

        var cir_Master = await _dbContext.CIR_Master
                              .Where(m => m.CIR_MasterID == cir_MasterID)
                              .OrderByDescending(m => m.CIR_MasterID)
                              .FirstOrDefaultAsync();
        if (cir_Master != null)
        {
            cir_Master.CIR_Number = cir_Number;
            cir_Master.CIR_CategoryID = categoryId;
            _dbContext.CIR_Master.Update(cir_Master);
            await _dbContext.SaveChangesAsync();
        }

        return new GetNextCIRNumberResponseDto
        {
            CirNumber = cir_Number,
            AllowedUserToAck = allowedUserToAck ?? string.Empty
        };
    }

    public async Task<EditCircularDto> GetCIRDetailsAsync(long cirMasterId)
    {
        var query = from cir in _dbContext.VwCIRMasterBlob
                    where cir.CIR_MasterID == cirMasterId
                    select new
                    {
                        Circular = cir,
                    };

        var result = await query.FirstOrDefaultAsync();

        if (result?.Circular == null)
        {
            throw new KeyNotFoundException($"Circular with ID {cirMasterId} not found.");
        }

        var circularDto = _mapper.Map<EditCircularDto>(result.Circular);
        var toAckRows = await _dbContext.CIR_ToAck_Vsl
                            .Where(x =>
                                x.CIR_MasterID == cirMasterId &&
                                x.IsActive == 1)
                            .ToListAsync();

        circularDto.VesselIdList = toAckRows
            .Select(x => x.VesselID)
            .Distinct()
            .ToList();

        circularDto.AllowedUserToAck = toAckRows
            .Select(x => x.UserRank)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return circularDto;
    }

    public async Task<List<CIR_LogDto>> GetCircularLogsAsync(long cirMasterId)
    {
        var logs = await _dbContext.CIR_Log
                            .Where(l => l.CIR_MasterID == cirMasterId)
                            .OrderByDescending(l => l.LogDate)
                            .Select(l => new CIR_LogDto
                            {
                                CIR_LogID = l.CIR_LogID,
                                CIR_MasterID = l.CIR_MasterID,
                                Logdate = l.LogDate,
                                UserId = l.UserId,
                                UserName = l.UserName,
                                Activity = l.Activity,
                                Remarks = l.Remarks,
                            })
                            .ToListAsync();
        return logs;
    }


    public async Task<long> InsertCIR_MasterRecord(string circularType)
    {
        try
        {
            var cir_Master = new CIR_Master();
            cir_Master.CIR_MasterID = await _dbContext.NextIdAsync<CIR_Master>();
            cir_Master.Origin = "HQ";
            cir_Master.VesselID = -1;
            cir_Master.Status = "Temp";
            cir_Master.CIR_Number = "Temp";
            cir_Master.CIR_CategoryID = -1;
            cir_Master.CircularType = circularType;
            _dbContext.CIR_Master.Add(cir_Master);
            await _dbContext.SaveChangesAsync();

            return cir_Master.CIR_MasterID;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task RemoveTempCIRAsync(long cir_MasterID)
    {
        try
        {
            var cir_Master = await _dbContext.CIR_Master
                                  .Where(m => m.CIR_MasterID == cir_MasterID)
                                  .OrderByDescending(m => m.CIR_MasterID)
                                  .FirstOrDefaultAsync();
            if (cir_Master != null)
            {
                _dbContext.CIR_Master.Remove(cir_Master);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception)
        { 
            throw;
        }
    }

    public async Task<long> CreateCircularAsync(CreateCircularDto createDto)
    {
        if (createDto is null)
            throw new ArgumentNullException(nameof(createDto));

        var result = await _mediator.Send(new CreateCircularsCommand(createDto));

        return result;
    }


    public async Task AddToFavouriteAsync(long cir_MasterID, string circularType, string userID)
    {
        try
        {
            if (!_dbContext.CIR_UserFavourite.Any(f => f.CIR_MasterID == cir_MasterID && f.UserId == userID))
            {
                var nextCIR_UserFavouriteID = await _dbContext.NextIdAsync<CIR_UserFavourite>();

                var userFavourite = new CIR_UserFavourite();
                userFavourite.CIR_UserFavouriteID = nextCIR_UserFavouriteID;
                userFavourite.Origin = "HQ";
                userFavourite.VesselID = -1;
                userFavourite.CIR_MasterID = cir_MasterID;
                userFavourite.UserId = userID;
                userFavourite.IsDisabled = 0;
                await _dbContext.CIR_UserFavourite.AddAsync(userFavourite);

                await _dbContext.SaveChangesAsync();

                await _queries.AddNotification(cir_MasterID, "", ManualNotificationType.FAVOURITE, circularType + " added as Favourite for user - " + userID, "");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task RemoveFromFavouriteAsync(long cir_MasterID, string userID)
    {
        var favourite = await _dbContext.CIR_UserFavourite
            .FirstOrDefaultAsync(f => f.CIR_MasterID == cir_MasterID && f.UserId == userID && (f.IsDisabled ?? 0) == 0);

        if (favourite == null)
            return;

        favourite.IsDisabled = 1;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<long> UpdateCircularAsync(EditCircularDto editDto)
    {
        if (editDto is null)
            throw new ArgumentNullException(nameof(editDto));

        var result = await _mediator.Send(new UpdateCircularsCommand(editDto));

        return result;
    }

    public async Task<bool> MarkNotificationReadAsync(long notificationID, string userID)
    {
        var notification = await _dbContext.CIR_Notification
                                .FirstOrDefaultAsync(n => n.CIR_NotificationID == notificationID);
        if (notification == null) return false;

        if (await NotificationReads.IsInstalledAsync(_dbContext))
        {
            // Read for this user only; others still see it as new.
            await NotificationReads.MarkReadAsync(_dbContext, NotificationReads.Circular, notificationID, userID);
            return true;
        }

        notification.IsRead = 1;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<CircularExtras?> GetCircularesExtraInfoAsync(long circularId)
    {
        // Read using the DbContext connection and map to the DTOs directly (avoids dependency on SqlDb here).
        CircularExtras? extras = null;
        var links = new List<CircularManualLink>();

        var sql = @"SELECT c.CIR_MasterID, c.Status, c.AckDueDays, c.ValidUntil, c.SupersededByID, s.CIR_Number AS SupersededByNumber,
       CAST(ISNULL(cat.RequiresValidUntil, 0) AS bit) AS RequiresValidUntil
  FROM dbo.CIR_Master c
  LEFT JOIN dbo.CIR_Master s ON s.CIR_MasterID = c.SupersededByID
  LEFT JOIN dbo.CIR_Category cat ON cat.CIR_CategoryID = c.CIR_CategoryID
 WHERE c.CIR_MasterID = @id";

        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            var p = cmd.CreateParameter();
            p.ParameterName = "@id";
            p.Value = circularId;
            cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                extras = new CircularExtras
                {
                    CIR_MasterID = reader.GetInt64(reader.GetOrdinal("CIR_MasterID")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
                    AckDueDays = reader.IsDBNull(reader.GetOrdinal("AckDueDays")) ? null : reader.GetInt32(reader.GetOrdinal("AckDueDays")),
                    ValidUntil = reader.IsDBNull(reader.GetOrdinal("ValidUntil")) ? null : reader.GetDateTime(reader.GetOrdinal("ValidUntil")),
                    SupersededByID = reader.IsDBNull(reader.GetOrdinal("SupersededByID")) ? null : reader.GetInt64(reader.GetOrdinal("SupersededByID")),
                    SupersededByNumber = reader.IsDBNull(reader.GetOrdinal("SupersededByNumber")) ? null : reader.GetString(reader.GetOrdinal("SupersededByNumber")),
                    RequiresValidUntil = !reader.IsDBNull(reader.GetOrdinal("RequiresValidUntil")) && reader.GetBoolean(reader.GetOrdinal("RequiresValidUntil")),
                };
            }

            await reader.CloseAsync();

            using var cmd2 = connection.CreateCommand();
            cmd2.CommandText = LinkSelect + " WHERE l.CIR_MasterID = @id ORDER BY l.IsActive DESC, m.ManualNo";
            var p2 = cmd2.CreateParameter();
            p2.ParameterName = "@id";
            p2.Value = circularId;
            cmd2.Parameters.Add(p2);

            using var reader2 = await cmd2.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                var link = new CircularManualLink
                {
                    CIR_ManualLinkID = reader2.GetInt64(reader2.GetOrdinal("CIR_ManualLinkID")),
                    CIR_MasterID = reader2.GetInt64(reader2.GetOrdinal("CIR_MasterID")),
                    CircularNumber = reader2.IsDBNull(reader2.GetOrdinal("CircularNumber")) ? null : reader2.GetString(reader2.GetOrdinal("CircularNumber")),
                    CircularTitle = reader2.IsDBNull(reader2.GetOrdinal("CircularTitle")) ? null : reader2.GetString(reader2.GetOrdinal("CircularTitle")),
                    CircularType = reader2.IsDBNull(reader2.GetOrdinal("CircularType")) ? null : reader2.GetString(reader2.GetOrdinal("CircularType")),
                    CircularStatus = reader2.IsDBNull(reader2.GetOrdinal("CircularStatus")) ? null : reader2.GetString(reader2.GetOrdinal("CircularStatus")),
                    DM_ManualID = reader2.GetInt64(reader2.GetOrdinal("DM_ManualID")),
                    ManualNo = reader2.IsDBNull(reader2.GetOrdinal("ManualNo")) ? null : reader2.GetString(reader2.GetOrdinal("ManualNo")),
                    ManualName = reader2.IsDBNull(reader2.GetOrdinal("ManualName")) ? null : reader2.GetString(reader2.GetOrdinal("ManualName")),
                    DM_ManualVersionID = reader2.IsDBNull(reader2.GetOrdinal("DM_ManualVersionID")) ? null : reader2.GetInt64(reader2.GetOrdinal("DM_ManualVersionID")),
                    LinkType = reader2.IsDBNull(reader2.GetOrdinal("LinkType")) ? string.Empty : reader2.GetString(reader2.GetOrdinal("LinkType")),
                    Remarks = reader2.IsDBNull(reader2.GetOrdinal("Remarks")) ? null : reader2.GetString(reader2.GetOrdinal("Remarks")),
                    IsActive = !reader2.IsDBNull(reader2.GetOrdinal("IsActive")) && reader2.GetBoolean(reader2.GetOrdinal("IsActive")),
                    ClosedOn = reader2.IsDBNull(reader2.GetOrdinal("ClosedOn")) ? null : reader2.GetDateTime(reader2.GetOrdinal("ClosedOn")),
                    CreatedOn = reader2.IsDBNull(reader2.GetOrdinal("CreatedOn")) ? null : reader2.GetDateTime(reader2.GetOrdinal("CreatedOn"))
                };
                links.Add(link);
            }
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }

        if (extras == null) return null;
        extras.Links = links;
        return extras;
    }

    public async Task<string?> SaveCircularesExtraInfoAsync(long circularId, SaveCircularExtras request)
    {
        var current = await GetCircularesExtraInfoAsync(circularId);
        if (current == null) return "Circular not found.";
        if (request.AckDueDays is < 0 or > 3650) return "Acknowledge-by days must be between 0 and 3650.";
        if (current.RequiresValidUntil && request.ValidUntil == null) return "This category needs a \"valid until\" date.";
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CIR_Master SET AckDueDays = {request.AckDueDays}, ValidUntil = {request.ValidUntil?.Date} WHERE CIR_MasterID = {circularId}");
        return null;
    }

    public async Task<string?> AddManualLinkAsync(long circularId, AddManualLink request, OfficeUser user)
    {
        if (!LinkTypes.Contains(request.LinkType)) return "Link type must be Amends, Supersedes or RefersTo.";
        // Verify circular exists
        if (!await _dbContext.CIR_Master.AnyAsync(m => m.CIR_MasterID == circularId))
            return "Circular not found.";

        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT 1 FROM dbo.DM_Manual WHERE DM_ManualID = @id AND ISNULL(IsDeleted, 0) <> 1";
            var cp = checkCmd.CreateParameter(); cp.ParameterName = "@id"; cp.Value = request.ManualId; checkCmd.Parameters.Add(cp);
            var found = await checkCmd.ExecuteScalarAsync();
            if (found == null) return "Manual not found.";

            using var checkCmd2 = connection.CreateCommand();
            checkCmd2.CommandText = "SELECT 1 FROM dbo.CIR_ManualLink WHERE CIR_MasterID = @c AND DM_ManualID = @m AND IsActive = 1";
            var p1 = checkCmd2.CreateParameter(); p1.ParameterName = "@c"; p1.Value = circularId; checkCmd2.Parameters.Add(p1);
            var p2 = checkCmd2.CreateParameter(); p2.ParameterName = "@m"; p2.Value = request.ManualId; checkCmd2.Parameters.Add(p2);
            var exists = await checkCmd2.ExecuteScalarAsync();
            if (exists != null) return "This manual is already linked.";

            var id = await _dbContext.NextIdAsync("CIR_ManualLink");

            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"INSERT dbo.CIR_ManualLink (CIR_ManualLinkID, Origin, VesselID, CIR_MasterID, DM_ManualID, DM_ManualVersionID, LinkType, Remarks, IsActive, CreatedBy, CreatedOn)
VALUES (@id, 'HQ', -1, @c, @m, @v, @t, @r, 1, @by, GETUTCDATE())";
            var ip = insertCmd.CreateParameter(); ip.ParameterName = "@id"; ip.Value = id; insertCmd.Parameters.Add(ip);
            ip = insertCmd.CreateParameter(); ip.ParameterName = "@c"; ip.Value = circularId; insertCmd.Parameters.Add(ip);
            ip = insertCmd.CreateParameter(); ip.ParameterName = "@m"; ip.Value = request.ManualId; insertCmd.Parameters.Add(ip);
            ip = insertCmd.CreateParameter(); ip.ParameterName = "@v"; ip.Value = request.ManualVersionId is null ? (object)DBNull.Value : request.ManualVersionId; insertCmd.Parameters.Add(ip);
            ip = insertCmd.CreateParameter(); ip.ParameterName = "@t"; ip.Value = request.LinkType; insertCmd.Parameters.Add(ip);
            ip = insertCmd.CreateParameter(); ip.ParameterName = "@r"; ip.Value = (object?)Cut(request.Remarks, 500) ?? DBNull.Value; insertCmd.Parameters.Add(ip);
            ip = insertCmd.CreateParameter(); ip.ParameterName = "@by"; ip.Value = user.UserId; insertCmd.Parameters.Add(ip);
            await insertCmd.ExecuteNonQueryAsync();
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
        return null;
    }

    public Task RemoveManualLinkAsync(long linkId)
        => _dbContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.CIR_ManualLink WHERE CIR_ManualLinkID = {linkId}");

    public async Task<string?> CancelCircularAsync(long circularId, CancelCircular request, OfficeUser user)
    {
        var current = await _dbContext.CIR_Master.FirstOrDefaultAsync(m => m.CIR_MasterID == circularId);
        if (current == null) return "Circular not found.";
        var status = current.Status;
        if (status == "Cancelled") return "This circular is already cancelled.";
        if (status is "Temp" or "Draft") return "A draft cannot be cancelled; delete it instead.";
        if (request.SupersededById == circularId) return "A circular cannot supersede itself.";
        if (request.SupersededById != null && !await _dbContext.CIR_Master.AnyAsync(m => m.CIR_MasterID == request.SupersededById && (m.IsDeleted ?? 0) != 1))
            return "The superseding circular was not found.";
        var remarks = string.IsNullOrWhiteSpace(request.Reason) ? null : $"Cancelled by {user.UserName}: {request.Reason.Trim()}";

        current.Status = "Cancelled";
        // Use SQL update because SupersededByID is not exposed on the EF entity mapping used here.
        // current.SupersededByID = request.SupersededById;
        current.ModifiedBy = user.UserId;
        current.ModifiedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            var combined = (current.Remarks ?? string.Empty) + (string.IsNullOrEmpty(current.Remarks) ? string.Empty : "\r\n") + remarks;
            current.Remarks = combined.Length <= 4000 ? combined : combined.Substring(0, 4000);
        }
        await _dbContext.SaveChangesAsync();
        return null;
    }

    public async Task<List<CircularManualLink>> GetForManualAsync(long manualId)
    {
        var result = new List<CircularManualLink>();
        var sql = LinkSelect + @" WHERE l.DM_ManualID = @id AND ISNULL(c.IsDeleted, 0) <> 1 AND ISNULL(c.Status, '') NOT IN ('Temp', 'Draft')
 ORDER BY l.IsActive DESC, COALESCE(c.ReleasedDate, c.DateIssued) DESC";
        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = manualId; cmd.Parameters.Add(p);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var link = new CircularManualLink
                {
                    CIR_ManualLinkID = reader.GetInt64(reader.GetOrdinal("CIR_ManualLinkID")),
                    CIR_MasterID = reader.GetInt64(reader.GetOrdinal("CIR_MasterID")),
                    CircularNumber = reader.IsDBNull(reader.GetOrdinal("CircularNumber")) ? null : reader.GetString(reader.GetOrdinal("CircularNumber")),
                    CircularTitle = reader.IsDBNull(reader.GetOrdinal("CircularTitle")) ? null : reader.GetString(reader.GetOrdinal("CircularTitle")),
                    CircularType = reader.IsDBNull(reader.GetOrdinal("CircularType")) ? null : reader.GetString(reader.GetOrdinal("CircularType")),
                    CircularStatus = reader.IsDBNull(reader.GetOrdinal("CircularStatus")) ? null : reader.GetString(reader.GetOrdinal("CircularStatus")),
                    DM_ManualID = reader.GetInt64(reader.GetOrdinal("DM_ManualID")),
                    ManualNo = reader.IsDBNull(reader.GetOrdinal("ManualNo")) ? null : reader.GetString(reader.GetOrdinal("ManualNo")),
                    ManualName = reader.IsDBNull(reader.GetOrdinal("ManualName")) ? null : reader.GetString(reader.GetOrdinal("ManualName")),
                    DM_ManualVersionID = reader.IsDBNull(reader.GetOrdinal("DM_ManualVersionID")) ? null : reader.GetInt64(reader.GetOrdinal("DM_ManualVersionID")),
                    LinkType = reader.IsDBNull(reader.GetOrdinal("LinkType")) ? string.Empty : reader.GetString(reader.GetOrdinal("LinkType")),
                    Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? null : reader.GetString(reader.GetOrdinal("Remarks")),
                    IsActive = !reader.IsDBNull(reader.GetOrdinal("IsActive")) && reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    ClosedOn = reader.IsDBNull(reader.GetOrdinal("ClosedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("ClosedOn")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("CreatedOn"))
                };
                result.Add(link);
            }
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
        return result;
    }

    public Task<int> CloseLinksForManualAsync(long manualId)
        => _dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CIR_ManualLink SET IsActive = 0, ClosedOn = GETUTCDATE()\n                              WHERE DM_ManualID = {manualId} AND IsActive = 1 AND LinkType IN ('Amends', 'Supersedes')");

    public async Task<List<CircularCategorySetting>> GetCategoriesAsync()
    {
        var result = new List<CircularCategorySetting>();
        var sql = @"SELECT CIR_CategoryID, CircularType, CategoryCode, Category, CAST(ISNULL(RequiresValidUntil, 0) AS bit) AS RequiresValidUntil
  FROM dbo.CIR_Category WHERE ISNULL(isActive, 1) = 1 ORDER BY CircularType, Sortorder, Category";
        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new CircularCategorySetting
                {
                    CIR_CategoryID = reader.GetInt64(reader.GetOrdinal("CIR_CategoryID")),
                    CircularType = reader.IsDBNull(reader.GetOrdinal("CircularType")) ? null : reader.GetString(reader.GetOrdinal("CircularType")),
                    CategoryCode = reader.IsDBNull(reader.GetOrdinal("CategoryCode")) ? null : reader.GetString(reader.GetOrdinal("CategoryCode")),
                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                    RequiresValidUntil = !reader.IsDBNull(reader.GetOrdinal("RequiresValidUntil")) && reader.GetBoolean(reader.GetOrdinal("RequiresValidUntil"))
                });
            }
        }
        finally { if (openedHere) await connection.CloseAsync(); }
        return result;
    }

    public Task SaveCategoryAsync(long categoryId, bool requiresValidUntil)
        => _dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CIR_Category SET RequiresValidUntil = {(requiresValidUntil ? 1 : 0)} WHERE CIR_CategoryID = {categoryId}");

    public async Task<ManualExtras?> GetManualAsync(long manualId)
    {
        ManualExtras? result = null;
        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DM_ManualID, AckDueDays FROM dbo.DM_Manual WHERE DM_ManualID = @id";
            var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = manualId; cmd.Parameters.Add(p);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                result = new ManualExtras
                {
                    DM_ManualID = reader.GetInt64(reader.GetOrdinal("DM_ManualID")),
                    AckDueDays = reader.IsDBNull(reader.GetOrdinal("AckDueDays")) ? null : reader.GetInt32(reader.GetOrdinal("AckDueDays"))
                };
            }
        }
        finally { if (openedHere) await connection.CloseAsync(); }
        return result;
    }

    public Task SaveManualAsync(long manualId, int? ackDueDays)
        => _dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.DM_Manual SET AckDueDays = {(ackDueDays is null ? (int?)null : Math.Clamp(ackDueDays.Value, 0, 3650))} WHERE DM_ManualID = {manualId}");

    private static string? Cut(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Length <= max ? s.Trim() : s.Trim()[..max];


}
