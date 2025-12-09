using DMS.Data.EF.DTO.Manuals;
using MediatR;

namespace DMS.Data.EF.Query.Manuals;

public class DeleteManualCommand : IRequest<bool>
{
    public DeleteManualDto Manual { get; set; }

    public DeleteManualCommand(DeleteManualDto manualDto)
    {
        Manual = manualDto;
    }
}
