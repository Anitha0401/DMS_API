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
        .ForMember(d => d.VesselID, opt => opt.MapFrom(src => src.VesselID))
        .ForMember(d => d.Origin, opt => opt.MapFrom(src => src.Origin))
        .ForMember(d => d.TextContents, opt => opt.MapFrom(src => src.TextContents))
        .ForMember(d => d.Comments, opt => opt.MapFrom(src => src.Comments))
        .ForMember(d => d.ReleasedBy, opt => opt.MapFrom(src => src.ReleasedDate))
        .ForMember(d => d.ReleasedDate, opt => opt.MapFrom(src => src.ReleasedDate))
        .ForMember(d => d.IsCurrent, opt => opt.MapFrom(src => src.IsCurrent))
        .ForMember(d => d.IsActive, opt => opt.MapFrom(src => src.IsActive));
    }
}
