namespace DMS.Application.Interface;

public sealed record HqLoginUser(string UserId, string UserName, string Role, string? Email, string? PasswordHash);


public interface IHQUserService
{
    public Task<List<string>> GetHQDeptList();

    public Task<List<string>> GetHQUserRoles();

    /// <summary>Active HQ user for login, or null if not found or disabled.</summary>
    public Task<HqLoginUser?> GetUserForLoginAsync(string userId);

    public Task<bool> SetPasswordHashAsync(string userId, string passwordHash);

}
