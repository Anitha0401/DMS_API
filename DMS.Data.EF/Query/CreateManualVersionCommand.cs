using MediatR;

namespace DMS.Data.EF.Query
{
    public class CreateManualVersionCommand : IRequest<long>
    {
        public CreateManualVersionDto manualVersion { get; set; }

        public CreateManualVersionCommand(CreateManualVersionDto manualVersionDto)
        {
            manualVersion = manualVersionDto;
        }
    }
}
