using DMS.Data.EF.DTO.Manuals;
using MediatR;

namespace DMS.Data.EF.Query.Manuals;

public class PublishManualCommand : IRequest<bool>
{
    public PublishManualDto Manual { get; set; }

    public PublishManualCommand(PublishManualDto manualDto)
    {
        Manual = manualDto;
    }
}
