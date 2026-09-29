using AutoMapper;
using DMS.Application.DTO.OtherDocument;
using DMS.Data.EF.Models.OtherDocument;

namespace DMS.Data.EF.Mapping;

internal class OtherDocumentNotificationMappingProfile : Profile
{
    public OtherDocumentNotificationMappingProfile()
    {
        CreateMap<VwOtherDocumentNotification, OtherDocumentNotificationDto>();
    }
}
