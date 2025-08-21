using Microsoft.EntityFrameworkCore;
using DMS.Application.Interface;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;
using DMS.Data.EF.DTO;

namespace DMS.Application;

public class DMSService : IDMSService
{
    private readonly DmsContext _dbContext;
    private readonly IMediator _mediator;
    private readonly GeneralQueries _queries;

    public DMSService(DmsContext context, IMediator mediator, GeneralQueries queries)
    {
        _dbContext = context;
        _mediator = mediator;
        _queries = queries;
    }

    public async Task<List<Manual_TreeviewDto>> GetTreeViewManualListAsync()
    {
        var manualList = await _queries.GetDmManualTreeviewAsync();

        if (manualList == null || !manualList.Any())
            throw new InvalidOperationException("No manuals found in the database.");

        var lookup = manualList.ToDictionary(m => m.DM_ManualID, m => new Manual_TreeviewDto
        {
            Key = m.DM_ManualID,
            Label = m.ManualNo + " - " + m.ManualName,
            data = new TreeviewDataDto
            {
                DM_ManualID = m.DM_ManualID,
                DM_ManualVersionID = m.DM_ManualVersionID,
                Version = m.Version,
                ParentID = m.ParentID,
                ManualLevel = m.ManualLevel
            }
        });

        var root = new List<Manual_TreeviewDto>();

        // Initialize children for each manual
        foreach (var manual in manualList)
        {
            if (manual.ParentID == -1) // Assuming 0 is the root parent ID
            {
                root.Add(lookup[manual.DM_ManualID]);
            }
            else if (lookup.ContainsKey(manual.ParentID))
            {
                var parent = lookup[manual.ParentID];
                if (parent.Children == null)
                {
                    parent.Children = new List<Manual_TreeviewDto>();
                }
                parent.Children.Add(lookup[manual.DM_ManualID]);
            }
        }

        return root;
    }

    public async Task<string?> GetManualContent(long dm_ManualID, bool includeSubManuals)
    {
        return await _queries.GetDmManualContentAsync(dm_ManualID, includeSubManuals);
    }
    public async Task<ManualContentDto?> GetManualDetails(long dm_ManualID)
    {
        var categoryList = _dbContext.Dm_Category
                                     .Where(x => x.IsDisabled == 0)
                                     .ToList(); // Ensure the query is executed and materialized into a list

        var manual = await _dbContext.DmManuals
                                .Where(x => x.DM_ManualID == dm_ManualID)
                                .Select(x => new ManualContentDto
                                {
                                    DM_ManualID = x.DM_ManualID,
                                    ManualCode = x.ManualCode ?? string.Empty,
                                    ManualName = x.ManualName,
                                    ManualNo = x.ManualNo ?? string.Empty,
                                    DM_CategoryID = x.DM_CategoryID,
                                    CategoryName = _dbContext.Dm_Category
                                                            .Where(v => v.DM_CategoryID == x.DM_CategoryID)
                                                            .Select(v => v.CategoryName)
                                                            .FirstOrDefault() ?? string.Empty,
                                    TextContents = _dbContext.DmManualVersion
                                                            .Where(v => v.DM_ManualID == x.DM_ManualID && v.IsActive == 1 && v.IsCurrent == 1)
                                                            .Select(v => v.TextContents)
                                                            .FirstOrDefault() ?? string.Empty
                                })
                                .FirstOrDefaultAsync();

        return manual;
    }

    public async Task<List<Dm_ManualVersion_View>> GetDmManualsAsync()
    {
        var data = await (from manual in _dbContext.DmManuals
                          join manualVersion in _dbContext.DmManualVersion
                         on manual.VesselID equals manualVersion.VesselID
                          where manualVersion.IsActive == 1 && manualVersion.IsCurrent == 1
                        && manual.IsDeleted == 0
                          select new Dm_ManualVersion_View()
                          {
                              DM_ManualID = manual.DM_ManualID,
                              Origin = manual.Origin,
                              VesselID = manual.VesselID,
                              Version = manualVersion.Version,
                              ManualCode = manual.ManualCode,
                              ManualName = manual.ManualName,
                              ManualNo = manual.ManualNo,
                              DM_CategoryID = manual.DM_CategoryID,
                              DM_StatusID = manual.DM_StatusID,
                              CanExport = manual.CanExport,
                              ManualLevel = manual.ManualLevel,
                              SortOrder = manual.SortOrder,
                              TextContents = manualVersion.TextContents,
                              ApprovedBy = manualVersion.ApprovedBy,
                              ApprovedDate = manualVersion.ApprovedDate,
                              ReleasedBy = manualVersion.ReleasedBy,
                              ReleasedDate = manualVersion.ReleasedDate,
                              Comments = manualVersion.Comments,
                              FinalisedBy = manualVersion.FinalisedBy,
                              FinalisedDate = manualVersion.FinalisedDate,
                          }).ToListAsync();

        return data;
    }


    public async Task<List<ManualCategoryDto>> GetManualCategory()
    {
        var categoryList = await _dbContext.Dm_Category
                                   .Where(x => x.IsDisabled == 0)
                                   .Select(x => new ManualCategoryDto
                                   {
                                       DM_CategoryID = x.DM_CategoryID,
                                       CategoryName = x.CategoryName
                                   }).ToListAsync();
        return categoryList;
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

    public async Task<List<VslRankListDto>> GetVslRankList()
    {
        var vslRankList = await _dbContext.VslStaffs
                                     .Select(x => new VslRankListDto
                                     {
                                         userRank = x.UserRank,
                                         userRankDescription = x.UserRank
                                     }).ToListAsync();
        return vslRankList;
    }

    public async Task<List<DM_Manual_Attachments>> GetManualAttachmentListAsync(long dm_ManualID)
    {
        var attachments = await _dbContext.DmManualAttachments
                             .Where(a => a.DM_ManualID == dm_ManualID)
                             .ToListAsync();

        return attachments;
    }

    public async Task<List<Dm_ManualVersion_View>> GetDmManualsAsyncx()
    {
        // Pseudocode:
        // 1. For each manual, get the latest (highest DM_ManualVersionID) version that is active and current.
        // 2. Join DmManuals with DmManualVersion on DM_ManualID.
        // 3. Filter for IsActive == 1, IsCurrent == 1, IsDeleted == 0.
        // 4. For each manual, select only the version with the highest DM_ManualVersionID.
        // 5. Project to Dm_ManualVersion_View and return as list.

        var latestVersions = _dbContext.DmManualVersion
            .Where(v => v.IsActive == 1 && v.IsCurrent == 1)
            .GroupBy(v => v.DM_ManualID)
            .Select(g => g.OrderByDescending(v => v.DM_ManualVersionID).First().DM_ManualVersionID);

        var data = await (from manual in _dbContext.DmManuals
                          join manualVersion in _dbContext.DmManualVersion
                              on manual.DM_ManualID equals manualVersion.DM_ManualID
                          where manualVersion.IsActive == 1
                                && manualVersion.IsCurrent == 1
                                && manual.IsDeleted == 0
                                && latestVersions.Contains(manualVersion.DM_ManualVersionID)
                          select new Dm_ManualVersion_View()
                          {
                              DM_ManualID = manual.DM_ManualID,
                              Origin = manual.Origin,
                              VesselID = manual.VesselID,
                              Version = manualVersion.Version,
                              ManualCode = manual.ManualCode,
                              ManualName = manual.ManualName,
                              ManualNo = manual.ManualNo,
                              DM_CategoryID = manual.DM_CategoryID,
                              DM_StatusID = manual.DM_StatusID,
                              CanExport = manual.CanExport,
                              ManualLevel = manual.ManualLevel,
                              SortOrder = manual.SortOrder,
                              TextContents = manualVersion.TextContents,
                              ApprovedBy = manualVersion.ApprovedBy,
                              ApprovedDate = manualVersion.ApprovedDate,
                              ReleasedBy = manualVersion.ReleasedBy,
                              ReleasedDate = manualVersion.ReleasedDate,
                              Comments = manualVersion.Comments,
                              FinalisedBy = manualVersion.FinalisedBy,
                              FinalisedDate = manualVersion.FinalisedDate,
                          }).ToListAsync();

        return data;
    }

    public async Task<Dm_ManualDetails> GetDmManualDetailsAsync(long dm_ManualID)
    {
        var data = await (from manual in _dbContext.DmManuals
                          join manualVersion in _dbContext.DmManualVersion
                               on manual.DM_ManualID equals manualVersion.DM_ManualID
                          join status in _dbContext.Dm_Status
                               on manual.DM_StatusID equals status.DM_StatusID
                          where manualVersion.IsActive == 1 && manualVersion.IsCurrent == 1
                              && manual.DM_ManualID == dm_ManualID
                          select new Dm_ManualDetails()
                          {
                              DM_ManualID = manual.DM_ManualID,
                              DM_ManualVersionID = manualVersion.DM_ManualVersionID,
                              VesselID = manual.VesselID,
                              Version = manualVersion.Version,
                              ManualCode = manual.ManualCode,
                              ManualName = manual.ManualName,
                              ManualNo = manual.ManualNo,
                              DM_CategoryID = manual.DM_CategoryID,
                              StatusString = status.StatusName,
                              DM_StatusID = manual.DM_StatusID,
                              CanExport = manual.CanExport,
                              TextContents = manualVersion.TextContents,
                              Comments = manualVersion.Comments,
                              VesselIdList = _dbContext.DmManualRolesToAck
                                  .Where(r => r.DM_ManualID == manual.DM_ManualID)
                                  .Select(r => r.VesselID)
                                  .Distinct()
                                  .ToList(),
                              VesselRankList = _dbContext.DmManualRolesToAck
                                  .Where(r => r.DM_ManualID == manual.DM_ManualID)
                                  .Select(r => r.UserRank)
                                  .Distinct()
                                  .ToList(),
                          }).FirstOrDefaultAsync();

        if (data == null)
            throw new ArgumentNullException(nameof(data), "The manual provided does not exist.");

        return data;
    }

    public async Task<long> AddDmManualAsync(CreateManualDto manual)
    {
        if (manual == null) throw new ArgumentNullException(nameof(manual));

        var dm_ManualId = await _mediator.Send(new CreateManualCommand(manual));

        return dm_ManualId;
    }

    public async Task<long> AddDmManualVersionAsync(CreateManualVersionDto manual)
    {
        if (manual == null) throw new ArgumentNullException(nameof(manual));

        var dm_ManualVersionId = await _mediator.Send(new CreateManualVersionCommand(manual));

        return dm_ManualVersionId;
    }

    public async Task<bool> ApproveManualAsync(ApproveManualDto request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        await _mediator.Send(new ApproveManualCommand(request));

        return true;
    }

    public async Task<bool> UpdateDmManualAsync(EditManualDto request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        await _mediator.Send(new EditManualCommand(request));

        return true;
    }

    public async Task UpdateManualTeset(EditManualDto request)
    { 
        // Fetch the manual
        var manual = await _dbContext.DmManuals.FirstOrDefaultAsync(m => m.DM_ManualID == request.DM_ManualID);
        if (manual == null) throw new ArgumentException("Manual not found.", nameof(request.DM_ManualID));

        // Update manual fields
        manual.DM_CategoryID = request.DM_CategoryID;
        manual.ManualCode = request.ManualCode;
        manual.ManualName = request.ManualName;
      
        // Fetch the current version (IsActive == 1 && IsCurrent == 1)
        var manualVersion = await _dbContext.DmManualVersion.FirstOrDefaultAsync(m => m.DM_ManualVersionID == request.DM_ManualVersionID);

        if (manualVersion != null && !string.IsNullOrEmpty(request.TextContents))
        {
            manualVersion.TextContents = request.TextContents;
            _dbContext.Entry(manualVersion).State = EntityState.Modified;
        }

        _dbContext.Entry(manual).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteDmManualAsync(long dm_ManualID)
    {
        var manual = await _dbContext.DmManuals.FindAsync(dm_ManualID);

        if (manual == null)
            throw new ArgumentNullException(nameof(dm_ManualID), "The dm_ManualID provided does not exist.");


        var manualVersionList = await _dbContext.DmManualVersion
                                    .Where(x => x.DM_ManualID == dm_ManualID)
                                    .ToListAsync();
        foreach (var manualVersion in manualVersionList)
        {
            manualVersion.IsActive = 0;
        }

        manual.IsDeleted = 1;
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteDmManualVersionAsync(long dm_ManualID)
    {
        long dm_ManualVersionID = -1; //Get latest version id
        var manualVersion = await _dbContext.DmManualVersion.FindAsync(dm_ManualVersionID);

        if (manualVersion == null)
            throw new ArgumentNullException(nameof(dm_ManualVersionID), "The dm_ManualVersionID provided does not exist.");

        manualVersion.IsActive = 0;
        await _dbContext.SaveChangesAsync();
    }
}