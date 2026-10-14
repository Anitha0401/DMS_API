using DMS.Application.DTO;

namespace DMS.Application.Interface;

public interface IVesselService
{
    public Task<List<string>> GetVslTypeList();

    public Task<List<VesselListDto>> GetVesselList();

    /// <summary>Manuals and circulars/alerts assigned to one vessel, with acknowledgement progress and whether the vessel has them.</summary>
    public Task<List<VesselSyncItemDto>> GetVesselSyncItemsAsync(int vesselId);

    /// <summary>Sync calls for one vessel (dbo.Sync_Log) and its sync key state. SyncInstalled = false until Database/005_VesselSync.sql is run.</summary>
    public Task<VesselSyncHistoryDto> GetVesselSyncHistoryAsync(int vesselId);

    /// <summary>Fleet overview for the sync monitor: link state, queued items and pending alerts per vessel.</summary>
    public Task<VesselSyncMonitorDto> GetSyncMonitorAsync();
}
