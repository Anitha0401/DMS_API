using DMS.Application.DTO.Manuals;
using MediatR;

namespace DMS.Data.EF.Query.Manuals;

public class ApproveManualCommand : IRequest<bool>
{
    public ManualApproveDto Manual { get; set; }

    public bool isToSendBack { get; set; }

    public ApproveManualCommand(ManualApproveDto manualDto, bool _isToSendBack)
    {
        Manual = manualDto;
        isToSendBack = _isToSendBack;
    }
}
