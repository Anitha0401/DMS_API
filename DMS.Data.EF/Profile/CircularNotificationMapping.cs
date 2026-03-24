using AutoMapper;
using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.Manuals;

namespace DMS.Data.EF.Mapping;

internal class CircularNotificationMapping : Profile
{
    public CircularNotificationMapping()
    {
        CreateMap<VwCIRNotification, CircularsNotificationDto>();
    }
}
