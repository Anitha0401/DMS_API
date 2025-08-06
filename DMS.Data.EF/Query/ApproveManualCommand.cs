using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class ApproveManualCommand : IRequest<bool>
    {
        public ApproveManualDto Manual { get; set; }

        public ApproveManualCommand(ApproveManualDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
