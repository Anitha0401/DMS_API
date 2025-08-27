using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class UpdateManualRightsCommand : IRequest<bool>
    {
        public UpdateManualRightsDto Manual { get; set; }

        public UpdateManualRightsCommand(UpdateManualRightsDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
