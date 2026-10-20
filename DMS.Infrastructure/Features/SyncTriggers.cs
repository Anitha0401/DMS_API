using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Context;

// Tables with the vessel sync triggers TR_<table>_SyncRow from Database/010_Sync_ChangeLog.sql (one per table in
// dbo.Sync_Table). EF Core 8 must know about them: without HasTrigger it saves with an OUTPUT clause, which SQL
// Server refuses on a table with triggers ("The target table ... cannot have any enabled triggers ...").
// Keep SyncedTables in step with dbo.Sync_Table.

internal static class SyncRowTriggers
{
    public static readonly HashSet<string> SyncedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "V_Vessel", "DM_Status", "DM_Settings", "CIR_Settings", "Vsl_Staff",
        "DM_Category", "DM_Manual", "DM_ManualVersion", "DM_Manual_Attachments", "DM_Manual_ToAck_Vsl", "DM_Manual_UserAck",
        "CIR_Category", "CIR_Master", "CIR_Blob", "CIR_ToAck_Vsl", "CIR_UserAck",
        "OtherDocument_Type", "OtherDocumentMaster", "OtherDocument_Blob",
    };

    /// <summary>Declares the sync trigger on every entity of this context that is mapped to a synced table.</summary>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var table = entityType.GetTableName();
            if (table == null || entityType.IsOwned() || !SyncedTables.Contains(table)) continue;
            modelBuilder.Entity(entityType.ClrType).ToTable(t => t.HasTrigger($"TR_{table}_SyncRow"));
        }
    }
}

public partial class DmsContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder) => SyncRowTriggers.Apply(modelBuilder);
}

public partial class CircularsContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder) => SyncRowTriggers.Apply(modelBuilder);
}

public partial class GeneralContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder) => SyncRowTriggers.Apply(modelBuilder);
}

public partial class OtherDocumentsContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder) => SyncRowTriggers.Apply(modelBuilder);
}
