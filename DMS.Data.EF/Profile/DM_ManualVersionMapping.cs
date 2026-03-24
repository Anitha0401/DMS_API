using AutoMapper;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.Models.Manuals;

namespace DMS.Data.EF.Mapping;

internal class DmManualVersionMapping : Profile
{
    public DmManualVersionMapping()
    {
        CreateMap<CreateManualVersionDto, DmManualVersion>()
        .ForMember(d => d.TextContents, opt => opt.MapFrom(src => src.TextContents))
        .ForMember(d => d.Comments, opt => opt.MapFrom(src => src.Comments));
    }
}
