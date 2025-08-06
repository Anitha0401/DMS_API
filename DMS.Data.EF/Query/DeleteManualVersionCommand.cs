using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class DeleteManualVersionCommand : IRequest<bool>
    {
        public DeleteManualVersionDto Manual { get; set; }

        public DeleteManualVersionCommand(DeleteManualVersionDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
