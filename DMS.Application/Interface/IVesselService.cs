using DMS.Application.DTO;

namespace DMS.Application.Interface;

public interface IVesselService
{
    public Task<List<string>> GetVslTypeList();

    public Task<List<VesselListDto>> GetVesselList();

    /// <summary>Manuals, circulars/alerts and other documents assigned to one vessel, with acknowledgement progress.</summary>
    public Task<List<VesselSyncItemDto>> GetVesselSyncItemsAsync(int vesselId);

    /// <summary>Sync runs for one vessel (empty, SyncInstalled = false, until the HQ sync add-on is installed).</summary>
    public Task<VesselSyncHistoryDto> GetVesselSyncHistoryAsync(int vesselId);
}
