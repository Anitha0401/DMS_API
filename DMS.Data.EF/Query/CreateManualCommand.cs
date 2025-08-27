using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class CreateManualCommand : IRequest<CreateManualResultDto>
    {
        public CreateManualDto Manual { get; set; }

        public CreateManualCommand(CreateManualDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
