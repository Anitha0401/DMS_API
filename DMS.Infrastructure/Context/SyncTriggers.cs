using DMS.Application.DTO.Circulars;
using DMS.Application.DTO.Manuals;
using DMS.Data.EF.Models;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.Manuals;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Context;

// Tables with the vessel sync triggers from Database/006_VesselSyncOutbox.sql.
// EF Core 8 must know about them: without HasTrigger it saves with an OUTPUT clause, which SQL Server
// refuses on a table with triggers ("The target table ... cannot have any enabled triggers ...").

public partial class DmsContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DmManual>().ToTable(t => t.HasTrigger("TR_DM_Manual_Sync"));
        modelBuilder.Entity<DmManualVersion>().ToTable(t => t.HasTrigger("TR_DM_ManualVersion_Sync"));
        modelBuilder.Entity<DmManualAttachment>().ToTable(t => t.HasTrigger("TR_DM_Manual_Attachments_Sync"));
        modelBuilder.Entity<DmManualToAckVsl>().ToTable(t => t.HasTrigger("TR_DM_Manual_ToAck_Vsl_Sync"));
        modelBuilder.Entity<DmCategory>().ToTable(t => t.HasTrigger("TR_DM_Category_Sync"));
    }
}

public partial class CircularsContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CIR_Master>().ToTable(t => t.HasTrigger("TR_CIR_Master_Sync"));
        modelBuilder.Entity<CIR_Blob>().ToTable(t => t.HasTrigger("TR_CIR_Blob_Sync"));
        modelBuilder.Entity<CIR_ToAck_Vsl>().ToTable(t => t.HasTrigger("TR_CIR_ToAck_Vsl_Sync"));
        modelBuilder.Entity<CIR_Category>().ToTable(t => t.HasTrigger("TR_CIR_Category_Sync"));
    }
}

public partial class GeneralContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VslStaff>().ToTable(t => t.HasTrigger("TR_Vsl_Staff_Sync"));
    }
}
