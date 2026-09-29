using AutoMapper;
using DMS.Application.DTO.Manuals;
using DMS.Data.EF.Models.Manuals;

namespace DMS.Data.EF.Mapping;

internal class ManualNotificationMappingProfile : Profile
{
    public ManualNotificationMappingProfile()
    {
        CreateMap<VwManualNotification, ManualNotificationDto>()
            .ForMember(dest => dest.ManualNotificationId, opt => opt.MapFrom(src => src.DM_ManualNotificationId))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom((src, dest) => src.IsRead.HasValue ? (src.IsRead.Value ? 1 : 0) : (int?)null));
    }
}
