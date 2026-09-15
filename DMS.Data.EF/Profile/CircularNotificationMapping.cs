using AutoMapper;
using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;

namespace DMS.Data.EF.Mapping;

internal class CircularNotificationMapping : Profile
{
    public CircularNotificationMapping()
    {
        CreateMap<VwCIRNotification, CircularsNotificationDto>();
    }
}
