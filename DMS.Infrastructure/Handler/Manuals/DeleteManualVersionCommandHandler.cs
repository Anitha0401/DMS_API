using DMS.Data.EF.Context;
using DMS.Data.EF.Query.Manuals;
using MediatR;

namespace DMS.Data.EF.Handler.Manuals
{
    public class DeleteManualVersionCommandHandler : IRequestHandler<DeleteManualVersionCommand, bool>
    {
        private readonly ManualGeneralQueries _queries;
        DmsContext _dbContext;

        public DeleteManualVersionCommandHandler(DmsContext dbContext, ManualGeneralQueries queries)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(DeleteManualVersionCommand request, CancellationToken cancellationToken)
        {
            var dmManualVersion = _dbContext.DmManualVersions
                                    .Where(x => x.DM_ManualVersionID == request.Manual.DM_ManualVersionID)
                                    .FirstOrDefault();
            if (dmManualVersion == null)
            {
                throw new ArgumentNullException(nameof(dmManualVersion), "Manual version not exists");
            }

            var previousManualVersion = _dbContext.DmManualVersions
                                          .Where(d => d.DM_ManualID == request.Manual.DM_ManualID && d.IsDisabled == 0)
                                          .OrderByDescending(d => d.Version)
                                          .FirstOrDefault();
            if (previousManualVersion != null)
            {
                previousManualVersion.IsActive = 0;
                previousManualVersion.IsDisabled = 1;
                _dbContext.DmManualVersions.Update(previousManualVersion);
            }

            dmManualVersion.IsActive = 1;
            dmManualVersion.IsDisabled = 0;
            _dbContext.DmManualVersions.Update(dmManualVersion);

            await _queries.AddToLog(request.Manual.DM_ManualID, request.Manual.DM_ManualVersionID, "Manual Version Deleted", "Manual Name: " + request.Manual.ManualName + Environment.NewLine + "Version: " + dmManualVersion.Version);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
