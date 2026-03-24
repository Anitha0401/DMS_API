using AutoMapper;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.Models.Manuals;

namespace DMS.Data.EF.Mapping;

internal class ManualNotificationMapping : Profile
{
    public ManualNotificationMapping()
    {
        CreateMap<VwManualNotification, ManualNotificationDto>();
    }
}
