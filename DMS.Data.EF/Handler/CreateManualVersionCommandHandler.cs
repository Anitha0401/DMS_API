using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;

namespace DMS.Data.EF.Handler
{
    public class CreateManualVersionCommandHandler : IRequestHandler<CreateManualVersionCommand, long>
    {
        private readonly IMapper _mapper;
        private readonly GeneralQueries _queries;
        private readonly DmsContext _dmsContext;

        public CreateManualVersionCommandHandler(DmsContext dmsContext, IMapper mapper, GeneralQueries queries)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper), "Mapper cannot be null");
            _queries = queries;
        }

        public async Task<long> Handle(CreateManualVersionCommand request, CancellationToken cancellationToken)
        {
            var dmManualVersion = _mapper.Map<DM_ManualVersion>(request.manualVersion);

            var nextVersion = (_dmsContext.DmManualVersion
                                .Where(x => x.DM_ManualID == request.manualVersion.DM_ManualID)
                                .Select(m => (int?)m.Version)
                                .Max() ?? 0) + 1;
            dmManualVersion.Version = nextVersion;
            dmManualVersion.Origin = "HQ";
            dmManualVersion.VesselID = -1;
            dmManualVersion.IsActive = 1;
            dmManualVersion.IsCurrent = 0;

            _dmsContext.DmManualVersion.Add(dmManualVersion);

            await _dmsContext.SaveChangesAsync(cancellationToken);

            await _queries.AddToLog(dmManualVersion.DM_ManualID, dmManualVersion.DM_ManualVersionID, "New Version Added", "Version: " + nextVersion);

            return dmManualVersion.DM_ManualVersionID;
        }
    }
}
