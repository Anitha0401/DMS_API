using MediatR;

namespace DMS.Data.EF.x
{
    public class EditManualCommandxx : IRequest<bool>
    {
        public EditManualxxDto Manual { get; set; }

        public EditManualCommandxx(EditManualxxDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
