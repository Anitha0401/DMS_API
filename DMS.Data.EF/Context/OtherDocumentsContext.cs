using DMS.Data.EF.DTO.OtherDocument;
using DMS.Data.EF.Models.OtherDocument;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Context;

public partial class OtherDocumentsContext : DbContext
{
    private DBConnectionString _dbActions;

    public OtherDocumentsContext(DbContextOptions<OtherDocumentsContext> options, DBConnectionString dbActions) : base(options)
    {
        _dbActions = dbActions;
    }

    public virtual DbSet<CIR_OtherDocument> CIR_OtherDocument { get; set; } = null!;

    public virtual DbSet<CIR_OtherDocument_Blob> CIR_OtherDocument_Blob { get; set; } = null!;

    public virtual DbSet<CIR_OtherDocument_Type> CIR_OtherDocument_Type { get; set; } = null!;

    public virtual DbSet<CIR_OtherDocument_Log> CIR_OtherDocument_Log { get; set; } = null!;

    public virtual DbSet<Vw_CIR_OtherDocument> Vw_CIR_OtherDocument { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlServer(_dbActions.DefaultConnection);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CIR_OtherDocument>(entity =>
        {
            entity.HasKey(e => e.CIR_OtherDocumentID).HasName("PK_CIR_OtherDocument");
            entity.Property(e => e.CIR_OtherDocumentID).ValueGeneratedNever();
            entity.Property(e => e.DocumentTitle).HasMaxLength(250).HasDefaultValue("");
            entity.Property(e => e.CIR_OtherDocument_TypeID);
            entity.Property(e => e.UploadedOn).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UploadedBy).HasMaxLength(150).HasDefaultValue("");
        });

        modelBuilder.Entity<CIR_OtherDocument_Blob>(entity =>
        {
            entity.HasKey(e => e.CIR_OtherDocument_BlobID).HasName("PK_CIR_OtherDocument_Blob");
            entity.Property(e => e.CIR_OtherDocument_BlobID).ValueGeneratedNever();
            entity.Property(e => e.CIR_OtherDocumentID).IsRequired();
            entity.Property(e => e.AddedOn).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FileName).HasMaxLength(150);
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.OriginalFileType).HasMaxLength(50);
            entity.Property(e => e.HQ_DatabaseName).HasDefaultValue("");
            entity.Property(e => e.HQ_Local_Folder).HasDefaultValue("");
            entity.Property(e => e.HQ_Storage_Option).HasDefaultValue("");
            entity.Property(e => e.VSL_DatabaseName).HasDefaultValue("");
            entity.Property(e => e.VSL_Local_Folder).HasDefaultValue("");
            entity.Property(e => e.VSL_Storage_Option).HasDefaultValue("");
            entity.Property(e => e.Cloud_ContainerName).HasDefaultValue("");
            entity.Property(e => e.Cloud_Directory).HasDefaultValue("");
        });

        modelBuilder.Entity<CIR_OtherDocument_Type>(entity =>
        {
            entity.HasKey(e => e.CIR_OtherDocument_TypeID).HasName("PK_CIR_OtherDocument_Type");
            entity.Property(e => e.CIR_OtherDocument_TypeID).ValueGeneratedNever();
            entity.Property(e => e.DocumentType).HasMaxLength(100).HasDefaultValue("");
            entity.Property(e => e.DocumentCode).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasDefaultValue(1);
        });

        modelBuilder.Entity<CIR_OtherDocument_Log>(entity =>
        {
            entity.HasKey(e => e.CIR_OtherDocument_LogID).HasName("PK_CIR_OtherDocument_Log");
            entity.Property(e => e.CIR_OtherDocument_LogID).ValueGeneratedNever();
            entity.Property(e => e.Activity).HasDefaultValue("");
            entity.Property(e => e.LogDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Remarks).HasDefaultValue("");
            entity.Property(e => e.UserName).HasDefaultValue("");
        });

        modelBuilder.Entity<Vw_CIR_OtherDocument>(eb =>
        {
            eb.HasNoKey();
            eb.ToView("Vw_CIR_OtherDocument");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}