using DMS.Data.EF.Models;

namespace DMS.Data.EF.Query;

public interface IGeneralQueries
{
    public Task<List<Manual_TreeviewEntity>> GetDmManualTreeviewAsync();

}
