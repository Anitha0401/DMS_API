using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Handler
{
    public class PublishManualCommandHandler : IRequestHandler<PublishManualCommand, bool>
    {
        private readonly GeneralQueries _queries;
        DmsContext _dmsContext;

        public PublishManualCommandHandler(DmsContext dmsContext, GeneralQueries queries)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(PublishManualCommand request, CancellationToken cancellationToken)
        {
            var dmManual = await _dmsContext.DmManuals
                                   .Where(x => x.DM_ManualID == request.Manual.DM_ManualID)
                                   .FirstOrDefaultAsync();

            if (dmManual == null)
            {
                throw new ArgumentNullException(nameof(dmManual), "Manual not exists");
            }

            var dmManualVersion = _dmsContext.DmManualVersions
                                    .Where(x => x.DM_ManualVersionID == request.Manual.DM_ManualVersionID)
                                    .FirstOrDefault();
            if (dmManualVersion == null)
            {
                throw new ArgumentNullException(nameof(dmManualVersion), "Manual version not exists");
            }

            var previousManualVersion = _dmsContext.DmManualVersions
                                          .Where(d => d.DM_ManualID == request.Manual.DM_ManualID && 
                                                      d.DM_ManualVersionID != request.Manual.DM_ManualVersionID && 
                                                      d.IsDisabled == 0)
                                          .FirstOrDefault();
            if (previousManualVersion != null)
            {
                previousManualVersion.IsActive = 0;
                previousManualVersion.IsDisabled = 1;
                _dmsContext.DmManualVersions.Update(previousManualVersion);
            }

            string nextApproval_HQRole = "", nextApproval_HQDept = "";
            long nextDM_StatusID = dmManual.DmStatusId;
            if (dmManual.IsApprovalRequired == 1)
            {
                // Get the next approval step for the current approval flow and status
                var nextApproval = await (
                                        from setup in _dmsContext.DmApprovalAndMailSetups
                                        where setup.FlowId == dmManual.Approval_FlowID
                                            && setup.DmStatusId == dmManual.DmStatusId
                                            && setup.NextDmStatusId != null && setup.NextDmStatusId > 0
                                            // Exclude if UserRole already exists in DM_Manual_ApprovedList for this manual
                                            && !_dmsContext.DmManualApprovedLists.Any(a => a.DM_ManualID == dmManual.DM_ManualID
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

            _dmsContext.DmManuals.Update(dmManual);
            _dmsContext.DmManualVersions.Update(dmManualVersion);

            await _queries.AddToLog(request.Manual.DM_ManualID, request.Manual.DM_ManualVersionID, "Manual Published", "Manual Name: " + dmManual.ManualName + Environment.NewLine + "Version: " + dmManualVersion.Version);

            await _dmsContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
