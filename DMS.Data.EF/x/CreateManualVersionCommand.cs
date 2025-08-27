using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.x
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
