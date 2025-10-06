using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class ApproveManualCommand : IRequest<bool>
    {
        public ApproveManualDto Manual { get; set; }

        public bool isToSendBack { get; set; }

        public ApproveManualCommand(ApproveManualDto manualDto, bool _isToSendBack)
        {
            Manual = manualDto;
            this.isToSendBack = _isToSendBack;
        }
    }
}
