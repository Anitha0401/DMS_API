using System.Data;
using System.Data.Common;
using System.Reflection;
using DMS.Data.EF.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DMS.Data.EF.Features;

/// <summary>
/// Plain SQL on the office database for the new feature tables (Database/012_NewFeatures.sql), which are not mapped
/// in the EF contexts. Uses GeneralContext's connection (and its transaction, when one is open). Rows map onto a
/// class by column name = property name (case-insensitive). New keys come from Proc_NextID.
/// </summary>
public sealed class SqlDb
{
    private readonly GeneralContext _ctx;

    public SqlDb(GeneralContext ctx) => _ctx = ctx;

    public const string Office = "HQ";
    /// <summary>VesselID of office-only rows: never synced to a vessel.</summary>
    public const int OfficeOnly = -2;
    public const int AllVessels = -1;

    public Task<long> NextIdAsync(string table, string origin = Office, int vesselId = -1, int count = 1)
        => _ctx.NextIdAsync(table, origin, vesselId, count);

    public async Task<List<T>> QueryAsync<T>(string sql, params (string Name, object? Value)[] ps) where T : new()
    {
        var result = new List<T>();
        await RunAsync(sql, ps, async cmd =>
        {
            using var r = await cmd.ExecuteReaderAsync();
            var map = Mapper<T>(r);
            while (await r.ReadAsync()) result.Add(map(r));
        });
        return result;
    }

    public async Task<T?> FirstAsync<T>(string sql, params (string Name, object? Value)[] ps) where T : class, new()
        => (await QueryAsync<T>(sql, ps)).FirstOrDefault();

    public async Task<object?> ScalarAsync(string sql, params (string Name, object? Value)[] ps)
    {
        object? value = null;
        await RunAsync(sql, ps, async cmd => value = await cmd.ExecuteScalarAsync());
        return value is DBNull ? null : value;
    }

    public async Task<int> ExecuteAsync(string sql, params (string Name, object? Value)[] ps)
    {
        var n = 0;
        await RunAsync(sql, ps, async cmd => n = await cmd.ExecuteNonQueryAsync());
        return n;
    }

    /// <summary>A setting value from DM_Settings or CIR_Settings (the office row, VesselID -1, first).</summary>
    public async Task<string?> SettingAsync(string table, string name)
    {
        if (table != "DM_Settings" && table != "CIR_Settings") throw new ArgumentException("Unknown settings table.", nameof(table));
        return (string?)await ScalarAsync(
            $"SELECT TOP (1) SettingValue FROM dbo.{table} WHERE SettingName = @n ORDER BY CASE WHEN VesselID = -1 THEN 0 ELSE 1 END",
            ("@n", name));
    }

    public async Task SetSettingAsync(string table, string name, string? value)
    {
        if (table != "DM_Settings" && table != "CIR_Settings") throw new ArgumentException("Unknown settings table.", nameof(table));
        var updated = await ExecuteAsync($"UPDATE dbo.{table} SET SettingValue = @v WHERE SettingName = @n AND VesselID = -1",
            ("@n", name), ("@v", value));
        if (updated > 0) return;
        var key = table == "DM_Settings" ? "DM_SettingsID" : "CIR_SettingsID";
        var id = await NextIdAsync(table);
        await ExecuteAsync($"INSERT dbo.{table} ({key}, Origin, VesselID, SettingName, SettingValue) VALUES (@id, 'HQ', -1, @n, @v)",
            ("@id", id), ("@n", name), ("@v", value));
    }

    /// <summary>Runs work in one transaction on this connection (committed when it returns, rolled back if it throws).</summary>
    public async Task InTransactionAsync(Func<Task> work)
    {
        if (_ctx.Database.CurrentTransaction != null) { await work(); return; }
        await using var tx = await _ctx.Database.BeginTransactionAsync();
        await work();
        await tx.CommitAsync();
    }

    private async Task RunAsync(string sql, (string Name, object? Value)[] ps, Func<DbCommand, Task> work)
    {
        var connection = _ctx.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 120;
            cmd.Transaction = _ctx.Database.CurrentTransaction?.GetDbTransaction();
            foreach (var (name, value) in ps)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value = value ?? DBNull.Value;
                if (value is DateTime) p.DbType = DbType.DateTime2;
                if (value is byte[]) p.DbType = DbType.Binary;
                cmd.Parameters.Add(p);
            }
            await work(cmd);
        }
        finally
        {
            if (openedHere && _ctx.Database.CurrentTransaction == null) await connection.CloseAsync();
        }
    }

    /// <summary>Column name -> property setter for T, built once per reader.</summary>
    private static Func<DbDataReader, T> Mapper<T>(DbDataReader r) where T : new()
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var setters = new List<(int Ordinal, PropertyInfo Prop, Type Target)>();
        for (var i = 0; i < r.FieldCount; i++)
        {
            if (!props.TryGetValue(r.GetName(i), out var prop)) continue;
            setters.Add((i, prop, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));
        }
        return reader =>
        {
            var item = new T();
            foreach (var (ordinal, prop, target) in setters)
            {
                if (reader.IsDBNull(ordinal)) continue;
                var value = reader.GetValue(ordinal);
                prop.SetValue(item, target == typeof(bool) ? Convert.ToBoolean(value)
                    : target.IsEnum ? Enum.ToObject(target, value)
                    : value.GetType() == target ? value
                    : Convert.ChangeType(value, target));
            }
            return item;
        };
    }
}
