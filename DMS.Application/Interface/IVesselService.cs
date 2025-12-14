using DMS.Data.EF.DTO;

namespace DMS.Application.Interface;

public interface IVesselService
{
    public Task<List<string>> GetVslTypeList();

    public Task<List<VesselListDto>> GetVesselList();
}
