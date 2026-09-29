using DMS.Application.DTO.Circulars;
using MediatR;

namespace DMS.Data.EF.Query.Circulars;

public class CreateCircularsCommand : IRequest<long>
{
    public CreateCircularDto createCIR { get; set; }

    public CreateCircularsCommand(CreateCircularDto createCIRDto)
    {
        createCIR = createCIRDto;
    }
}
