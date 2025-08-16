using Microsoft.EntityFrameworkCore;
using DMS.Data.EF.Models;
using Microsoft.Extensions.Options;

namespace DMS.Data.EF.Context;

public partial class DmsContext : DbContext
{
    private DBConnectionString _dbActions;

    public DmsContext(DbContextOptions<DmsContext> options, DBConnectionString dbActions) : base(options)
    {
        _dbActions = dbActions;
    }

    public virtual DbSet<Manual_TreeviewEntity> DmManual_Treeview { get; set; } = null!;

    public virtual DbSet<DM_Manual_Content> DmManualContent { get; set; } = null!;

    public virtual DbSet<DM_Manual> DmManuals { get; set; }

    public virtual DbSet<DM_ManualVersion> DmManualVersion { get; set; }

    public virtual DbSet<DM_Manual_ApprovalSettings> DmManualApprovalSettings { get; set; }

    public virtual DbSet<DM_Manual_Attachments> DmManualAttachments { get; set; }

    public virtual DbSet<DM_Category> Dm_Category { get; set; }

    public virtual DbSet<DM_Manual_Log> DmManualLogs { get; set; }

    public virtual DbSet<DM_Status> Dm_Status { get; set; }

    public virtual DbSet<DM_Manual_RolesToAck> DmManualRolesToAck { get; set; }

    public virtual DbSet<DM_Manual_UserAck> DmManualVslAcks { get; set; }

    public virtual DbSet<Gen_Error_HQ_Log> GenErrorHqLogs { get; set; }

    public virtual DbSet<Gen_Error_Vsl_Log> GenErrorVslLogs { get; set; }

    public virtual DbSet<HQ_UserRoles> HqUserRoles { get; set; }

    public virtual DbSet<HQ_Users> HqUsers { get; set; }

    public virtual DbSet<V_Vessel> VVessels { get; set; }

    public virtual DbSet<Vsl_Staff> VslStaffs { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(_dbActions.DefaultConnection);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DM_Manual>(entity =>
        {
            entity.ToTable("DM_Manual");

            entity.Property(e => e.DM_ManualID)
                .ValueGeneratedNever()
                .HasColumnName("DM_ManualID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DM_CategoryID).HasColumnName("DM_CategoryID");
            entity.Property(e => e.DM_StatusID).HasColumnName("DM_StatusID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(0);
            entity.Property(e => e.ManualCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ManualName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.ParentID).HasColumnName("ParentID");
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<DM_ManualVersion>(entity =>
        {
            entity.ToTable("DM_ManualVersion");

            entity.Property(e => e.DM_ManualVersionID)
                .ValueGeneratedNever()
                .HasColumnName("DM_ManualVersionID");
            entity.Property(e => e.DM_ManualID).HasColumnName("DM_ManualID");
            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ApprovedDate).HasColumnType("datetime");
            entity.Property(e => e.Comments)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.ReleasedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReleasedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FinalisedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FinalisedDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(0);
            entity.Property(e => e.IsCurrent).HasDefaultValue(0);
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<DM_Manual_ApprovalSettings>(entity =>
        {
            entity.HasKey(e => e.DM_Manual_ApprovalSettingsID).HasName("PK_DM_ApprovalSettings");
            
            entity.ToTable("DM_Manual_ApprovalSettings");

            entity.Property(e => e.DM_Manual_ApprovalSettingsID)
                .ValueGeneratedNever()
                .HasColumnName("DM_Manual_ApprovalSettingsID");
            entity.Property(e => e.CanAddEmailApprovalLink).HasDefaultValue(0);
            entity.Property(e => e.DM_StatusID).HasColumnName("DM_StatusID");
            entity.Property(e => e.Next_DM_StatusID).HasColumnName("Next_DM_StatusID");
            entity.Property(e => e.Hqdept)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("HQDept");
            entity.Property(e => e.IsDisabled).HasDefaultValue(0);
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.SendBack_DM_StatusID)
                .HasDefaultValue(-1L)
                .HasColumnName("SendBack_DM_StatusID");
            entity.Property(e => e.UserID)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");
            entity.Property(e => e.UserRole)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<DM_Manual_Attachments>(entity =>
        {
            entity.HasKey(e => e.DM_Manual_AttachmentsID).HasName("PK_DM_ManualAttachments");

            entity.ToTable("DM_Manual_Attachments");

            entity.Property(e => e.DM_Manual_AttachmentsID)
                .ValueGeneratedNever()
                .HasColumnName("DM_Manual_AttachmentsID");
            entity.Property(e => e.AddedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.BlobContents).HasColumnType("image");
            entity.Property(e => e.DM_ManualID).HasColumnName("DM_ManualID");
            entity.Property(e => e.FileType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<DM_Category>(entity =>
        {
            entity.HasKey(e => e.DM_CategoryID).HasName("PK_DM_Category");

            entity.ToTable("DM_Category");

            entity.Property(e => e.DM_CategoryID)
                .ValueGeneratedNever()
                .HasColumnName("DM_CategoryID");
            entity.Property(e => e.CategoryCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.IsDisabled).HasDefaultValue(0);
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<DM_Manual_Log>(entity =>
        {
            entity.HasKey(e => e.DM_Manual_LogID).HasName("PK_DM_ManualLog");

            entity.ToTable("DM_Manual_Log");

            entity.Property(e => e.DM_Manual_LogID)
                .ValueGeneratedNever()
                .HasColumnName("DM_Manual_LogID");
            entity.Property(e => e.Activity)
                .HasMaxLength(120)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.DM_ManualID).HasColumnName("DM_ManualID");
            entity.Property(e => e.Logdate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.Remarks)
                .HasMaxLength(3000)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.UserName)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.UserRank)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<DM_Status>(entity =>
        {
            entity.HasKey(e => e.DM_StatusID).HasName("PK_DM_Status");

            entity.ToTable("DM_Status");

            entity.Property(e => e.DM_StatusID)
                .ValueGeneratedNever()
                .HasColumnName("DM_StatusID");
            entity.Property(e => e.IsDisabled).HasDefaultValue(0);
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.SortOrder).HasDefaultValue(-1);
            entity.Property(e => e.StatusCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.StatusName)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<DM_Manual_RolesToAck>(entity =>
        {
            entity.ToTable("DM_Manual_RolesToAck");

            entity.Property(e => e.DM_Manual_RolesToAckID)
                .ValueGeneratedNever()
                .HasColumnName("DM_Manual_RolesToAckID");
            entity.Property(e => e.DM_ManualID).HasColumnName("DM_ManualID");
            entity.Property(e => e.IsActive).HasDefaultValue(0);
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.UserRank)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<DM_Manual_UserAck>(entity =>
        {
            entity.ToTable("DM_Manual_UserAck");

            entity.Property(e => e.DM_Manual_UserAckID)
                .ValueGeneratedNever()
                .HasColumnName("DM_Manual_UserAckID");
            entity.Property(e => e.DateRead).HasColumnType("datetime");
            entity.Property(e => e.Comments)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DM_ManualID).HasColumnName("DM_ManualID");
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.UserID)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("UserID");
            entity.Property(e => e.UserName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.UserRank)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<Gen_Error_HQ_Log>(entity =>
        {
            entity.ToTable("Gen_Error_HQ_Log");

            entity.Property(e => e.Gen_Error_HQ_LogID)
                .ValueGeneratedNever()
                .HasColumnName("Gen_Error_HQ_LogID");
            entity.Property(e => e.AddedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ApplicationName)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.FunctionName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.LoggedInUserID)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI")
                .HasColumnName("LoggedInUserID");
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.PageName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.TextContents)
                .UseCollation("Latin1_General_CI_AI")
                .HasColumnType("text");
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<Gen_Error_Vsl_Log>(entity =>
        {
            entity.HasKey(e => new { e.Gen_Error_Vsl_LogID, e.VesselID });

            entity.ToTable("Gen_Error_VSL_Log");

            entity.Property(e => e.Gen_Error_Vsl_LogID).HasColumnName("Gen_Error_VSL_LogID");
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
            entity.Property(e => e.AddedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ApplicationName)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.FunctionName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.LoggedInUserID)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI")
                .HasColumnName("LoggedInUserID");
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.PageName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasDefaultValue("")
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.TextContents)
                .UseCollation("Latin1_General_CI_AI")
                .HasColumnType("text");
        });

        modelBuilder.Entity<HQ_Users>(entity =>
        {
            entity.HasKey(e => e.HQ_UsersID);

            entity.ToTable("HQ_Users");

            entity.Property(e => e.HQ_UsersID)
                .ValueGeneratedNever()
                .HasColumnName("HQ_UsersID");
            entity.Property(e => e.DateAdded).HasColumnType("datetime");
            entity.Property(e => e.DateDisabled).HasColumnType("datetime");
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.Email)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("EMail");
            entity.Property(e => e.EmpID)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("EmpID");
            entity.Property(e => e.HqDept)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("HQDept");
            entity.Property(e => e.IsDisabled).HasDefaultValue(0);
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.PhoneNo)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserID)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");
            entity.Property(e => e.UserName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<HQ_UserRoles>(entity =>
        {
            entity.HasKey(e => e.HQ_UserRolesID);

            entity.ToTable("HQ_UserRoles");

            entity.Property(e => e.HQ_UserRolesID)
                .ValueGeneratedNever()
                .HasColumnName("HQ_UserRolesID");
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.HQDept)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("HQDept");
            entity.Property(e => e.IsDisabled).HasDefaultValue(0);
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.UserRole)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ApprovalOrder).HasColumnName("ApprovalOrder");
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
        });

        modelBuilder.Entity<V_Vessel>(entity =>
        {
            entity.ToTable("V_Vessel");
            
            entity.Property(e => e.V_VesselID)
                .ValueGeneratedNever()
                .HasColumnName("V_VesselID");
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
            entity.Property(e => e.VslCode)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.VslName)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Vsl_Staff>(entity =>
        {
            entity.HasKey(e => new { e.Vsl_StaffID, e.VesselID });

            entity.ToTable("Vsl_Staff");

            entity.Property(e => e.Vsl_StaffID).HasColumnName("Vsl_StaffID");
            entity.Property(e => e.VesselID).HasColumnName("VesselID");
            entity.Property(e => e.Origin)
                .HasMaxLength(3)
                .IsUnicode(false)
                .UseCollation("Latin1_General_CI_AI");
            entity.Property(e => e.UserDept)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.UserID)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");
            entity.Property(e => e.UserName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.UserRank).HasMaxLength(50);
        });

        modelBuilder.Entity<Manual_TreeviewDto>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null); // prevents EF from expecting a database view/table
        });

        modelBuilder.Entity<Manual_TreeviewEntity>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null); // prevents EF from expecting a database view/table
        });

        modelBuilder.Entity<TreeviewDataDto>(eb =>
        {
            eb.HasNoKey();
            eb.ToView(null); // prevents EF from expecting a database view/table
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
