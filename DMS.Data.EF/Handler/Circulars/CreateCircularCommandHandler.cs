using AutoMapper;
using DMS.Data.EF.Constant;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.Manuals;
using DMS.Data.EF.Query;
using DMS.Data.EF.Query.Manuals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Handler
{
    public class CreateCircularCommandHandler : IRequestHandler<CreateCircularsCommand, long>
    {
        private readonly IMapper _mapper;
        private readonly CIRGeneralQueries _queries;
        private readonly CircularsContext _dbContext;

        public CreateCircularCommandHandler(CircularsContext dbContext, IMapper mapper, CIRGeneralQueries queries)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "DmsContext cannot be null");
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper), "Mapper cannot be null");
            _queries = queries;
        }

        public async Task<long> Handle(CreateCircularsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cir_Master = _mapper.Map<CIR_Master>(request.createCIR);

                //Insert Manual
                // var cir_Number = await _queries.GetNextCIRNumbersByCategoryAsync(request.createCIR.CIR_CategoryID);
                //cir_Master.CIR_MasterID = (_dbContext.CIR_Master
                //                     .Select(m => (long?)m.CIR_MasterID)
                //                     .Max() ?? 0) + 1;
                //cir_Master.Origin = "HQ";
                //cir_Master.VesselID = -1;
                //cir_Master.CIR_Number = cir_Number;

                cir_Master.CreatedDate = DateTime.UtcNow;
                cir_Master.ModifiedBy = null;
                cir_Master.ModifiedDate = null;
                cir_Master.IsDeleted = 0;

                _dbContext.CIR_Master.Update(cir_Master);

                // Insert Blob
                var cir_Blob = _mapper.Map<CIR_Blob>(request.createCIR);

                cir_Blob.CIR_BlobID = (_dbContext.CIR_Blob
                                     .Select(m => (long?)m.CIR_BlobID)
                                     .Max() ?? 0) + 1;
                cir_Blob.CIR_MasterID = cir_Master.CIR_MasterID;
                cir_Blob.Origin = "HQ";
                cir_Blob.VesselID = -1;
                cir_Blob.HQ_Storage_Option = "Database";
                cir_Blob.HQ_Local_Folder = "";
                cir_Blob.HQ_DatabaseName = "";
                cir_Blob.AddedOn = DateTime.UtcNow;
                cir_Blob.Cloud_ContainerName = null;
                cir_Blob.Cloud_Directory = null;

                _dbContext.CIR_Blob.Add(cir_Blob);

                var allowedUsersToAck = await _dbContext.CIR_CategoryExt
                                           .Where(c => c.CIR_CategoryID == request.createCIR.CIR_CategoryID)
                                           .Select(x => x.AllowedUsersToAck)
                                           .FirstOrDefaultAsync(cancellationToken);

                var userRanksList = allowedUsersToAck?
                                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(u => u.Trim())
                                        .Where(u => !string.IsNullOrWhiteSpace(u))
                                        .ToList();

                if (userRanksList != null && userRanksList.Count > 0)
                {
                    var nextCIR_ToAck_VslID = (_dbContext.CIR_ToAck_Vsl
                                                 .Select(m => (long?)m.CIR_ToAck_VslID)
                                                 .Max() ?? 0) + 1;

                    var toAckEntities = userRanksList
                                        .Select((rank, index) => new CIR_ToAck_Vsl
                                        {
                                            CIR_ToAck_VslID = nextCIR_ToAck_VslID + index,
                                            CIR_MasterID = cir_Master.CIR_MasterID,
                                            Origin = "HQ",
                                            VesselID = -1,
                                            UserRank = rank,
                                            IsActive = 1
                                        })
                                        .ToList();

                    _dbContext.CIR_ToAck_Vsl.AddRange(toAckEntities);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                string notificationType = CIRNotificationType.ADD, notificationTitle = "New " + request.createCIR.CircularType + " Added";
                string remarks = request.createCIR.CircularType + ": " + cir_Master.CIR_Number;

                await _queries.AddToLog(cir_Master.CIR_MasterID, "New " + request.createCIR.CircularType + " Added", remarks);

                await _queries.AddNotification(cir_Master.CIR_MasterID, "", notificationType, notificationTitle, request.createCIR.CircularType + ": " + cir_Master.CIR_Number);

                return cir_Master.CIR_MasterID;
            }
            catch (Exception err)
            {
                throw err;
            }
        }
    }
}
