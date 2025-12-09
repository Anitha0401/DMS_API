namespace DMS.Application.Interface;

public interface IVesselService
{
    public Task<List<string>> GetVslTypeList();

}
