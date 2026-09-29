using AutoMapper;
using DMS.Application.DTO.Circulars;
using DMS.Application.Interface;
using DMS.Data.EF.Constant;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Query.Circulars;
using DMS.Infrastructure.Query.Circulars;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data.Entity;
using System.Threading;

namespace DMS.Application;

public class CIRService : ICIRService
{
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

    public async Task<List<CircularsNotificationDto>> GetCIRNotificationListAsync(int limit)
    {
        var data = await _queries.GetNotificationList();

        var notificationList = _mapper.Map<List<CircularsNotificationDto>>(data);

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
            throw new InvalidOperationException($"Category with ID {categoryId} not found.");

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
            throw new Exception($"Circular with ID {cirMasterId} not found.");
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
            cir_Master.CIR_MasterID = (_dbContext.CIR_Master
                                     .Select(m => (long?)m.CIR_MasterID)
                                     .Max() ?? 0) + 1;
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
        catch (Exception ex)
        {
            throw new Exception("Failed to insert CIR_Master record", ex);
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
        catch (Exception ex)
        { 
            throw new Exception("Failed to remove CIR_Master record", ex);
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
                var nextCIR_UserFavouriteID = _dbContext.CIR_UserFavourite
                                                  .Select(m => (long?)m.CIR_UserFavouriteID)
                                                  .Max() ?? 0;

                nextCIR_UserFavouriteID = nextCIR_UserFavouriteID + 1;

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
        catch (Exception ex)
        {
            throw new Exception("Failed to add to favourites", ex);
        }
    }

    public async Task<long> UpdateCircularAsync(EditCircularDto editDto)
    {
        if (editDto is null)
            throw new ArgumentNullException(nameof(editDto));

        var result = await _mediator.Send(new UpdateCircularsCommand(editDto));

        return result;
    }
}