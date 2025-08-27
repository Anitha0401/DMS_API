using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Handler
{
    public class UpdateManualRightsCommandHandler : IRequestHandler<UpdateManualRightsCommand, bool>
    {
        private readonly GeneralQueries _queries;
        private readonly DmsContext _dmsContext;

        public UpdateManualRightsCommandHandler(DmsContext dmsContext, GeneralQueries queries)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(UpdateManualRightsCommand request, CancellationToken cancellationToken)
        {
            var dmManualID = request.Manual.DM_ManualID;

            //Vessel and Roles to Ack
            var nextDM_Manual_ToAck_VslID = (_dmsContext.DmManual_ToAck_Vsl
                                 .Select(m => (long?)m.DM_Manual_ToAck_VslID)
                                 .Max() ?? 0);

            if (request.Manual.VesselIDList != null && request.Manual.VslRankList != null)
            {
                foreach (int vesslID in request.Manual.VesselIDList)
                {
                    foreach (string vslRank in request.Manual.VslRankList)
                    {
                        nextDM_Manual_ToAck_VslID = nextDM_Manual_ToAck_VslID + 1;

                        var roleToAck = new DM_Manual_ToAck_Vsl();
                        roleToAck.DM_Manual_ToAck_VslID = nextDM_Manual_ToAck_VslID;
                        roleToAck.Origin = "HQ";
                        roleToAck.DM_ManualID = dmManualID;
                        roleToAck.VesselID = vesslID;
                        roleToAck.UserRank = vslRank;
                        roleToAck.IsActive = 1;
                        _dmsContext.DmManual_ToAck_Vsl.Add(roleToAck);
                    }
                }
            }

            //Vessel and Roles to Ack
            var nextDmManual_ToAck_HQID = (_dmsContext.DmManual_ToAck_HQ
                                 .Select(m => (long?)m.DM_Manual_ToAck_HQID)
                                 .Max() ?? 0);

            if (request.Manual.HQUserIDList != null)
            {
                foreach (int vesslID in request.Manual.HQUserIDList)
                {
                    nextDmManual_ToAck_HQID = nextDmManual_ToAck_HQID + 1;

                    var hqToAck = new DM_Manual_ToAck_HQ();
                    hqToAck.DM_Manual_ToAck_HQID = nextDmManual_ToAck_HQID;
                    hqToAck.Origin = "HQ";
                    hqToAck.VesselID = vesslID;
                    hqToAck.UserRole = "";
                    hqToAck.IsActive = 1;
                    _dmsContext.DmManual_ToAck_HQ.Add(hqToAck);
                }
            }

            await _dmsContext.SaveChangesAsync(cancellationToken);

            await _queries.AddToLog(dmManualID, -1, "Manual Rights Assigned", "");

            return true;
        }
    }
}
