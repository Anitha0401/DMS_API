using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class EditManualCommand : IRequest<CreateManualResultDto>
    {
        public EditManualDto Manual { get; set; }

        public EditManualCommand(EditManualDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
