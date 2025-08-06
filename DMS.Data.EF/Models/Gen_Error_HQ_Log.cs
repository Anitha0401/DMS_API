using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class Gen_Error_HQ_Log
{
    public long Gen_Error_HQ_LogID { get; set; }

    public string Origin { get; set; } = null!;

    public int VesselID { get; set; }

    public string ApplicationName { get; set; } = null!;

    public string PageName { get; set; } = null!;

    public string FunctionName { get; set; } = null!;

    public string? TextContents { get; set; }

    public string LoggedInUserID { get; set; } = null!;

    public DateTime AddedOn { get; set; }
}
