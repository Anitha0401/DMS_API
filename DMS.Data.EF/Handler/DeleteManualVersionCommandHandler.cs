using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;

namespace DMS.Data.EF.Handler
{
    public class DeleteManualVersionCommandHandler : IRequestHandler<DeleteManualVersionCommand, bool>
    {
        private readonly GeneralQueries _queries;
        DmsContext _dmsContext;

        public DeleteManualVersionCommandHandler(DmsContext dmsContext, GeneralQueries queries)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(DeleteManualVersionCommand request, CancellationToken cancellationToken)
        {
            var dmManualVersion = _dmsContext.DmManualVersion
                                    .Where(x => x.DM_ManualVersionID == request.Manual.DM_ManualVersionID)
                                    .FirstOrDefault();
            if (dmManualVersion == null)
            {
                throw new ArgumentNullException(nameof(dmManualVersion), "Manual version not exists");
            }

            var previousManualVersion = _dmsContext.DmManualVersion
                                          .Where(d => d.DM_ManualID == request.Manual.DM_ManualID && d.IsCurrent == 0)
                                          .OrderByDescending(d => d.Version)
                                          .FirstOrDefault();
            if (previousManualVersion != null)
            {
                previousManualVersion.IsCurrent = 1;
                _dmsContext.DmManualVersion.Update(previousManualVersion);
            }

            dmManualVersion.IsActive = 0;
            dmManualVersion.IsCurrent = 0;
            _dmsContext.DmManualVersion.Update(dmManualVersion);

            await _queries.AddToLog(request.Manual.DM_ManualID, request.Manual.DM_ManualVersionID, "Manual Version Deleted", "Manual Name: " + request.Manual.ManualName + Environment.NewLine + "Version: " + dmManualVersion.Version);

            await _dmsContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
