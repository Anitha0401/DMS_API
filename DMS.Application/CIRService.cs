using AutoMapper;
using DMS.Application.Interface;
using DMS.Data.EF.Constant;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Query;
using MediatR;

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
                Label = m.Category ?? string.Empty,
                data = new CircularsTreeviewDataDto
                {
                    CIR_MasterID = m.CIR_MasterID,
                    CIR_BlobID = m.CIR_BlobId,
                    CIR_Number = m.CIR_Number,
                    StatusString = m.StatusString,
                    Category = m.Category,
                    DateIssued = m.DateIssued ?? string.Empty,
                    ParentID = m.ParentID,
                    CIRLevel = m.CIRLevel,
                    Reference = m.Reference,
                    IsActive = m.IsActive,
                    IsFavourite = m.IsFavourite,
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

    public async Task<List<CircularsNotificationDto>> GetCIRNotificationListAsync(int limit)
    {
        var data = await _queries.GetNotificationList();

        var notificationList = _mapper.Map<List<CircularsNotificationDto>>(data);

        return notificationList;
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