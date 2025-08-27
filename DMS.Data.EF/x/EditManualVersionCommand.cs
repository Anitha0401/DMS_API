using MediatR;

namespace DMS.Data.EF.x
{
    public class EditManualVersionCommand : IRequest<bool>
    {
        public EditManualVersionDto Manual { get; set; }

        public EditManualVersionCommand(EditManualVersionDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
