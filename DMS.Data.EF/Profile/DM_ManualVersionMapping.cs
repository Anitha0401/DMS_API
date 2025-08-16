using AutoMapper;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;

namespace DMS.Data.EF.Mapping;

internal class DM_ManualVersionMapping : Profile
{
    public DM_ManualVersionMapping()
    {
        // < From , To >
        CreateMap<CreateManualVersionDto, DM_ManualVersion>()
        .ForMember(d => d.TextContents, opt => opt.MapFrom(src => src.TextContents))
        .ForMember(d => d.Comments, opt => opt.MapFrom(src => src.Comments));
    }
}
