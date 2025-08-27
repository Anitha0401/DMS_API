using System;
using System.Collections.Generic;

namespace DMS.Data.EF.Models;

public partial class Dm_ManualRights
{
    public long DM_ManualID { get; set; }

    public List<int>? VesselIdList { get; set; }

    public List<string>? VesselRankList { get; set; }

    public List<long>? HQ_UsersID { get; set; }
}
