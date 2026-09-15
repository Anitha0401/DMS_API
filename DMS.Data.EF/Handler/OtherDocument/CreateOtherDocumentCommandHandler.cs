using AutoMapper;
using DMS.Data.EF.Constant;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models.OtherDocument;
using DMS.Data.EF.Query.Circulars;
using MediatR;

namespace DMS.Data.EF.Handler
{
    public class CreateOtherDocumentCommandHandler : IRequestHandler<CreateOtherDocumentCommand, long>
    {
        private readonly IMapper _mapper;
        private readonly CIRGeneralQueries _queries;
        private readonly OtherDocumentsContext _dbContext;

        public CreateOtherDocumentCommandHandler(OtherDocumentsContext dbContext, IMapper mapper, CIRGeneralQueries queries)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "OtherDocumentsContext cannot be null");
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper), "Mapper cannot be null");
            _queries = queries;
        }

        public async Task<long> Handle(CreateOtherDocumentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cir_Master = _mapper.Map<OtherDocumentMaster>(request.createOtherDocument);
                
                cir_Master.CreatedDate = DateTime.UtcNow;
                cir_Master.ModifiedBy = null;
                cir_Master.ModifiedDate = null;
                cir_Master.IsDeleted = 0;

                _dbContext.OtherDocumentMaster.Update(cir_Master);

                var cir_Blob = _mapper.Map<OtherDocument_Blob>(request.createOtherDocument);

                cir_Blob.OtherDocument_BlobID = (_dbContext.OtherDocument_Blob
                                     .Select(m => (long?)m.OtherDocument_BlobID)
                                     .Max() ?? 0) + 1;
                cir_Blob.OtherDocumentMasterID = cir_Master.OtherDocumentMasterID;
                cir_Blob.Origin = "HQ";
                cir_Blob.VesselID = -1;
                cir_Blob.HQ_Storage_Option = "Database";
                cir_Blob.HQ_Local_Folder = "";
                cir_Blob.HQ_DatabaseName = "";
                cir_Blob.AddedOn = DateTime.UtcNow;
                cir_Blob.Cloud_ContainerName = null;
                cir_Blob.Cloud_Directory = null;

                _dbContext.OtherDocument_Blob.Add(cir_Blob);

                await _dbContext.SaveChangesAsync(cancellationToken);

                string notificationType = OtherDocumentNotificationType.ADD, notificationTitle = "New " + request.createOtherDocument.DocumentNumber + " Added";
                string remarks = request.createOtherDocument.DocumentNumber + ": " + cir_Master.Reference;

                await _queries.AddToLog(cir_Master.OtherDocumentMasterID, "New " + request.createOtherDocument.DocumentNumber + " Added", remarks);

                await _queries.AddNotification(cir_Master.OtherDocumentMasterID, "", notificationType, notificationTitle, request.createOtherDocument.DocumentNumber + ": " + cir_Master.Reference);

                return cir_Master.OtherDocumentMasterID;
            }
            catch (Exception err)
            {
                throw err;
            }
        }
    }
}
