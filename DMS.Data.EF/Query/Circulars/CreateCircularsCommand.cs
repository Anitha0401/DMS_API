using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.DTO.Manuals;
using MediatR;

namespace DMS.Data.EF.Query.Manuals;

public class CreateCircularsCommand : IRequest<long>
{
    public CreateCircularDto createCIR { get; set; }

    public CreateCircularsCommand(CreateCircularDto createCIRDto)
    {
        createCIR = createCIRDto;
    }
}
