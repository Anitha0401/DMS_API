using AutoMapper;
using DMS.Application.DTO.Circulars;

namespace DMS.Data.EF.Mapping;

internal class CircularMappingProfile : Profile
{
    public CircularMappingProfile()
    {
        CreateMap<VwCIRMasterBlob, EditCircularDto>()
            .ForMember(
                destination => destination.CIR_CategoryID,
                options => options.MapFrom(source => source.CIR_CategoryID ?? 0))
            .ForMember(
                destination => destination.AllowedUserToAck,
                options => options.Ignore())
            .ForMember(
                destination => destination.CalledMode,
                options => options.Ignore());
    }
}