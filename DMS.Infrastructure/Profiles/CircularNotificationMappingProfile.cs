using AutoMapper;
using DMS.Application.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;

namespace DMS.Data.EF.Mapping;

internal class CircularNotificationMappingProfile : Profile
{
    public CircularNotificationMappingProfile()
    {
        CreateMap<VwCIRNotification, CircularsNotificationDto>()
            .ForMember(dest => dest.CIRNotificationId, opt => opt.MapFrom(src => src.CIR_NotificationID))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom((src, dest) => src.IsRead.HasValue ? (src.IsRead.Value ? 1 : 0) : (int?)null));
    }
}
