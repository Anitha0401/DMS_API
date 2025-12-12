using DMS.Data.EF.DTO.Circulars;
using DMS.Data.EF.Models.Circulars;
using DMS.Data.EF.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Context;

public partial class CircularsContext : DbContext
{
    private DBConnectionString _dbActions;

    public CircularsContext(DbContextOptions<CircularsContext> options, DBConnectionString dbActions) : base(options)
    {
        _dbActions = dbActions;
    }

    public virtual DbSet<CIR_TreeviewEntity> CIR_Treeview { get; set; } = null!;

    public virtual DbSet<CIR_Blob> CIR_Blob { get; set; } = null!;
    
    public virtual DbSet<CIR_Category> CIR_Category { get; set; } = null!;

    public virtual DbSet<CIR_CategoryExt> CIR_CategoryExt { get; set; } = null!;

    public virtual DbSet<CIR_Log> CIR_Log { get; set; } = null!;

    public virtual DbSet<CIR_Master> CIR_Master { get; set; } = null!;

    public virtual DbSet<CIR_UserAck> CIR_UserAck { get; set; } = null!;

    public virtual DbSet<CIR_ToAck_Vsl> CIR_ToAck_Vsl { get; set; } = null!;

    public virtual DbSet<CIR_ToAck_HQ> CIR_ToAck_HQ { get; set; } = null!;

    public virtual DbSet<CIR_UserFavourite> CIR_UserFavourite { get; set; }

    public virtual DbSet<CIR_Settings> CIR_Settings { get; set; }

    public virtual DbSet<CIR_Notification> CIR_Notification { get; set; }

    public virtual DbSet<VwCIRMasterBlob> VwCIRMasterBlob { get; set; }

    public virtual DbSet<VwCIRNotification> VwCIRNotification { get; set; }

    public virtual DbSet<VwCIRVesselAck> Vw_CIR_VesselAck { get; set; }


    public virtual DbSet<CIRMasterList> CIRMasterList { get; set; }

    public virtual DbSet<CircularsChart> CircularsChart { get; set; }

    public virtual DbSet<CircularCount_Dashboard> CircularCount_Dashboard { get; set; } = null!;

    public virtual DbSet<CIRRecentList> CIRRecentList { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(_dbActions.DefaultConnection);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<CIR_Category>(entity =>
        {
            entity.HasKey(e => e.CIR_CategoryID).HasName("PK_CIR_Category");

            entity.Property(e => e.CIR_CategoryID).ValueGeneratedNever();
            entity.Property(e => e.Category).HasDefaultValue("");
            entity.Property(e => e.isActive).HasDefaultValue(0);
        });

        modelBuilder.Entity<CIR_Blob>(entity =>
        {
            entity.Property(e => e.CIR_BlobID).ValueGeneratedNever();
            entity.Property(e => e.AddedOn).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.HQ_DatabaseName).HasDefaultValue("");
            entity.Property(e => e.HQ_Local_Folder).HasDefaultValue("");
            entity.Property(e => e.HQ_Storage_Option).HasDefaultValue("");
            entity.Property(e => e.VSL_DatabaseName).HasDefaultValue("");
            entity.Property(e => e.VSL_Local_Folder).HasDefaultValue("");
            entity.Property(e => e.VSL_Storage_Option).HasDefaultValue("");
            entity.Property(e => e.Cloud_ContainerName).HasDefaultValue("");
            entity.Property(e => e.Cloud_Directory).HasDefaultValue("");
        });

        modelBuilder.Entity<CIR_Log>(entity =>
        {
            entity.HasKey(e => e.CIR_LogID).HasName("PK_CIR_Log");

            entity.Property(e => e.CIR_LogID).ValueGeneratedNever();
            entity.Property(e => e.Activity).HasDefaultValue("");
            entity.Property(e => e.LogDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Remarks).HasDefaultValue("");
            entity.Property(e => e.UserName).HasDefaultValue("");
            entity.Property(e => e.UserRank).HasDefaultValue("");
        });


        modelBuilder.Entity<CIR_UserAck>(entity =>
        {
            entity.HasKey(e => e.CIR_UserAckID).HasName("PK_CIR_UserAck");

            entity.Property(e => e.CIR_UserAckID).ValueGeneratedNever();
            entity.Property(e => e.Comments).HasDefaultValue("");
            entity.Property(e => e.DateRead).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(0);
        });

        modelBuilder.Entity<CIR_UserFavourite>(entity =>
        {
            entity.Property(e => e.CIR_UserFavouriteID).ValueGeneratedNever();
        });

        modelBuilder.Entity<CIR_Notification>(entity =>
        {
            entity.HasKey(e => e.CIR_NotificationID).HasName("PK_CIR_Notification");

            entity.Property(e => e.CIR_NotificationID).ValueGeneratedNever();
            entity.Property(e => e.Comments).HasDefaultValue("");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsRead).HasDefaultValue(0);
        });

        modelBuilder.Entity<CIR_Settings>(entity =>
        {
            entity.Property(e => e.CIR_SettingsID).ValueGeneratedNever();
        });


        modelBuilder.Entity<VwCIRMasterBlob>(entity =>
        {
            entity.ToView("Vw_CIR_MasterBlob");
        });

        modelBuilder.Entity<VwCIRNotification>(entity =>
        {
            entity.ToView("Vw_CIRNotification");
        });

        modelBuilder.Entity<VwCIRVesselAck>(entity =>
        {
            entity.ToView("Vw_CIR_VesselAck");
        });

        modelBuilder.Entity<CircularsChart>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null); // prevent EF expecting a DB view/table
        });

        modelBuilder.Entity<Circulars_TreeviewDto>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null); // prevents EF from expecting a database view/table
        });

        modelBuilder.Entity<CIR_TreeviewEntity>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null);
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
