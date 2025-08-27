using AutoMapper;
using DMS.Data.EF.DTO;
using DMS.Data.EF.Models;

namespace DMS.Data.EF.Mapping;

internal class DM_ManualMapping : Profile
{
    public DM_ManualMapping()
    {
        // < From , To >
        CreateMap<CreateManualDto, DM_Manual>()
        .ForMember(d => d.ManualCode, opt => opt.MapFrom(src => src.ManualCode))
        .ForMember(d => d.ManualName, opt => opt.MapFrom(src => src.ManualName))
        .ForMember(d => d.DM_CategoryID, opt => opt.MapFrom(src => src.DM_CategoryID));

        CreateMap<CreateManualDto, DM_ManualVersion>()
        .ForMember(d => d.TextContents, opt => opt.MapFrom(src => src.TextContents))
        .ForMember(d => d.Comments, opt => opt.MapFrom(src => src.Comments));
    }
}
