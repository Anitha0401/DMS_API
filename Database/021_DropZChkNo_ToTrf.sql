/* =====================================================================================================
   021_DropZChkNo_ToTrf.sql  -  drops the columns zChkNo and ToTrf from every table in the database.
   Run on the OFFICE (HQ) database, and on each vessel database if you want them gone there too.
   Safe to run more than once.

   BEFORE RUNNING
     1. Take a backup. Dropping a column cannot be undone.
     2. Run the updated 009_Proc_NextID.sql first: Proc_NextID used to write TableID_Setup.ToTrf, and every new
        record (all inserts through the API) gets its ID from it. This script refuses to drop anything while a
        procedure, trigger, view or function still mentions zChkNo or ToTrf.

   HOW TO USE
     @Apply = 0  (default)  preview: lists every column that would be dropped, what depends on it, and any code
                            that still mentions it. Nothing is changed.
     @Apply = 1             drops them, in one transaction: first the default and check constraints, statistics
                            and indexes on each column, then the column.

   An index that uses one of these columns (as a key or included column) is dropped with it. Indexes are listed in
   the preview; re-create any you still need without the column afterwards.
   ===================================================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;          -- set to 1 to drop

/* ---------------------------------------------------------------------------------- What is there ---- */
DECLARE @cols TABLE (ObjectId int, SchemaName sysname, TableName sysname, ColumnName sysname, ColumnId int);
INSERT @cols (ObjectId, SchemaName, TableName, ColumnName, ColumnId)
SELECT t.object_id, s.name, t.name, c.name, c.column_id
  FROM sys.tables t
  JOIN sys.schemas s ON s.schema_id = t.schema_id
  JOIN sys.columns c ON c.object_id = t.object_id
 WHERE c.name IN (N'zChkNo', N'ToTrf')
   AND t.is_ms_shipped = 0;

IF NOT EXISTS (SELECT 1 FROM @cols)
BEGIN
    PRINT 'No table has a zChkNo or ToTrf column. Nothing to do.';
    RETURN;
END;

/* Columns found */
SELECT SchemaName + '.' + TableName AS [Table], ColumnName AS [Column]
  FROM @cols
 ORDER BY TableName, ColumnName;

/* Code that still mentions the columns (procedures, triggers, views, functions) */
DECLARE @code TABLE (ObjectName nvarchar(300), ObjectType nvarchar(60));
INSERT @code
SELECT OBJECT_SCHEMA_NAME(m.object_id) + '.' + OBJECT_NAME(m.object_id), o.type_desc
  FROM sys.sql_modules m
  JOIN sys.objects o ON o.object_id = m.object_id
 WHERE (m.definition LIKE N'%zChkNo%' OR m.definition LIKE N'%ToTrf%')
   AND o.is_ms_shipped = 0;

IF EXISTS (SELECT 1 FROM @code)
    SELECT ObjectName AS [Code that still uses zChkNo / ToTrf], ObjectType AS [Type] FROM @code ORDER BY ObjectName;

/* Indexes that will be dropped with the columns */
SELECT DISTINCT c.SchemaName + '.' + c.TableName AS [Table], i.name AS [Index dropped with the column],
       CASE WHEN i.is_primary_key = 1 THEN 'PRIMARY KEY' WHEN i.is_unique_constraint = 1 THEN 'UNIQUE constraint'
            WHEN i.is_unique = 1 THEN 'unique index' ELSE 'index' END AS [Kind]
  FROM @cols c
  JOIN sys.index_columns ic ON ic.object_id = c.ObjectId AND ic.column_id = c.ColumnId
  JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
 WHERE i.index_id > 0;

/* Foreign keys or computed columns that use them (these stop the script) */
DECLARE @blockers TABLE (Reason nvarchar(400));
INSERT @blockers
SELECT 'Foreign key ' + fk.name + ' on ' + c.TableName + '.' + c.ColumnName
  FROM @cols c
  JOIN sys.foreign_key_columns fkc ON (fkc.parent_object_id = c.ObjectId AND fkc.parent_column_id = c.ColumnId)
                                   OR (fkc.referenced_object_id = c.ObjectId AND fkc.referenced_column_id = c.ColumnId)
  JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
UNION ALL
SELECT 'Computed column ' + cc.name + ' of ' + c.TableName + ' uses ' + c.ColumnName
  FROM @cols c
  JOIN sys.computed_columns cc ON cc.object_id = c.ObjectId
 WHERE cc.definition LIKE '%' + c.ColumnName + '%'
UNION ALL
SELECT 'Primary key ' + i.name + ' of ' + c.TableName + ' contains ' + c.ColumnName
  FROM @cols c
  JOIN sys.index_columns ic ON ic.object_id = c.ObjectId AND ic.column_id = c.ColumnId
  JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
 WHERE i.is_primary_key = 1;

IF EXISTS (SELECT 1 FROM @blockers)
    SELECT Reason AS [Needs attention before the columns can be dropped] FROM @blockers;

IF @Apply = 0
BEGIN
    PRINT 'Preview only. Check the lists above, then set @Apply = 1 and run again.';
    RETURN;
END;

IF EXISTS (SELECT 1 FROM @code)
BEGIN
    RAISERROR('Some procedures, triggers, views or functions still use zChkNo or ToTrf (listed above). Update them first (e.g. run the new 009_Proc_NextID.sql).', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM @blockers)
BEGIN
    RAISERROR('A foreign key, computed column or primary key uses one of the columns (listed above). Nothing was dropped.', 16, 1);
    RETURN;
END;

/* ------------------------------------------------------------------------------------------ Drop ---- */
DECLARE @sql nvarchar(max) = N'';

-- Default constraints
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(c.SchemaName) + N'.' + QUOTENAME(c.TableName)
             + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
  FROM @cols c
  JOIN sys.default_constraints dc ON dc.parent_object_id = c.ObjectId AND dc.parent_column_id = c.ColumnId;

-- Check constraints on the column
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(c.SchemaName) + N'.' + QUOTENAME(c.TableName)
             + N' DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';' + CHAR(10)
  FROM @cols c
  JOIN sys.check_constraints cc ON cc.parent_object_id = c.ObjectId AND cc.parent_column_id = c.ColumnId;

-- Indexes and unique constraints that use the column
SELECT @sql += CASE WHEN x.is_unique_constraint = 1
                    THEN N'ALTER TABLE ' + QUOTENAME(x.SchemaName) + N'.' + QUOTENAME(x.TableName) + N' DROP CONSTRAINT ' + QUOTENAME(x.IndexName) + N';'
                    ELSE N'DROP INDEX ' + QUOTENAME(x.IndexName) + N' ON ' + QUOTENAME(x.SchemaName) + N'.' + QUOTENAME(x.TableName) + N';' END + CHAR(10)
  FROM (SELECT DISTINCT c.SchemaName, c.TableName, i.name AS IndexName, i.is_unique_constraint
          FROM @cols c
          JOIN sys.index_columns ic ON ic.object_id = c.ObjectId AND ic.column_id = c.ColumnId
          JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
         WHERE i.index_id > 0 AND i.is_primary_key = 0) x;

-- Statistics created on the column (by hand or automatically)
SELECT @sql += N'DROP STATISTICS ' + QUOTENAME(x.SchemaName) + N'.' + QUOTENAME(x.TableName) + N'.' + QUOTENAME(x.StatName) + N';' + CHAR(10)
  FROM (SELECT DISTINCT c.SchemaName, c.TableName, st.name AS StatName
          FROM @cols c
          JOIN sys.stats_columns sc ON sc.object_id = c.ObjectId AND sc.column_id = c.ColumnId
          JOIN sys.stats st ON st.object_id = sc.object_id AND st.stats_id = sc.stats_id
         WHERE NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = st.object_id AND i.name = st.name)) x;

-- The columns
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(c.SchemaName) + N'.' + QUOTENAME(c.TableName)
             + N' DROP COLUMN ' + QUOTENAME(c.ColumnName) + N';' + CHAR(10)
  FROM @cols c
 ORDER BY c.TableName, c.ColumnName;

PRINT @sql;

BEGIN TRANSACTION;
    EXEC sys.sp_executesql @sql;
COMMIT TRANSACTION;

PRINT 'Dropped zChkNo / ToTrf from ' + CAST((SELECT COUNT(DISTINCT ObjectId) FROM @cols) AS varchar(10)) + ' table(s).';
GO
