using System;
using System.Collections.Generic;

namespace DMS.Data.EF.DTO;

public partial class ManualRightsDto
{
    public long DM_ManualID { get; set; }

    public List<int>? VesselIdList { get; set; }

    public List<string>? VesselRankList { get; set; }

    public List<long>? HQ_UsersIDList { get; set; }
}
