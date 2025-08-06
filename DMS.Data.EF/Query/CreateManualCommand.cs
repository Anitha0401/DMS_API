using MediatR;

namespace DMS.Data.EF.Query
{
    public class CreateManualCommand : IRequest<long>
    {
        public CreateManualDto Manual { get; set; }

        public CreateManualCommand(CreateManualDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
