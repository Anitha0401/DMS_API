using AutoMapper;
using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.OtherDocument;

namespace DMS.Data.EF.Mapping;

internal class OtherDocumentMapping : Profile
{
    public OtherDocumentMapping()
    {
        CreateMap<CreateOtherDocumentDto, OtherDocumentMaster>()
       .ForMember(d => d.OtherDocument_TypeID, opt => opt.MapFrom(src => src.OtherDocument_TypeID))
       .ForMember(d => d.DocumentNumber, opt => opt.MapFrom(src => src.DocumentNumber))
       .ForMember(d => d.Reference, opt => opt.MapFrom(src => src.Reference))
       .ForMember(d => d.DocumentTitle, opt => opt.MapFrom(src => src.DocumentTitle))
       .ForMember(d => d.Status, opt => opt.MapFrom(src => src.Status))
       .ForMember(d => d.Comments, opt => opt.MapFrom(src => src.Comments))
       .ForMember(d => d.UploadedOn, opt => opt.MapFrom(src => src.UploadedOn))
       .ForMember(d => d.UploadedBy, opt => opt.MapFrom(src => src.UploadedBy));
    }
}
