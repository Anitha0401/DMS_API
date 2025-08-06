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

            var dmManualVersion = _dmsContext.DmManualVersion
                                    .Where(x => x.DM_ManualVersionID == request.Manual.DM_ManualVersionID)
                                    .FirstOrDefault();
            if (dmManualVersion == null)
            {
                throw new ArgumentNullException(nameof(dmManualVersion), "Manual version not exists");
            }

            var dm_StatusID = _dmsContext.Dm_Status.FirstOrDefault(x => x.StatusName == "PUBLISH")?.DM_StatusID;
            if (dm_StatusID == null)
            {
                throw new ArgumentNullException(nameof(dm_StatusID), "Status ID for 'PUBLISH' not found");
            }

            dmManual.DM_StatusID = (long)dm_StatusID;
            dmManual.ModifiedBy = request.Manual.UserID;
            dmManual.ModifiedDate = DateTime.UtcNow;
            _dmsContext.DmManuals.Update(dmManual);

            var previousManualVersion = _dmsContext.DmManualVersion
                                          .Where(d => d.DM_ManualID == request.Manual.DM_ManualID && d.IsCurrent == 1)
                                          .FirstOrDefault();
            if (previousManualVersion != null)
            {
                previousManualVersion.IsCurrent = 0;
                _dmsContext.DmManualVersion.Update(previousManualVersion);
            }

            dmManualVersion.IsCurrent = 1;
            dmManualVersion.ReleasedBy = request.Manual.UserID;
            dmManualVersion.ReleasedDate = DateTime.UtcNow;

            _dmsContext.DmManualVersion.Update(dmManualVersion);

            await _queries.AddToLog(request.Manual.DM_ManualID, request.Manual.DM_ManualVersionID, "Manual Published", "Manual Name: " + dmManual.ManualName + Environment.NewLine + "Version: " + dmManualVersion.Version);

            await _dmsContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
