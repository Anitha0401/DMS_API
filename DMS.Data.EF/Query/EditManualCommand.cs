using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class EditManualCommand : IRequest<bool>
    {
        public EditManualDto Manual { get; set; }

        public EditManualCommand(EditManualDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
