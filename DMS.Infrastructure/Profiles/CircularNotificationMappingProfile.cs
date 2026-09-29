using AutoMapper;
using DMS.Application.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;

namespace DMS.Data.EF.Mapping;

internal class CircularNotificationMappingProfile : Profile
{
    public CircularNotificationMappingProfile()
    {
        CreateMap<VwCIRNotification, CircularsNotificationDto>();
    }
}
