using DMS.Data.EF.DTO.Circulars;

namespace DMS.Application.Interface;

public interface ICIRService
{
    public Task<List<Circulars_TreeviewDto>> GetTreeViewCircularsListAsync(string userID, string circularType);

    public Task<List<CircularsNotificationDto>> GetCIRNotificationListAsync(int limit);

    public Task AddToFavouriteAsync(long cir_MasterID, string circularType, string userID);
}
