using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
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
