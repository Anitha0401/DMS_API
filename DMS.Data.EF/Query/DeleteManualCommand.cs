using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class DeleteManualCommand : IRequest<bool>
    {
        public DeleteManualDto Manual { get; set; }

        public DeleteManualCommand(DeleteManualDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
