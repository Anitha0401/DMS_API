using Microsoft.EntityFrameworkCore;

namespace DMS.Application.DTO.Manuals;

[Keyless]
public class ManualApprovalRoleListDto
{
    public int FlowID { get; set; }

    public string ApprovalFlow { get; set; } = null!;
}
