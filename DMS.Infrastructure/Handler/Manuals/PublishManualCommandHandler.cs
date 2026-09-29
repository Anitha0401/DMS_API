using DMS.Data.EF.Constant;
using DMS.Data.EF.Context;
using DMS.Data.EF.Query.Manuals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Handler.Manuals
{
    public class PublishManualCommandHandler : IRequestHandler<PublishManualCommand, bool>
    {
        private readonly ManualGeneralQueries _queries;
        DmsContext _dbContext;

        public PublishManualCommandHandler(DmsContext dbContext, ManualGeneralQueries queries)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(PublishManualCommand request, CancellationToken cancellationToken)
        {
            var dmManual = await _dbContext.DmManuals
                                   .Where(x => x.DM_ManualID == request.Manual.DM_ManualID)
                                   .FirstOrDefaultAsync();

            if (dmManual == null)
            {
                throw new ArgumentNullException(nameof(dmManual), "Manual not exists");
            }

            var dmManualVersion = _dbContext.DmManualVersions
                                    .Where(x => x.DM_ManualVersionID == request.Manual.DM_ManualVersionID)
                                    .FirstOrDefault();
            if (dmManualVersion == null)
            {
                throw new ArgumentNullException(nameof(dmManualVersion), "Manual version not exists");
            }

            var previousManualVersion = _dbContext.DmManualVersions
                                          .Where(d => d.DM_ManualID == request.Manual.DM_ManualID && 
                                                      d.DM_ManualVersionID != request.Manual.DM_ManualVersionID && 
                                                      d.IsDisabled == 0)
                                          .FirstOrDefault();
            if (previousManualVersion != null)
            {
                previousManualVersion.IsActive = 0;
                previousManualVersion.IsDisabled = 1;
                _dbContext.DmManualVersions.Update(previousManualVersion);
            }

            string nextApproval_HQRole = "", nextApproval_HQDept = "";
            long nextDM_StatusID = dmManual.DmStatusId;
            if (dmManual.IsApprovalRequired == 1)
            {
                // Get the next approval step for the current approval flow and status
                var nextApproval = await (
                                        from setup in _dbContext.DmApprovalAndMailSetups
                                        where setup.FlowId == dmManual.Approval_FlowID
                                            && setup.DmStatusId == dmManual.DmStatusId
                                            && setup.NextDmStatusId != null && setup.NextDmStatusId > 0
                                            // Exclude if UserRole already exists in DM_Manual_ApprovedList for this manual
                                            && !_dbContext.DmManualApprovedLists.Any(a => a.DM_ManualID == dmManual.DM_ManualID
                                                                                        && a.UserRole == setup.UserRole && a.HQDept == setup.HQDept)
                                        orderby setup.ApprovalOrder
                                        select setup
                                     ).FirstOrDefaultAsync();

                if (nextApproval != null)
                {
                    nextDM_StatusID = nextApproval.DmStatusId ?? nextDM_StatusID;
                    if (nextDM_StatusID != DMStatusConstant.STATUS_RELEASE)
                    {
                        nextApproval_HQRole = nextApproval.UserRole ?? "";
                        nextApproval_HQDept = nextApproval.HQDept ?? "";
                    }
                }
            }

            dmManual.DmStatusId = nextDM_StatusID;
            dmManual.ModifiedBy = request.Manual.UserID;
            dmManual.ModifiedDate = DateTime.UtcNow;
            dmManual.NextApproval_HQRole = nextApproval_HQRole;
            dmManual.NextApproval_HQDept = nextApproval_HQDept;

            dmManualVersion.IsActive = 1;
            dmManualVersion.IsDisabled = 0;
            dmManualVersion.LastModifiedBy = request.Manual.UserID;
            dmManualVersion.LastModifiedDate = DateTime.UtcNow;
            if (nextDM_StatusID == DMStatusConstant.STATUS_RELEASE)
            {
                dmManualVersion.ReleasedBy = request.Manual.UserID;
                dmManualVersion.ReleasedDate = DateTime.UtcNow;
            }

            _dbContext.DmManuals.Update(dmManual);
            _dbContext.DmManualVersions.Update(dmManualVersion);

            await _queries.AddToLog(request.Manual.DM_ManualID, request.Manual.DM_ManualVersionID, "Manual Published", "Manual Name: " + dmManual.ManualName + Environment.NewLine + "Version: " + dmManualVersion.Version);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
