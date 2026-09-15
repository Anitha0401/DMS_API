using DMS.Data.EF.Constant;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.Models.Manuals;
using DMS.Data.EF.Query.Manuals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Handler.Manuals
{
    public class EditManualCommandHandler : IRequestHandler<EditManualCommand, CreateManualResultDto>
    {
        private readonly ManualGeneralQueries _queries;
        DmsContext _dbContext;

        public EditManualCommandHandler(DmsContext dbContext, ManualGeneralQueries queries)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<CreateManualResultDto> Handle(EditManualCommand request, CancellationToken cancellationToken)
        {
            string remarks = "";
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

            if (dmManual.DM_CategoryID != request.Manual.DM_CategoryID)
            {
                var fromCategory = _dbContext.DmCategories
                                    .Where(c => c.DM_CategoryID == dmManual.DM_CategoryID)
                                    .Select(x => x.CategoryName)
                                    .FirstOrDefault();

                var toCategory = _dbContext.DmCategories
                                  .Where(c => c.DM_CategoryID == request.Manual.DM_CategoryID)
                                  .Select(x => x.CategoryName)
                                  .FirstOrDefault();

                dmManual.DM_CategoryID = request.Manual.DM_CategoryID;

                remarks = remarks +
                    "Category changed from " + fromCategory + " to " + toCategory + Environment.NewLine;
            }

            if (dmManual.ManualCode != request.Manual.ManualCode)
            {
                dmManual.ManualCode = request.Manual.ManualCode;

                remarks = remarks +
                        "Manual code changed from " + dmManual.ManualCode + " to " + request.Manual.ManualCode + Environment.NewLine;
            }

            if (dmManual.ManualName != request.Manual.ManualName)
            {   
                dmManual.ManualName = request.Manual.ManualName;

                remarks = remarks +
                        "Manual name changed from " + dmManual.ManualName + " to " + request.Manual.ManualName + Environment.NewLine;
            }

            if (dmManual.IsMainTitle != request.Manual.IsMainTitle)
            {
                dmManual.IsMainTitle = request.Manual.IsMainTitle;

                string isMainTitle = "YES"; if (dmManual.IsMainTitle == 0) { isMainTitle = "NO"; }
                string isMainTitle_to = "YES"; if (request.Manual.IsMainTitle == 0) { isMainTitle_to = "NO"; }
                remarks = remarks +
                    "Is Main Title changed from " + isMainTitle + " to " + isMainTitle_to + Environment.NewLine;
            }

            if (dmManual.IsAcknowledgementRequired != request.Manual.IsAcknowledgementRequired)
            {
                dmManual.IsAcknowledgementRequired = request.Manual.IsAcknowledgementRequired;

                string isAcknowledgementRequired = "YES"; if (dmManual.IsAcknowledgementRequired == 0) { isAcknowledgementRequired = "NO"; }
                string isIsAcknowledgementRequired_to = "YES"; if (request.Manual.IsAcknowledgementRequired == 0) { isIsAcknowledgementRequired_to = "NO"; }
                remarks = remarks +
                    "Acknowledgement Required changed from " + isAcknowledgementRequired + " to " + isIsAcknowledgementRequired_to + Environment.NewLine;
            }

            if (dmManual.CanExport != request.Manual.CanExport)
            {
                dmManual.CanExport = request.Manual.CanExport;

                string canExport = "YES"; if (dmManual.CanExport == 0) { canExport = "NO"; }
                string canExport_to = "YES"; if (request.Manual.CanExport == 0) { canExport_to = "NO"; }
                remarks = remarks +
                    "Can Export changed from " + canExport + " to " + canExport_to + Environment.NewLine;
            }

            if (dmManual.IsApprovalRequired != request.Manual.IsApprovalRequired)
            {
                dmManual.IsApprovalRequired = request.Manual.IsApprovalRequired;

                string isApprReqn = "YES"; if (dmManual.IsApprovalRequired == 0) { isApprReqn = "NO"; }
                string isApprReqn_to = "YES"; if (request.Manual.IsApprovalRequired == 0) { isApprReqn_to = "NO"; }
                remarks = remarks +
                    "Approval Required changed from " + isApprReqn + " to " + isApprReqn_to + Environment.NewLine;
            }

            if (dmManual.Approval_FlowID != request.Manual.Approval_FlowID)
            {
                dmManual.Approval_FlowID = request.Manual.Approval_FlowID;
              
                remarks = remarks +
                    "Approval Flow changed from " + dmManual.Approval_FlowID + " to " + request.Manual.Approval_FlowID + Environment.NewLine;
            }

            if (dmManualVersion.TextContents != request.Manual.TextContents)
            {
                if (dmManual.DmStatusId == DMStatusConstant.STATUS_RELEASE)
                {
                    dmManual.DmStatusId = _dbContext.DmStatuses.FirstOrDefault(x => x.StatusName == "Draft")?.DmStatusId ?? -1;

                    var currentVersion = string.IsNullOrEmpty(dmManualVersion.Version) ? "" : dmManualVersion.Version;
                    string[] parts = currentVersion.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    int major = int.Parse(parts[0]);
                    int minor = int.Parse(parts[1]);

                    // Next minor version
                    // string nextMinor = $"{major}.{minor + 1}";

                    // Next major version
                    string nextVersion = $"{major + 1}.0";

                    var newDMManualVersion = new DmManualVersion();
                    newDMManualVersion.DM_ManualVersionID = (_dbContext.DmManualVersions
                                                          .Select(m => (long?)m.DM_ManualVersionID)
                                                          .Max() ?? 0) + 1;
                    newDMManualVersion.DM_ManualID = dmManual.DM_ManualID;
                    newDMManualVersion.Origin = "HQ";
                    newDMManualVersion.VesselID = -1;
                    newDMManualVersion.Version = nextVersion;
                    newDMManualVersion.IsActive = 1;
                    newDMManualVersion.TextContents = request.Manual.TextContents;
                    _dbContext.DmManualVersions.Add(newDMManualVersion);

                    dmManualVersion.IsActive = 0;

                     remarks = remarks +
                        "New Manual version " + nextVersion + " added " + Environment.NewLine;
                }
                else
                {
                    dmManualVersion.TextContents = request.Manual.TextContents;

                    remarks = remarks +
                        "Manual content changed from " + dmManualVersion.TextContents + " to " + request.Manual.TextContents + Environment.NewLine;
                }
            }

            string notificationType = ManualNotificationType.EDIT, notificationTitle = "Manual details editted";
            if (request.Manual.IsToPublish)
            {
                long nextDM_StatusID = DMStatusConstant.STATUS_RELEASE;
                notificationType = ManualNotificationType.PUBLISH; notificationTitle = "Manual details editted and Released";

                string nextApproval_HQRole = "", nextApproval_HQDept = "";
                if (request.Manual.IsApprovalRequired == 1)
                {
                    nextDM_StatusID = await _queries.GetNextApprovalStatusID(request.Manual.Approval_FlowID ?? -1, DMStatusConstant.STATUS_DRAFT);

                    if (nextDM_StatusID != DMStatusConstant.STATUS_RELEASE)
                    {
                        var nextApproval = await (
                                        from setup in _dbContext.DmApprovalAndMailSetups
                                        where setup.FlowId == request.Manual.Approval_FlowID
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
                        }
                    }
                    notificationType = ManualNotificationType.APPROVE; notificationTitle = "Manual details editted and send to next approval";
                }

                dmManual.DmStatusId = nextDM_StatusID;
                dmManual.NextApproval_HQRole = nextApproval_HQRole;
                dmManual.NextApproval_HQDept = nextApproval_HQDept;

                dmManualVersion.PublishedBy = request.Manual.UserID;
                dmManualVersion.PublishedDate = DateTime.UtcNow;
                if (nextDM_StatusID == DMStatusConstant.STATUS_RELEASE)
                {
                    dmManualVersion.ReleasedBy = request.Manual.UserID;
                    dmManualVersion.ReleasedDate = DateTime.UtcNow;
                }

                remarks = remarks + "Manual Published " + Environment.NewLine;
            }

            if (remarks.Trim().Length > 0)
            {
                dmManual.ModifiedBy = request.Manual.UserID;
                dmManual.ModifiedDate = DateTime.UtcNow;

                dmManualVersion.LastModifiedBy = request.Manual.UserID;
                dmManualVersion.LastModifiedDate = DateTime.UtcNow;

                _dbContext.DmManuals.Update(dmManual);
                _dbContext.DmManualVersions.Update(dmManualVersion);

                // Add Log
                await _queries.AddToLog(request.Manual.DM_ManualID, request.Manual.DM_ManualVersionID, "Manual Edited", "Manual Name: " + request.Manual.ManualName + Environment.NewLine + remarks);

                await _queries.AddNotification(dmManual.DM_ManualID, dmManualVersion.DM_ManualVersionID, "", notificationType, notificationTitle, "Manual: " + request.Manual.ManualName);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateManualResultDto
            {
                ManualId = dmManual.DM_ManualID,
                ManualVersionId = dmManualVersion.DM_ManualVersionID
            };
        }
    }
}
