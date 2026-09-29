using AutoMapper;
using DMS.Application.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;

namespace DMS.Data.EF.Mapping;

internal class CirBlobMappingProfile : Profile
{
    public CirBlobMappingProfile()
    {
        CreateMap<CreateCircularDto, CIR_Blob>()
       .ForMember(d => d.Description, opt => opt.MapFrom(src => src.Description))
       .ForMember(d => d.FileName, opt => opt.MapFrom(src => src.FileName))
       .ForMember(d => d.FileType, opt => opt.MapFrom(src => src.FileType))
       .ForMember(d => d.OriginalFileType, opt => opt.MapFrom(src => src.OriginalFileType))
       .ForMember(d => d.BlobSize, opt => opt.MapFrom(src => src.BlobSize))
       .ForMember(d => d.BlobContents, opt => opt.MapFrom(src => src.BlobContents));

    }
}
