using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Models;

[Keyless]
public class UserManualCount
{
    public int? NewCnt { get; set; } = null!;
  
    public int? ApprovalCnt { get; set; } = null!;
 
    public int? ReviewCnt { get; set; } = null!;
    
    public int? AckCnt { get; set; } = null!;

    public int? FavouriteCnt { get; set; } = null!;
}
