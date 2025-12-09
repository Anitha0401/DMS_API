namespace DMS.Data.EF.Models.DTO;

public class UserConstants
{
    public static readonly List<UserModel> Users = new()
    {
        new UserModel { UserId = "admin", Password = "admin123", Role = "Admin" }
    };
}
