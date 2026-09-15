using DMS.Data.EF.DTO.OtherDocument;
using MediatR;

namespace DMS.Data.EF.Query.Circulars;

public class CreateOtherDocumentCommand : IRequest<long>
{
    public CreateOtherDocumentDto createOtherDocument { get; set; }

    public CreateOtherDocumentCommand(CreateOtherDocumentDto createDto)
    {
        createOtherDocument = createDto;
    }
}
