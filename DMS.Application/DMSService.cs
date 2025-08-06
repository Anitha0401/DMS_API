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
            Label = m.ManualNo + " - " + m.ManualName
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

    public async Task<string?> GetManualContent(long dm_ManualID)
    {
        return await _queries.GetDmManualContentAsync(dm_ManualID);
    }

    public async Task<List<DM_Manual_Attachments>> GetManualAttachmentListAsync(long dm_ManualID)
    {
        var attachments = await _dbContext.DmManualAttachments
                             .Where(a => a.DM_ManualID == dm_ManualID)
                             .ToListAsync();

        return attachments;
    }

    public async Task<List<Dm_ManualVersion_View>> GetDmManualsAsync()
    {
        var data = await (from manual in _dbContext.DmManuals
                    join manualVersion in _dbContext.DmManualVersion
                         on manual.DM_ManualID equals manualVersion.DM_ManualID
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

    public async Task<DM_Manual> GetDmManualDetailsAsync(long dm_ManualID)
    {
        var manual = await _dbContext.DmManuals.FirstOrDefaultAsync(m => m.DM_ManualID == dm_ManualID);
        if (manual == null)
            throw new ArgumentNullException(nameof(dm_ManualID), "The dm_ManualID provided does not exist.");
        return manual;
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

    public async Task UpdateDmManualAsync(DM_Manual manual)
    {
        if (manual == null) throw new ArgumentNullException(nameof(manual));

        _dbContext.Update(manual);
        _dbContext.Entry(manual).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteDmManualAsync(long dm_ManualID)
    {
        var manual = await _dbContext.DmManuals.FindAsync(dm_ManualID);

        if (manual == null)
            throw new ArgumentNullException(nameof(dm_ManualID), "The dm_ManualID provided does not exist.");

        _dbContext.DmManuals.Remove(manual);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteDmManualVersionAsync(long dm_ManualID)
    {
        long dm_ManualVersionID = -1; //Get latest version id
        var manualVersion = await _dbContext.DmManualVersion.FindAsync(dm_ManualVersionID);

        if (manualVersion == null)
            throw new ArgumentNullException(nameof(dm_ManualVersionID), "The dm_ManualVersionID provided does not exist.");

        _dbContext.DmManualVersion.Remove(manualVersion);
        await _dbContext.SaveChangesAsync();
    }
}