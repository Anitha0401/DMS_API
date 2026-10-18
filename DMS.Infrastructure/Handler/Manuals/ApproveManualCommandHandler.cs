using DMS.Data.EF.Constant;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models.Manuals;
using DMS.Data.EF.Query.Manuals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Handler.Manuals
{
    public class ApproveManualCommandHandler : IRequestHandler<ApproveManualCommand, bool>
    {
        private readonly ManualGeneralQueries _queries;
        DmsContext _dbContext;

        public ApproveManualCommandHandler(DmsContext dbContext, ManualGeneralQueries queries)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(ApproveManualCommand request, CancellationToken cancellationToken)
        {
            var dmManual = _dbContext.DmManuals
                                .Where(x => x.DM_ManualID == request.Manual.DM_ManualID)
                                .FirstOrDefault();
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

            if (!request.isToSendBack)
            {
                var user = await _queries.GetHQUser(request.Manual.UserID);

                // Insert a new record into DmManualApprovedLists
                var approvedListEntry = new DmManualApprovedList
                {
                    DmManualApprovedListId = await _dbContext.NextIdAsync<DmManualApprovedList>(ct: cancellationToken),
                    Origin = "HQ",
                    VesselID = -1,
                    DM_ManualID = request.Manual.DM_ManualID,
                    UserId = request.Manual.UserID,
                    UserRole = user?.Role ?? "",
                    UserName = user?.UserName ?? "",
                    HQDept = user?.HQDept ?? "",
                    ApprovedDate = DateTime.UtcNow,
                    Comments = request.Manual.Comments
                };
                _dbContext.DmManualApprovedLists.Add(approvedListEntry);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // Get the next approval step for the current approval flow and status
            string nextApproval_HQRole = "", nextApproval_HQDept = "";
            long nextDM_StatusID = -1;
            string notificationType = ManualNotificationType.APPROVE, notificationTitle = "Manual approved";

            if (request.isToSendBack)
            {
                nextDM_StatusID = await _queries.GetSendBackApprovalStatusID(dmManual.Approval_FlowID ?? -1, dmManual.DmStatusId);
                notificationTitle = "Manual sendback for edit";
            }
            else
            { 
                nextDM_StatusID = await _queries.GetNextApprovalStatusID(dmManual.Approval_FlowID ?? -1, dmManual.DmStatusId);
            }

            if (nextDM_StatusID == DMStatusConstant.STATUS_RELEASE)
            {
                notificationType = ManualNotificationType.PUBLISH;
                notificationTitle = "Manual approved and Released";
            }

            if (nextDM_StatusID != DMStatusConstant.STATUS_RELEASE)
            {
                var nextApproval = await (
                                from setup in _dbContext.DmApprovalAndMailSetups
                                where setup.FlowId == dmManual.Approval_FlowID
                                    && setup.DmStatusId == nextDM_StatusID
                                    && !_dbContext.DmManualApprovedLists.Any(a => a.DM_ManualID == dmManual.DM_ManualID
                                                                                && a.UserRole == setup.UserRole && a.HQDept == setup.HQDept)
                                orderby setup.ApprovalOrder
                                select setup
                             ).FirstOrDefaultAsync();
                if (nextApproval != null)
                {
                    nextApproval_HQRole = nextApproval.UserRole ?? "";
                    nextApproval_HQDept = nextApproval.HQDept ?? "";
                    notificationTitle = "Manual approved and moved to next approval (" + nextApproval_HQRole + " - " + nextApproval_HQDept + ")";
                }
            }

            dmManual.DmStatusId = nextDM_StatusID;
            dmManual.NextApproval_HQRole = nextApproval_HQRole;
            dmManual.NextApproval_HQDept = nextApproval_HQDept;
            dmManual.ModifiedBy = request.Manual.UserID;
            dmManual.ModifiedDate = DateTime.UtcNow;

            dmManualVersion.LastModifiedBy = request.Manual.UserID;
            dmManualVersion.LastModifiedDate = DateTime.UtcNow;
            if (nextDM_StatusID == DMStatusConstant.STATUS_RELEASE)
            {
                dmManualVersion.ReleasedBy = request.Manual.UserID;
                dmManualVersion.ReleasedDate = DateTime.UtcNow;
            }

            _dbContext.DmManuals.Update(dmManual);
            _dbContext.DmManualVersions.Update(dmManualVersion);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _queries.AddNotification(dmManual.DM_ManualID, dmManualVersion.DM_ManualVersionID, "", notificationType, notificationTitle, "Manual: " + request.Manual.ManualName);

            return true;
        }
    }
}
