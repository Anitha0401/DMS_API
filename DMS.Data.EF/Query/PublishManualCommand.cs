using DMS.Data.EF.DTO;
using MediatR;

namespace DMS.Data.EF.Query
{
    public class PublishManualCommand : IRequest<bool>
    {
        public PublishManualDto Manual { get; set; }

        public PublishManualCommand(PublishManualDto manualDto)
        {
            Manual = manualDto;
        }
    }
}
