using DMS.Application.DTO.Manuals;
using MediatR;

namespace DMS.Data.EF.Query.Manuals;

public class DeleteManualVersionCommand : IRequest<bool>
{
    public DeleteManualVersionDto Manual { get; set; }

    public DeleteManualVersionCommand(DeleteManualVersionDto manualDto)
    {
        Manual = manualDto;
    }
}
