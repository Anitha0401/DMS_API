namespace DMS.Application.DTO;

/// <summary>One manual, circular/alert or other document assigned to a vessel.</summary>
public class VesselSyncItemDto
{
    /// <summary>Manual | Circular | Alert | Other document</summary>
    public string ItemType { get; set; } = string.Empty;
    public long ItemID { get; set; }
    public string? Number { get; set; }
    public string? Title { get; set; }
    public string? Version { get; set; }
    public string? Category { get; set; }
    public DateTime? IssuedOn { get; set; }
    /// <summary>Ranks on board that must acknowledge (0 = no acknowledgement needed).</summary>
    public int RanksRequired { get; set; }
    public int RanksAcknowledged { get; set; }
    public DateTime? LastReadOn { get; set; }
    /// <summary>Acknowledged | Partly acknowledged | Pending | Sent</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Sync runs for a vessel, read from dbo.Sync_Package (created by the HQ sync add-on).</summary>
public class VesselSyncHistoryDto
{
    /// <summary>False when the HQ sync add-on is not installed (no dbo.Sync_Package table).</summary>
    public bool SyncInstalled { get; set; }
    public List<VesselSyncPackageDto> Packages { get; set; } = new();
}

public class VesselSyncPackageDto
{
    public Guid PackageId { get; set; }
    /// <summary>Up (vessel to HQ) | Down (HQ to vessel)</summary>
    public string Direction { get; set; } = string.Empty;
    public long FromVersion { get; set; }
    public long ToVersion { get; set; }
    public int RowsCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? AppliedUtc { get; set; }
    public string? Error { get; set; }
}
