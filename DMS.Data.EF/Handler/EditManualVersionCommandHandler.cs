using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;

namespace DMS.Data.EF.Handler
{
    public class EditManualVersionCommandHandler : IRequestHandler<EditManualVersionCommand, bool>
    {
        private readonly IMapper _mapper;
        private readonly GeneralQueries _queries;
        DmsContext _dmsContext;

        public EditManualVersionCommandHandler(DmsContext dmsContext, IMapper mapper, GeneralQueries queries)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper), "Mapper cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(EditManualVersionCommand request, CancellationToken cancellationToken)
        {
            string remarks = "";

            var dmManualVersion = _dmsContext.DmManualVersion
                            .Where(x => x.DM_ManualVersionID == request.Manual.DM_ManualVersionID)
                            .FirstOrDefault();

            if (dmManualVersion == null)
            {
                throw new ArgumentNullException(nameof(dmManualVersion), "Manual version not exists");
            }

            if (dmManualVersion.TextContents != request.Manual.TextContents)
            {
                dmManualVersion.TextContents = request.Manual.TextContents;

                remarks = remarks +
                        "Manual content changed from " + dmManualVersion.TextContents + " to " + request.Manual.TextContents + Environment.NewLine;
            }

            if (remarks.Trim().Length > 0)
            {
                _dmsContext.DmManualVersion.Update(dmManualVersion);

                remarks = "Manual Name: " + request.Manual.ManualName + Environment.NewLine + "Version: " + dmManualVersion.Version + Environment.NewLine + remarks;
                await _queries.AddToLog(request.Manual.DM_ManualID, request.Manual.DM_ManualVersionID, "Manual Edited", remarks);
            }

            await _dmsContext.SaveChangesAsync(cancellationToken);

            return true;
        } 
    }
}
