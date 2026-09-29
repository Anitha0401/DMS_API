using DMS.Application.DTO.Circulars;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Infrastructure.Query.Circulars;

public class UpdateCircularsCommand : IRequest<long>
{
    public EditCircularDto EditCIR { get; }

    public UpdateCircularsCommand(EditCircularDto editDto)
    {
        EditCIR = editDto;
    }
}