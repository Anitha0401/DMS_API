using AutoMapper;
using DMS.Data.EF.DTO;
using DMS.Data.EF.Models.Manuals;

namespace DMS.Data.EF.Mapping;

internal class NotificationMapping : Profile
{
    public NotificationMapping()
    {
        // < From , To >
        CreateMap<VwManualNotification, NotificationDto>();
    }
}
