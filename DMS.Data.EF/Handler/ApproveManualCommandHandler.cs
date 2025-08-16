using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO;
using DMS.Data.EF.Query;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Handler
{
    public class ApproveManualCommandHandler : IRequestHandler<ApproveManualCommand, bool>
    {
        private readonly IMediator _mediator;
        DmsContext _dmsContext;

        public ApproveManualCommandHandler(DmsContext dmsContext, IMediator mediator)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _mediator = mediator;
        }

        public async Task<bool> Handle(ApproveManualCommand request, CancellationToken cancellationToken)
        {
            var dm_StatusID = (long)_dmsContext.DmManuals
                                     .Where(c => c.DM_ManualID == request.Manual.DM_ManualID)
                                     .Select(x => x.DM_StatusID)
                                     .FirstOrDefault();

            // Get the next status ID based on the current status and user role
            var nextApproval = await (from s in _dmsContext.DmManualApprovalSettings
                                          join r in _dmsContext.HqUserRoles on s.UserRole equals r.UserRole
                                          where s.DM_StatusID == dm_StatusID && r.UserRole != request.Manual.UserRole
                                          select s).FirstOrDefaultAsync();
            if (nextApproval != null)
            {
                // Send Approval mail
            }
            else
            {
                // Get the next status ID based on the current status and user role
                var next_DM_StatusID = await (from s in _dmsContext.DmManualApprovalSettings
                                              join r in _dmsContext.HqUserRoles on s.UserRole equals r.UserRole
                                              where s.DM_StatusID == dm_StatusID
                                              select s.Next_DM_StatusID).FirstOrDefaultAsync();
                if (next_DM_StatusID == 0)
                {
                    //Call Publish 
                    var publishDto = new PublishManualDto
                    {
                        DM_ManualID = request.Manual.DM_ManualID,
                        DM_ManualVersionID = request.Manual.DM_ManualVersionID,
                        UserID = request.Manual.UserID,
                        UserRole = request.Manual.UserRole,
                        Comments = request.Manual.Comments
                    };
                        
                    await _mediator.Send(new PublishManualCommand(publishDto));
                }
                else
                {
                    // Send Approval mail
                }
            }
         
            return true;
        }
    }
}
