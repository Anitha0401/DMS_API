using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class HQ_Users
{
    public long HQ_UsersID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string? HqDept { get; set; }

    public string UserID { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? Designation { get; set; }

    public string? Email { get; set; }

    public string? EmpID { get; set; }

    public string? PhoneNo { get; set; }

    public int? IsDisabled { get; set; }

    public DateTime? DateAdded { get; set; }

    public DateTime? DateDisabled { get; set; }
}
