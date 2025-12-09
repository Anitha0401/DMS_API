using DMS.Data.EF.DTO.Manuals;
using MediatR;

namespace DMS.Data.EF.Query.Manuals;

public class CreateManualCommand : IRequest<CreateManualResultDto>
{
    public CreateManualDto Manual { get; set; }

    public CreateManualCommand(CreateManualDto manualDto)
    {
        Manual = manualDto;
    }
}
