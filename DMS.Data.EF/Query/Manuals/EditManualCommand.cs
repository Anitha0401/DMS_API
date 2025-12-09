using DMS.Data.EF.DTO.Manuals;
using MediatR;

namespace DMS.Data.EF.Query.Manuals;

public class EditManualCommand : IRequest<CreateManualResultDto>
{
    public EditManualDto Manual { get; set; }

    public EditManualCommand(EditManualDto manualDto)
    {
        Manual = manualDto;
    }
}
