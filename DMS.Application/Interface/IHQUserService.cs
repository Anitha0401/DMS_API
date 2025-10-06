using DMS.Data.EF.DTO;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;

namespace DMS.Application.Interface;

public interface IHQUserService
{
    public Task<List<string>> GetHQDeptList();

    public Task<List<string>> GetHQUserRoles();

}
