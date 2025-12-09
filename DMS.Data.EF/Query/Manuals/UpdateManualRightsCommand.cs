using DMS.Data.EF.DTO.Manuals;
using MediatR;

namespace DMS.Data.EF.Query.Manuals;

public class UpdateManualRightsCommand : IRequest<bool>
{
    public ManualRightsDto Manual { get; set; }

    public UpdateManualRightsCommand(ManualRightsDto manualDto)
    {
        Manual = manualDto;
    }
}
