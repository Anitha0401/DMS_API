using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class UpdateManualRightsCommand : IRequest<bool>
    {
        public ManualRightsDto Manual { get; set; }

        public UpdateManualRightsCommand(ManualRightsDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
