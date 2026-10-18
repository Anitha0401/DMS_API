/* =====================================================================================================
   009_Proc_NextID.sql  -  one "next ID" procedure for every table, in the office (HQ) and ship (VSL) databases.

   Replaces the per-table procedures such as Proc_NextID_App_Images. Same rules:
     - The new ID is MAX(primary key) + 1 for this origin, but never below the last number handed out
       (TableID_Setup.LastNumber), so a number is not reused after its row is deleted.
     - The first time a table is used, its numbering starts from the vessel's TableIDstart (V_Vessels).
     - @Incrementor reserves a block: IDs @NewTableID .. @NewTableID + @Incrementor - 1 are all yours.

   HQ vs VSL:
     - Origin 'HQ'  : no vessel check. MAX is taken over Origin = 'HQ' rows only, whatever their VesselID,
                      and the counter is kept in TableID_Setup under VesselID = 0 (starts from 0).
     - Origin 'VSL' : MAX is taken over Origin = 'VSL' rows of @VesselID only, and the counter is kept
                      under that VesselID, starting from that vessel's TableIDstart.
     A table with no Origin or no VesselID column is simply not filtered on it.

   The primary key column is read from the table itself (single-column primary key), falling back to
   <TableName>ID. The table name is checked against the database, so it cannot be used to inject SQL.

   Usage:
     DECLARE @id bigint;
     EXEC dbo.Proc_NextID @TableName = 'OtherDocumentMaster', @Origin = 'VSL', @VesselID = 101,
                          @Incrementor = 1, @NewTableID = @id OUTPUT;
     SELECT @id;

   Run on the office database and on each ship database. Needs TableID_Setup, and V_Vessels (or V_Vessel)
   with a TableIDstart column for the VSL start numbers (without it, numbering starts from 0).
   ===================================================================================================== */

CREATE OR ALTER PROCEDURE dbo.Proc_NextID
(
    @TableName   varchar(50),
    @Origin      varchar(3),
    @VesselID    int,
    @Incrementor int,
    @NewTableID  bigint OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MaxNumFromTable bigint, @LastNumFromTableID bigint, @TableID_SetupID bigint,
            @KeyColumn sysname, @Sql nvarchar(max), @Where nvarchar(400) = N' WHERE 1 = 1',
            @SetupVesselID int, @IsHQ bit, @ObjectId int;

    SET @TableName = LTRIM(RTRIM(@TableName));
    SET @Origin = UPPER(LTRIM(RTRIM(ISNULL(@Origin, ''))));
    SET @IsHQ = CASE WHEN @Origin = 'HQ' THEN 1 ELSE 0 END;
    IF @Incrementor IS NULL OR @Incrementor <= 0 SET @Incrementor = 1;

    -- ---- The table, and its primary key column ------------------------------------------------------
    SET @ObjectId = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U');
    IF @ObjectId IS NULL
    BEGIN
        RAISERROR('Proc_NextID: table dbo.%s does not exist.', 16, 1, @TableName);
        RETURN;
    END;

    SELECT @KeyColumn = c.name
      FROM sys.indexes i
      JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
      JOIN sys.columns c        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
     WHERE i.object_id = @ObjectId AND i.is_primary_key = 1
       AND (SELECT COUNT(*) FROM sys.index_columns x WHERE x.object_id = i.object_id AND x.index_id = i.index_id) = 1;

    IF @KeyColumn IS NULL AND COL_LENGTH(N'dbo.' + QUOTENAME(@TableName), @TableName + 'ID') IS NOT NULL
        SET @KeyColumn = @TableName + 'ID';

    IF @KeyColumn IS NULL
    BEGIN
        RAISERROR('Proc_NextID: cannot find a single-column primary key (or %sID) on dbo.%s.', 16, 1, @TableName, @TableName);
        RETURN;
    END;

    -- ---- Which rows count: HQ -> Origin only; VSL -> Origin and this vessel ---------------------------
    IF COL_LENGTH(N'dbo.' + QUOTENAME(@TableName), 'Origin') IS NOT NULL
        SET @Where += N' AND Origin = @Origin';
    IF @IsHQ = 0 AND COL_LENGTH(N'dbo.' + QUOTENAME(@TableName), 'VesselID') IS NOT NULL
        SET @Where += N' AND VesselID = @VesselID';

    SET @SetupVesselID = CASE WHEN @IsHQ = 1 THEN 0 ELSE @VesselID END;

    BEGIN TRANSACTION;

    -- One caller per table at a time, so two users saving together never get the same number.
    DECLARE @LockName nvarchar(255) = N'Proc_NextID:' + @TableName + N':' + CAST(@SetupVesselID AS nvarchar(20));
    DECLARE @Lock int;
    EXEC @Lock = sp_getapplock @Resource = @LockName, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
    IF @Lock < 0
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('Proc_NextID: timed out waiting for the next number of dbo.%s.', 16, 1, @TableName);
        RETURN;
    END;

    ---- Max primary key in that table (for this origin / vessel)
    SET @Sql = N'SELECT @Max = ISNULL(MAX(' + QUOTENAME(@KeyColumn) + N'), 0) FROM dbo.' + QUOTENAME(@TableName) + @Where + N';';
    EXEC sp_executesql @Sql,
         N'@Origin varchar(3), @VesselID int, @Max bigint OUTPUT',
         @Origin = @Origin, @VesselID = @VesselID, @Max = @MaxNumFromTable OUTPUT;

    ---- Last number handed out for that table (from TableID_Setup)
    SET @LastNumFromTableID = ISNULL((SELECT LastNumber FROM dbo.TableID_Setup WITH (UPDLOCK, HOLDLOCK)
                                       WHERE TableName = @TableName AND VesselID = @SetupVesselID), 0);

    -------------------------------------------------------------------------------------------------------
    IF @LastNumFromTableID = 0
    BEGIN
        -- First use: start from the vessel's TableIDstart (HQ starts from 0).
        IF @IsHQ = 0
        BEGIN
            IF COL_LENGTH('dbo.V_Vessels', 'TableIDstart') IS NOT NULL
                EXEC sp_executesql N'SELECT @Start = ISNULL(TableIDstart, 0) FROM dbo.V_Vessels WHERE VesselID = @VesselID;',
                     N'@VesselID int, @Start bigint OUTPUT', @VesselID = @VesselID, @Start = @LastNumFromTableID OUTPUT;
            ELSE IF COL_LENGTH('dbo.V_Vessel', 'TableIDstart') IS NOT NULL
                EXEC sp_executesql N'SELECT @Start = ISNULL(TableIDstart, 0) FROM dbo.V_Vessel WHERE VesselID = @VesselID;',
                     N'@VesselID int, @Start bigint OUTPUT', @VesselID = @VesselID, @Start = @LastNumFromTableID OUTPUT;
        END;
        SET @LastNumFromTableID = ISNULL(@LastNumFromTableID, 0);

        SET @TableID_SetupID = (SELECT ISNULL(MAX(TableID_SetupID), 0) FROM dbo.TableID_Setup WITH (UPDLOCK, HOLDLOCK)) + 1;
        DELETE FROM dbo.TableID_Setup WHERE TableName = @TableName AND VesselID = @SetupVesselID;
        IF COL_LENGTH('dbo.TableID_Setup', 'IsForVsl') IS NOT NULL
            -- Same IsForVsl as the table's other rows (0 when it has none).
            EXEC sp_executesql N'INSERT INTO dbo.TableID_Setup (TableID_SetupID, TableName, VesselID, LastNumber, ToTrf, IsForVsl)
                SELECT @Id, @Table, @Vessel, @Last, 1,
                       ISNULL((SELECT TOP (1) IsForVsl FROM dbo.TableID_Setup WHERE TableName = @Table AND IsForVsl IS NOT NULL), 0);',
                 N'@Id bigint, @Table varchar(50), @Vessel int, @Last bigint',
                 @Id = @TableID_SetupID, @Table = @TableName, @Vessel = @SetupVesselID, @Last = @LastNumFromTableID;
        ELSE
            INSERT INTO dbo.TableID_Setup (TableID_SetupID, TableName, VesselID, LastNumber, ToTrf)
            VALUES (@TableID_SetupID, @TableName, @SetupVesselID, @LastNumFromTableID, 1);
    END;
    -------------------------------------------------------------------------------------------------------

    ---- Next number
    SET @NewTableID = @MaxNumFromTable + 1;

    ---- Never hand out a number another row already has (another origin or vessel, or a TableIDstart that is
    ---- missing or overlaps): carry on after the highest ID in the whole table instead.
    DECLARE @Clash int, @AllMax bigint, @To bigint;
    SET @To = @NewTableID + @Incrementor - 1;
    SET @Sql = N'SELECT @Clash = COUNT(*) FROM dbo.' + QUOTENAME(@TableName)
             + N' WHERE ' + QUOTENAME(@KeyColumn) + N' BETWEEN @From AND @To;'
             + N' SELECT @AllMax = ISNULL(MAX(' + QUOTENAME(@KeyColumn) + N'), 0) FROM dbo.' + QUOTENAME(@TableName) + N';';
    EXEC sp_executesql @Sql,
         N'@From bigint, @To bigint, @Clash int OUTPUT, @AllMax bigint OUTPUT',
         @From = @NewTableID, @To = @To, @Clash = @Clash OUTPUT, @AllMax = @AllMax OUTPUT;
    IF @Clash > 0 SET @NewTableID = @AllMax + 1;

    ---- Remember it in TableID_Setup (the whole block when @Incrementor > 1)
    UPDATE dbo.TableID_Setup
       SET LastNumber = @NewTableID + @Incrementor - 1, ToTrf = 1
     WHERE TableName = @TableName AND VesselID = @SetupVesselID;

    COMMIT TRANSACTION;
END;
GO
