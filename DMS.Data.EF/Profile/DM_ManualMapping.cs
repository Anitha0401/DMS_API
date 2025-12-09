using AutoMapper;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.Models.Manuals;

namespace DMS.Data.EF.Mapping;

internal class DM_ManualMapping : Profile
{
    public DM_ManualMapping()
    {
        // < From , To >
        CreateMap<CreateManualDto, DmManual>()
        .ForMember(d => d.ManualCode, opt => opt.MapFrom(src => src.ManualCode))
        .ForMember(d => d.ManualName, opt => opt.MapFrom(src => src.ManualName))
        .ForMember(d => d.DM_CategoryID, opt => opt.MapFrom(src => src.DM_CategoryID))
        .ForMember(d => d.IsMainTitle, opt => opt.MapFrom(src => src.IsMainTitle))
        .ForMember(d => d.IsAcknowledgementRequired, opt => opt.MapFrom(src => src.IsAcknowledgementRequired))
        .ForMember(d => d.CanExport, opt => opt.MapFrom(src => src.CanExport))
        .ForMember(d => d.IsApprovalRequired, opt => opt.MapFrom(src => src.IsApprovalRequired))
        .ForMember(d => d.Approval_FlowID, opt => opt.MapFrom(src => src.Approval_FlowID));

        CreateMap<CreateManualDto, DmManualVersion>()
        .ForMember(d => d.TextContents, opt => opt.MapFrom(src => src.TextContents))
        .ForMember(d => d.Comments, opt => opt.MapFrom(src => src.Comments));
    }
}
