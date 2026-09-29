using Microsoft.EntityFrameworkCore;

namespace DMS.Application.DTO.Circulars;

[Keyless]
public class CircularCount_Dashboard
{
    public int TotalCirculars { get; set; }

    public int TotalAlerts { get; set; }
}