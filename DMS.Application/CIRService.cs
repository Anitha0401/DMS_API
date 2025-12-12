using AutoMapper;
using DMS.Application.Interface;
using DMS.Data.EF.Constant;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.DTO;
using DMS.Data.EF.Models.Manuals;
using DMS.Data.EF.Query;
using DMS.Data.EF.Query.Manuals;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

        // Initialize children for each manual
        foreach (var cir in uniqueList)
        {
            var currentKey = $"{cir.CIR_CategoryID}-{cir.CIR_MasterID}";

            if (cir.ParentID == -1) // Assuming 0 is the root parent ID
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
        IQueryable<CIR_Category> query = _dbContext.CIR_Category;

        if (!string.IsNullOrWhiteSpace(circularType))
            query = query.Where(c => c.CircularType == circularType);

        query = query.Where(c => (c.isActive ?? 0) == 1);

        var categories = await query
                        .OrderBy(c => c.Sortorder)
                        .ThenBy(c => c.Category)
                        .Select(c => new CircularCategoryDto
                        {
                            CIR_CategoryID = c.CIR_CategoryID,
                            Category = c.Category ?? string.Empty
                        })
                        .ToListAsync();

        if (isToIncludeAll && !categories.Any(c => c.CIR_CategoryID == -1))
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


    public async Task<string> GetNextCIRNumbersByCategoryAsync(long categoryId)
    {
        var cir_Number = await _queries.GetNextCIRNumbersByCategoryAsync(categoryId);

        return cir_Number;
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
            // Log or rethrow for debugging
            throw new Exception("Failed to add to favourites", ex);
        }
    }
}