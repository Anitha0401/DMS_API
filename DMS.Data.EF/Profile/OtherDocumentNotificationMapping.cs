using AutoMapper;
using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.OtherDocument;

namespace DMS.Data.EF.Mapping;

internal class OtherDocumentNotificationMapping : Profile
{
    public OtherDocumentNotificationMapping()
    {
        CreateMap<VwOtherDocumentNotification, OtherDocumentNotificationDto>();
    }
}
