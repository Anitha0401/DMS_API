/*
    014_RemoveFolderFiles.sql  -  folders hold Other Documents only (run after 013_DocumentFolders.sql; safe to run again)

    Files were once uploaded straight into a folder (FLD_File, with the contents in FLD_Blob). A folder now holds
    Other Documents only: OtherDocumentMaster rows with FLD_FolderID, their files in OtherDocument_Blob.
    This script removes FLD_File and FLD_Blob from the sync and drops both tables.

    If live files are still in FLD_File the script stops, so nothing is lost by mistake. Re-run it with
    @DropFolderFiles = 1 to drop them anyway.
*/
SET NOCOUNT ON;

DECLARE @DropFolderFiles bit = 0;   -- 1 = drop the tables even when files are still in them

IF OBJECT_ID('dbo.FLD_File', 'U') IS NOT NULL AND @DropFolderFiles = 0
BEGIN
    DECLARE @live int;
    EXEC sp_executesql N'SELECT @n = COUNT(*) FROM dbo.FLD_File WHERE IsDeleted = 0', N'@n int OUTPUT', @n = @live OUTPUT;
    IF @live > 0
    BEGIN
        DECLARE @msg nvarchar(400) = CONCAT(@live, N' file(s) are still in FLD_File. Add them as Other Documents, or set @DropFolderFiles = 1 to drop them.');
        THROW 50000, @msg, 1;
    END
END
GO

-- No longer synced to or from the vessels.
IF OBJECT_ID('dbo.Sync_ChangeLog', 'U') IS NOT NULL
    DELETE FROM dbo.Sync_ChangeLog WHERE TableName IN ('FLD_File', 'FLD_Blob');
IF OBJECT_ID('dbo.Sync_Table', 'U') IS NOT NULL
    DELETE FROM dbo.Sync_Table WHERE TableName IN ('FLD_File', 'FLD_Blob');
IF OBJECT_ID('dbo.TableID_Setup', 'U') IS NOT NULL
    DELETE FROM dbo.TableID_Setup WHERE TableName IN ('FLD_File', 'FLD_Blob');
GO

-- Dropping the tables drops their sync triggers too.
IF OBJECT_ID('dbo.FLD_Blob', 'U') IS NOT NULL DROP TABLE dbo.FLD_Blob;
IF OBJECT_ID('dbo.FLD_File', 'U') IS NOT NULL DROP TABLE dbo.FLD_File;
GO

PRINT '014_RemoveFolderFiles: FLD_File and FLD_Blob are gone; folders hold Other Documents only.';
GO
