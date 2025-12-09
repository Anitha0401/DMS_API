using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.Models;
using DMS.Data.EF.Models.Manuals;
using DMS.Data.EF.Query.Manuals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Handler
{
    public class UpdateManualRightsCommandHandler : IRequestHandler<UpdateManualRightsCommand, bool>
    {
        private readonly ManualGeneralQueries _queries;
        private readonly DmsContext _dmsContext;

        public UpdateManualRightsCommandHandler(DmsContext dmsContext, ManualGeneralQueries queries)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(UpdateManualRightsCommand request, CancellationToken cancellationToken)
        {
            var DM_ManualID = request.Manual.DM_ManualID;

            //Vessel and Roles to Ack
            var existingVslAcks = await _dmsContext.DmManualToAckVsls
                                            .Where(x => x.DM_ManualID == DM_ManualID)
                                            .ToListAsync(cancellationToken);

            var vesselIdList = request.Manual.VesselIdList ?? new List<int>();
            var vesselRankList = request.Manual.VesselRankList ?? new List<string>();

            // Mark IsActive = 0 for items not in the new list
            foreach (var ack in existingVslAcks)
            {
                if (!vesselIdList.Contains(ack.VesselID) || !vesselRankList.Contains(ack.UserRank))
                {
                    ack.IsActive = 0;
                }
            }

            var nextDM_Manual_ToAck_VslID = (_dmsContext.DmManualToAckVsls
                                 .Select(m => (long?)m.DmManualToAckVslId)
                                 .Max() ?? 0);

            foreach (int vesslID in vesselIdList)
            {
                foreach (string vslRank in vesselRankList)
                {
                    var existing = existingVslAcks.FirstOrDefault(x => x.VesselID == vesslID && x.UserRank == vslRank);
                    if (existing != null)
                    {
                        if (existing.IsActive == 0)
                        {
                            existing.IsActive = 1;
                        }
                    }
                    else
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
            var existingHqAcks = await _dmsContext.DmManualToAckHqs
                                           .Where(x => x.DM_ManualID == DM_ManualID)
                                           .ToListAsync(cancellationToken);

            var hqUsersIdList = request.Manual.HQ_UsersIDList ?? new List<long>();

            foreach (var ack in existingHqAcks)
            {
                if (!hqUsersIdList.Contains(ack.HqUsersId))
                {
                    ack.IsActive = 0;
                }
            }

            var nextDmManual_ToAck_HQID = (_dmsContext.DmManualToAckHqs
                                 .Select(m => (long?)m.DmManualToAckHqid)
                                 .Max() ?? 0);
            foreach (int hqUserID in hqUsersIdList)
            {
                var existing = existingHqAcks.FirstOrDefault(x => x.HqUsersId == hqUserID);
                if (existing != null)
                {
                    if (existing.IsActive == 0)
                    {
                        existing.IsActive = 1;
                    }
                }
                else
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
