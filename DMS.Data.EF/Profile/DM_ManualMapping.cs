using AutoMapper;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;

namespace DMS.Data.EF.Mapping;

internal class DM_ManualMapping : Profile
{
    public DM_ManualMapping()
    {
        // < From , To >
        CreateMap<CreateManualDto, DM_Manual>()
        .ForMember(d => d.ManualCode, opt => opt.MapFrom(src => src.ManualCode))
        .ForMember(d => d.ManualLevel, opt => opt.MapFrom(src => src.ManualLevel))
        .ForMember(d => d.ManualName, opt => opt.MapFrom(src => src.ManualName))
        .ForMember(d => d.DM_CategoryID, opt => opt.MapFrom(src => src.DM_CategoryID))
        .ForMember(d => d.CanExport, opt => opt.MapFrom(src => src.CanExport))
        .ForMember(d => d.VesselID, opt => opt.MapFrom(src => src.VesselID))
        .ForMember(d => d.Origin, opt => opt.MapFrom(src => src.Origin))
        .ForMember(d => d.ParentID, opt => opt.MapFrom(src => src.ParentID))
        .ForMember(d => d.ManualLevel, opt => opt.MapFrom(src => src.ManualLevel))
        .ForMember(d => d.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
        .ForMember(d => d.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
        .ForMember(d => d.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted));

        CreateMap<CreateManualDto, DM_ManualVersion>()
        .ForMember(d => d.VesselID, opt => opt.MapFrom(src => src.VesselID))
        .ForMember(d => d.Origin, opt => opt.MapFrom(src => src.Origin))
        .ForMember(d => d.TextContents, opt => opt.MapFrom(src => src.TextContents))
        .ForMember(d => d.Comments, opt => opt.MapFrom(src => src.Comments))
        .ForMember(d => d.ReleasedBy, opt => opt.MapFrom(src => src.ReleasedBy))
        .ForMember(d => d.ReleasedDate, opt => opt.MapFrom(src => src.ReleasedDate))
        .ForMember(d => d.IsCurrent, opt => opt.MapFrom(src => src.IsCurrent))
        .ForMember(d => d.IsActive, opt => opt.MapFrom(src => src.IsActive));
    }
}
