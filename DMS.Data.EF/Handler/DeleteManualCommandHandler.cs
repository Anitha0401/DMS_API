using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query.Manuals;
using MediatR;

namespace DMS.Data.EF.Handler
{
    public class DeleteManualCommandHandler : IRequestHandler<DeleteManualCommand, bool>
    {
        private readonly ManualGeneralQueries _queries;
        DmsContext _dmsContext;

        public DeleteManualCommandHandler(DmsContext dmsContext, ManualGeneralQueries queries)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(DeleteManualCommand request, CancellationToken cancellationToken)
        {
            var dmManual = _dmsContext.DmManuals
                            .Where(x => x.DM_ManualID == request.Manual.DM_ManualID)
                            .FirstOrDefault();

            if (dmManual == null)
            {
                throw new ArgumentNullException(nameof(dmManual), "Manual not exists");
            }
       
            dmManual.IsDeleted = 1;
            dmManual.ModifiedBy = request.Manual.UserID;
            dmManual.ModifiedDate = DateTime.UtcNow;
            _dmsContext.DmManuals.Update(dmManual);

            // Optionally, can also delete related versions & attachments
            var manualVersions = _dmsContext.DmManualVersions
                                    .Where(v => v.DM_ManualID == request.Manual.DM_ManualID)
                                    .ToList();
            if (manualVersions.Any())
            {
                foreach (var version in manualVersions)
                {
                    version.IsActive = 0;
                }
                _dmsContext.DmManualVersions.UpdateRange(manualVersions);
            }

            var attachments = _dmsContext.DmManualAttachments
                                .Where(a => a.DM_ManualID == request.Manual.DM_ManualID)
                                .ToList();
            if (attachments.Any())
            {
                for (int i = 0; i < attachments.Count; i++)
                {
                    attachments[i].IsDeleted = 1;
                }
                _dmsContext.DmManualAttachments.UpdateRange(attachments);
            }

            await _queries.AddToLog(request.Manual.DM_ManualID, -1, "Manual Deleted", "Manual Name: " + dmManual.ManualName);

            await _dmsContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
