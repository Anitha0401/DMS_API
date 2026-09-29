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
                var dto = request.createOtherDocument;
                var origin = string.IsNullOrWhiteSpace(dto.Origin) ? "HQ" : dto.Origin.Trim().ToUpperInvariant();
                var vesselId = dto.VesselID;
                var nextMasterId = (_dbContext.OtherDocumentMaster
                    .Select(m => (long?)m.OtherDocumentMasterID)
                    .Max() ?? 0) + 1;

                var otherDocument = new OtherDocumentMaster
                {
                    OtherDocumentMasterID = nextMasterId,
                    Origin = origin,
                    VesselID = vesselId,
                    OtherDocument_TypeID = (int?)dto.OtherDocument_TypeID,
                    DocumentNumber = dto.DocumentNumber ?? dto.DocumentTitle ?? "DOC-" + nextMasterId,
                    DocumentTitle = dto.DocumentTitle ?? dto.Title ?? string.Empty,
                    Status = dto.Status ?? "Active",
                    Reference = dto.Reference,
                    Comments = dto.Comments,
                    UploadedOn = dto.UploadedOn ?? DateTime.UtcNow,
                    UploadedBy = dto.UploadedBy,
                    IsDeleted = 0,
                    CreatedBy = dto.UploadedBy,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedBy = null,
                    ModifiedDate = null
                };

                _dbContext.OtherDocumentMaster.Add(otherDocument);

                var cir_Blob = new OtherDocument_Blob
                {
                    OtherDocument_BlobID = (_dbContext.OtherDocument_Blob
                        .Select(m => (long?)m.OtherDocument_BlobID)
                        .Max() ?? 0) + 1,
                    OtherDocumentMasterID = otherDocument.OtherDocumentMasterID,
                    Origin = origin,
                    VesselID = vesselId,
                    Description = dto.Description ?? dto.Comments ?? dto.DocumentTitle ?? dto.Title,
                    FileName = dto.FileName,
                    FileType = dto.FileType,
                    OriginalFileType = dto.OriginalFileType,
                    BlobSize = dto.BlobSize,
                    BlobContents = dto.BlobContents,
                    AddedOn = DateTime.UtcNow,
                    Cloud_ContainerName = null,
                    Cloud_Directory = null,
                    HQ_Storage_Option = origin == "HQ" ? "Database" : null,
                    HQ_Local_Folder = origin == "HQ" ? "" : null,
                    HQ_DatabaseName = origin == "HQ" ? "" : null,
                    VSL_Storage_Option = origin == "VSL" ? "Database" : null,
                    VSL_Local_Folder = origin == "VSL" ? "" : null,
                    VSL_DatabaseName = origin == "VSL" ? "" : null
                };

                _dbContext.OtherDocument_Blob.Add(cir_Blob);

                await _dbContext.SaveChangesAsync(cancellationToken);

                string notificationType = OtherDocumentNotificationType.ADD, notificationTitle = "New " + otherDocument.DocumentNumber + " Added";
                string remarks = otherDocument.DocumentNumber + ": " + otherDocument.Reference;

                await _queries.AddToLog(otherDocument.OtherDocumentMasterID, "New " + otherDocument.DocumentNumber + " Added", remarks);

                await _queries.AddNotification(otherDocument.OtherDocumentMasterID, "", notificationType, notificationTitle, otherDocument.DocumentNumber + ": " + otherDocument.Reference);

                return otherDocument.OtherDocumentMasterID;
            }
            catch (Exception err)
            {
                throw err;
            }
        }
    }
}
