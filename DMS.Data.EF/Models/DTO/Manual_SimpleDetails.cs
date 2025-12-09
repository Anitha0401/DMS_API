namespace DMS.Data.EF.Models.DTO;

public partial class Manual_SimpleDetails
{
    public string? ManualCode { get; set; }

    public string? ManualName { get; set; } = null!;

    public string? Version { get; set; }

    public string StatusString { get; set; } = null!;
}
