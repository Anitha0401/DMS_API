using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DMS.Data.EF.Query;

public class GeneralQueries : IGeneralQueries
{
    private readonly DmsContext _dmsContext;
    private readonly IConfiguration _configuration;

    public GeneralQueries(DmsContext dmsContext, IConfiguration config)
    {
        _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(_dmsContext));
        _configuration = config ?? throw new ArgumentNullException("config");
    }

    public async Task<List<Manual_TreeviewEntity>> GetDmManualTreeviewAsyncs()
    {
        var result = new List<Manual_TreeviewEntity>();

        var connStr = _configuration.GetConnectionString("LocalDBConnection");
        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(); // Does this throw?

        var connection = _dmsContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "GetTreeViewManualList";
        command.CommandType = CommandType.StoredProcedure;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new Manual_TreeviewEntity
            {
                DM_ManualID = reader.GetInt64(0),
                ManualNo = reader.GetString(1),
                ManualCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                ManualName = reader.GetString(3),
                ParentID = reader.GetInt64(4),
                ManualLevel = reader.GetInt32(5),
                SortOrder = reader.IsDBNull(6) ? null : reader.GetString(6),
                AddlSortOrder = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return result;
    }

    public async Task<List<Manual_TreeviewEntity>> GetDmManualTreeviewAsync()
    {
        var result = await _dmsContext
            .DmManual_Treeview
            .FromSqlInterpolated($"EXEC GetTreeViewManualList")
            .AsNoTracking()
            .ToListAsync();

        return result ?? new List<Manual_TreeviewEntity>();
    }

    public async Task AddToLog(long dm_ManualID, long dm_ManualVersionID, string activity, string remarks)
    {
        var dm_Manual_LogID = (_dmsContext.DmManualLogs
                                .Select(m => (long?)m.DM_Manual_LogID)
                                .Max() ?? 0) + 1;

        var manualLog = new DM_Manual_Log
        {
            DM_Manual_LogID = dm_Manual_LogID,
            Origin = "HQ",
            VesselID = -1,
            DM_ManualID = dm_ManualID,
            DM_ManualVersionID = dm_ManualVersionID,
            Activity = activity,
            Remarks = remarks,
            UserName = "",
            UserRank = "",
            Logdate = DateTime.Now,
        };

        _dmsContext.DmManualLogs.Add(manualLog);
        await _dmsContext.SaveChangesAsync();
    }

    public async Task<string?> GetDmManualContentAsync(long dm_ManualID)
    {
        var result = await _dmsContext.DmManualContent
                           .FromSqlInterpolated($"EXEC GetManualAndSubManualContent @DM_ManualID = {dm_ManualID}")
                           .AsNoTracking()
                           .ToListAsync();

        var content = result[0]?.ManualContent;
        if (content == null) content = "";
        return content;
    }

    public async Task<string?> GetDmManualContent_ScalarAsync(long dm_ManualID)
    {
        var connection = _dmsContext.Database.GetDbConnection();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "EXEC GetManualAndSubManualContent @DM_ManualID";
            command.CommandType = System.Data.CommandType.Text;

            var rootParam = command.CreateParameter();
            rootParam.ParameterName = "@DM_ManualID";
            rootParam.Value = dm_ManualID;
            command.Parameters.Add(rootParam);

            if (connection.State == System.Data.ConnectionState.Closed)
                await connection.OpenAsync();

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
    }

    public async Task<bool> CheckDmManualAsync(int manualID)
    {
        return await _dmsContext.DmManuals.AnyAsync(e => e.DM_ManualID == manualID);
    }
}
