using AutoMapper;
using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;

namespace DMS.Application.Mappings;

public class CircularsMappingProfile : Profile
{
    public CircularsMappingProfile()
    {
        CreateMap<VwCIRMasterBlob, EditCircularDto>();
        CreateMap<CIR_Notification, CircularsNotificationDto>();
        CreateMap<CreateCircularDto, CIR_Master>();
    }
}