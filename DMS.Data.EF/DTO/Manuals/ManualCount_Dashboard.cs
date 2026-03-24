using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.DTO.Manuals;

[Keyless]
public class ManualCount_Dashboard
{
    public int? NewCnt { get; set; } = null!;
  
    public int? ToApproveCnt { get; set; } = null!;
 
    public int? ReleasedCnt { get; set; } = null!;
    
    public int? ToAckCnt { get; set; } = null!;

    public int? UserToApproveCnt { get; set; } = null!;

    public int? UserToReviewCnt { get; set; } = null!;

    public int? UserToAckCnt { get; set; } = null!;

    public int? FavouriteCnt { get; set; } = null!;
}
