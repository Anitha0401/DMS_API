namespace DMS.Data.EF.DTO;

public class UserConstants
{
    public static readonly List<UserModel> Users = new()
    {
        new UserModel { UserId = "admin", Password = "admin123", Role = "Admin" }
    };
}
