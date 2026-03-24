using AutoMapper;
using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.Manuals;
using DMS.Data.EF.Models.OtherDocument;

namespace DMS.Data.EF.Mapping;

internal class Cir_OtherDocumentMapping : Profile
{
    public Cir_OtherDocumentMapping()
    {
        CreateMap<CreateOtherDocumentDto, CIR_OtherDocument>()
       .ForMember(d => d.CIR_OtherDocument_TypeID, opt => opt.MapFrom(src => src.CIR_OtherDocument_TypeID))
       .ForMember(d => d.DocumentNumber, opt => opt.MapFrom(src => src.DocumentNumber))
       .ForMember(d => d.Reference, opt => opt.MapFrom(src => src.Reference))
       .ForMember(d => d.DocumentTitle, opt => opt.MapFrom(src => src.DocumentTitle))
       .ForMember(d => d.Status, opt => opt.MapFrom(src => src.Status))
       .ForMember(d => d.Comments, opt => opt.MapFrom(src => src.Comments))
       .ForMember(d => d.UploadedOn, opt => opt.MapFrom(src => src.UploadedOn))
       .ForMember(d => d.UploadedBy, opt => opt.MapFrom(src => src.UploadedBy));
    }
}
