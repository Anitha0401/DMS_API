using Microsoft.EntityFrameworkCore;

namespace DMS.Application.DTO.Manuals;

[Keyless]
public class ManualCount_User
{
    public int? NewCnt { get; set; } = null!;
  
    public int? ApprovalCnt { get; set; } = null!;
 
    public int? ReviewCnt { get; set; } = null!;
    
    public int? AckCnt { get; set; } = null!;

    public int? FavouriteCnt { get; set; } = null!;
}
