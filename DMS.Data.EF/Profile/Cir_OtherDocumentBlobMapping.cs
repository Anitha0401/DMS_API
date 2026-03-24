using AutoMapper;
using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.DTO.Manuals;
using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.Manuals;
using DMS.Data.EF.Models.OtherDocument;

namespace DMS.Data.EF.Mapping;

internal class Cir_OtherDocumentBlobMapping : Profile
{
    public Cir_OtherDocumentBlobMapping()
    {
        CreateMap<CreateOtherDocumentDto, CIR_OtherDocument_Blob>()
       .ForMember(d => d.Description, opt => opt.MapFrom(src => src.Description))
       .ForMember(d => d.FileName, opt => opt.MapFrom(src => src.FileName))
       .ForMember(d => d.FileType, opt => opt.MapFrom(src => src.FileType))
       .ForMember(d => d.OriginalFileType, opt => opt.MapFrom(src => src.OriginalFileType))
       .ForMember(d => d.BlobSize, opt => opt.MapFrom(src => src.BlobSize))
       .ForMember(d => d.BlobContents, opt => opt.MapFrom(src => src.BlobContents));

    }
}
