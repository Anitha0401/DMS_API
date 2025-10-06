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
            var DM_ManualID = request.Manual.DM_ManualID;

            //Vessel and Roles to Ack
            var nextDM_Manual_ToAck_VslID = (_dmsContext.DmManualToAckVsls
                                 .Select(m => (long?)m.DmManualToAckVslId)
                                 .Max() ?? 0);

            if (request.Manual.VesselIdList != null && request.Manual.VesselRankList != null)
            {
                foreach (int vesslID in request.Manual.VesselIdList)
                {
                    foreach (string vslRank in request.Manual.VesselRankList)
                    {
                        nextDM_Manual_ToAck_VslID = nextDM_Manual_ToAck_VslID + 1;

                        var roleToAck = new DmManualToAckVsl();
                        roleToAck.DmManualToAckVslId = nextDM_Manual_ToAck_VslID;
                        roleToAck.Origin = "HQ";
                        roleToAck.DM_ManualID = DM_ManualID;
                        roleToAck.VesselID = vesslID;
                        roleToAck.UserRank = vslRank;
                        roleToAck.IsActive = 1;
                        _dmsContext.DmManualToAckVsls.Add(roleToAck);
                    }
                }
            }

            //HQ user to Ack
            var nextDmManual_ToAck_HQID = (_dmsContext.DmManualToAckHqs
                                 .Select(m => (long?)m.DmManualToAckHqid)
                                 .Max() ?? 0);

            if (request.Manual.HQ_UsersIDList != null)
            {
                foreach (int hqUserID in request.Manual.HQ_UsersIDList)
                {
                    nextDmManual_ToAck_HQID = nextDmManual_ToAck_HQID + 1;

                    var hqToAck = new DmManualToAckHq();
                    hqToAck.DmManualToAckHqid = nextDmManual_ToAck_HQID;
                    hqToAck.Origin = "HQ";
                    hqToAck.VesselID = -1;
                    hqToAck.HqUsersId = hqUserID;
                    hqToAck.DM_ManualID = DM_ManualID;
                    hqToAck.Role = "";
                    hqToAck.IsActive = 1;
                    _dmsContext.DmManualToAckHqs.Add(hqToAck);
                }
            }

            await _dmsContext.SaveChangesAsync(cancellationToken);

            await _queries.AddToLog(DM_ManualID, -1, "Manual Rights Assigned", "");

            return true;
        }
    }
}
