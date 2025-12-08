 using DMS.Data.EF.DTO;
using DMS.Data.EF.Models;
using DMS.Data.EF.Models.Circulars;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Context;

public partial class CircularsContext : DbContext
{
    private DBConnectionString _dbActions;

    public CircularsContext(DbContextOptions<CircularsContext> options, DBConnectionString dbActions) : base(options)
    {
        _dbActions = dbActions;
    }

    public virtual DbSet<ManualCount_Dashboard> DashboardManualCount { get; set; } = null!;

    public virtual DbSet<Manual_TreeviewEntity> DmManual_Treeview { get; set; } = null!;

    public virtual DbSet<CIR_Blob> CIR_Blob { get; set; } = null!;
    
    public virtual DbSet<CIR_Category> CIR_Category { get; set; } = null!;

    public virtual DbSet<CIR_CategoryExt> CIR_CategoryExt { get; set; } = null!;

    public virtual DbSet<CIR_Log> CIR_Log { get; set; } = null!;

    public virtual DbSet<CIR_Master> CIR_Master { get; set; } = null!;

    public virtual DbSet<CIR_UserAck> CIR_UserAck { get; set; } = null!;

    public virtual DbSet<CIR_UserFavourite> CIR_UserFavourite { get; set; }


    public virtual DbSet<CIR_Settings> CIR_Settings { get; set; }

    public virtual DbSet<VwCIRMasterBlob> VwCIRMasterBlob { get; set; }

    public virtual DbSet<VwCIRNotification> VwCIRNotification { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(_dbActions.DefaultConnection);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<CIR_Category>(entity =>
        {
            entity.HasKey(e => e.DM_CategoryID).HasName("PK_DM_ManualCategory");

            entity.Property(e => e.DM_CategoryID).ValueGeneratedNever();
            //entity.Property(e => e.CategoryCode).HasDefaultValue("");
            //entity.Property(e => e.IsDisabled).HasDefaultValue(0);
        });

        modelBuilder.Entity<CIR_Blob>(entity =>
        {
            entity.Property(e => e.DM_ManualID).ValueGeneratedNever();
            entity.Property(e => e.CanExport).HasDefaultValue(0);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsApprovalRequired).HasDefaultValue(0);
            entity.Property(e => e.IsDeleted).HasDefaultValue(0);
            entity.Property(e => e.ManualCode).HasDefaultValue("");
            entity.Property(e => e.SortOrder).HasDefaultValueSql("((0))");
        });

        modelBuilder.Entity<CIR_Log>(entity =>
        {
            //entity.HasKey(e => e.).HasName("PK_DM_ManualLog");

            //entity.Property(e => e.DmManualLogId).ValueGeneratedNever();
            //entity.Property(e => e.Activity).HasDefaultValue("");
            //entity.Property(e => e.Logdate).HasDefaultValueSql("(getdate())");
            //entity.Property(e => e.Remarks).HasDefaultValue("");
            //entity.Property(e => e.UserName).HasDefaultValue("");
            //entity.Property(e => e.UserRank).HasDefaultValue("");
        });


        //modelBuilder.Entity<CIR_UserAck>(entity =>
        //{
        //    entity.HasKey(e => e.CIR_UserAck).HasName("PK_DM_Manual_VslAck");

        //    entity.Property(e => e.DmManualUserAckId).ValueGeneratedNever();
        //    entity.Property(e => e.Comments).HasDefaultValue("");
        //    entity.Property(e => e.DateRead).HasDefaultValueSql("(getdate())");
        //    entity.Property(e => e.IsActive).HasDefaultValue(0);
        //});

        modelBuilder.Entity<CIR_UserFavourite>(entity =>
        {
            entity.Property(e => e.CIR_UserFavouriteID).ValueGeneratedNever();
        });

        modelBuilder.Entity<DM_ManualNotification>(entity =>
        {
            entity.HasKey(e => e.DM_ManualNotificationId).HasName("PK_DM_ManualNotification");

            entity.Property(e => e.DM_ManualNotificationId).ValueGeneratedNever();
            entity.Property(e => e.Comments).HasDefaultValue("");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsRead).HasDefaultValue(0);
        });

        modelBuilder.Entity<CIR_Settings>(entity =>
        {
            entity.Property(e => e.DmSettingsId).ValueGeneratedNever();
        });

        modelBuilder.Entity<VwCIRMasterBlob>(entity =>
        {
            entity.ToView("Vw_CIR_MasterBlob");
        });

        modelBuilder.Entity<VwCIRNotification>(entity =>
        {
            entity.ToView("Vw_CIRNotification");
        });


        modelBuilder.Entity<Manual_TreeviewDto>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null); // prevents EF from expecting a database view/table
        });

        modelBuilder.Entity<Manual_TreeviewEntity>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null);
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
