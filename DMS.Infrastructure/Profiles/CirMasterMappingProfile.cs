using AutoMapper;
using DMS.Application.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;

namespace DMS.Data.EF.Mapping;

internal class CirMasterMappingProfile : Profile
{
    public CirMasterMappingProfile()
    {
        CreateMap<CreateCircularDto, CIR_Master>()
       .ForMember(d => d.CIR_CategoryID, opt => opt.MapFrom(src => src.CIR_CategoryID))
       .ForMember(d => d.CircularType, opt => opt.MapFrom(src => src.CircularType))
       .ForMember(d => d.CIR_Number, opt => opt.MapFrom(src => src.CIR_Number))
       .ForMember(d => d.Reference, opt => opt.MapFrom(src => src.Reference))
       .ForMember(d => d.Title, opt => opt.MapFrom(src => src.Title))
       .ForMember(d => d.Status, opt => opt.MapFrom(src => src.Status))
       .ForMember(d => d.Priority, opt => opt.MapFrom(src => src.Priority))
       .ForMember(d => d.DateIssued, opt => opt.MapFrom(src => src.DateIssued))
       .ForMember(d => d.Remarks, opt => opt.MapFrom(src => src.Remarks))
       .ForMember(d => d.ReleasedDate, opt => opt.MapFrom(src => src.ReleasedDate))
       .ForMember(d => d.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
       .ForMember(d => d.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate));

    }
}
