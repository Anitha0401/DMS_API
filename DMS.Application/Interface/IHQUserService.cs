namespace DMS.Application.Interface;

public interface IHQUserService
{
    public Task<List<string>> GetHQDeptList();

    public Task<List<string>> GetHQUserRoles();

}
