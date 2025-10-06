using DMS.Data.EF.DTO;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;

namespace DMS.Application.Interface;

public interface IVesselService
{
    public Task<List<string>> GetVslTypeList();

}
