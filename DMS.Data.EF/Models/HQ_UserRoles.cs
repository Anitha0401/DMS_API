using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class HQ_UserRoles
{
    public long HQ_UserRolesID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string HQDept { get; set; } = null!;

    public string UserRole { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public int? ApprovalOrder { get; set; }

    public int? IsDisabled { get; set; }
}
