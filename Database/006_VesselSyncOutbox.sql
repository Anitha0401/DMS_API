/* =====================================================================================
   DMS office database: change table for vessel sync (Sync_Outbox) and the triggers that fill it.
   Run after 005_VesselSync.sql. Safe to run again (objects are created or replaced).

   Every insert, edit or delete of a manual, circular/alert, their files, their vessel assignments,
   crew (Vsl_Staff) and categories writes one row per affected vessel into dbo.Sync_Outbox.
   VesselID -1 ("all vessels") is expanded to one row for every vessel in V_Vessel that is not retired.

   The vessel's next pull sends every record with a waiting row (or tells the vessel to remove it,
   when it is deleted or no longer assigned), together with the rows themselves (SyncRows). Once the
   vessel has applied the pull it stores the rows on board (SyncDownload) and calls api/Sync/Confirm
   with their SyncOutboxIDs, which sets DownloadedUtc. If that call is lost, the vessel's next pull
   sets it instead.

   Column  Action:  I = inserted, U = updated, D = deleted at HQ (for information; the pull always
                    sends the record's current state).
           SentUtc:        when the record went out in a pull response
           DownloadedUtc:  the marker - when the vessel confirmed it downloaded the row (api/Sync/Confirm)
   ===================================================================================== */

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_Outbox
    (
        SyncOutboxID   bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_Outbox PRIMARY KEY,
        VesselID       int            NOT NULL,
        ItemType       nvarchar(20)   NOT NULL,   -- Manual | Circular | Crew | Categories
        ItemID         bigint         NOT NULL,   -- DM_ManualID / CIR_MasterID; 0 for Crew and Categories
        Action         char(1)        NOT NULL,   -- I | U | D
        SourceTable    nvarchar(128)  NOT NULL,   -- table whose change wrote the row
        CreatedUtc     datetime2(3)   NOT NULL CONSTRAINT DF_Sync_Outbox_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        SentUtc        datetime2(3)   NULL,
        DownloadedUtc  datetime2(3)   NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sync_Outbox_Vessel' AND object_id = OBJECT_ID('dbo.Sync_Outbox'))
    CREATE INDEX IX_Sync_Outbox_Vessel ON dbo.Sync_Outbox (VesselID, DownloadedUtc) INCLUDE (ItemType, ItemID, SentUtc);
GO

IF TYPE_ID('dbo.Sync_ItemList') IS NULL
    CREATE TYPE dbo.Sync_ItemList AS TABLE
    (
        VesselID  int           NOT NULL,   -- -1 = all vessels
        ItemType  nvarchar(20)  NOT NULL,
        ItemID    bigint        NOT NULL,
        Action    char(1)       NOT NULL
    );
GO

/* Writes the rows: expands VesselID -1 to every active vessel, and skips an item that already
   has a row waiting to be sent to that vessel. */
CREATE OR ALTER PROCEDURE dbo.Sync_Enqueue
    @Items  dbo.Sync_ItemList READONLY,
    @Source nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH expanded AS
    (
        SELECT i.VesselID, i.ItemType, i.ItemID, i.Action
        FROM @Items i
        WHERE i.VesselID > 0
        UNION ALL
        SELECT v.VesselID, i.ItemType, i.ItemID, i.Action
        FROM @Items i
        JOIN dbo.V_Vessel v ON i.VesselID = -1 AND v.VesselID > 0 AND ISNULL(v.IsRetired, 0) = 0
    ),
    distinctItems AS
    (
        SELECT VesselID, ItemType, ItemID, MAX(Action) AS Action
        FROM expanded
        GROUP BY VesselID, ItemType, ItemID
    )
    INSERT dbo.Sync_Outbox (VesselID, ItemType, ItemID, Action, SourceTable)
    SELECT d.VesselID, d.ItemType, d.ItemID, d.Action, @Source
    FROM distinctItems d
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Sync_Outbox o
                      WHERE o.VesselID = d.VesselID AND o.ItemType = d.ItemType AND o.ItemID = d.ItemID
                        AND o.SentUtc IS NULL AND o.DownloadedUtc IS NULL);
END
GO

/* Manuals changed: every vessel that has the manual, or a manual below it in the tree
   (a parent manual is sent with its children). */
CREATE OR ALTER PROCEDURE dbo.Sync_EnqueueManuals
    @ManualIds dbo.Sync_ItemList READONLY,   -- ItemID = DM_ManualID, Action; VesselID ignored
    @Source    nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @items dbo.Sync_ItemList;

    ;WITH tree AS
    (
        SELECT m.ItemID AS RootID, m.ItemID AS NodeID, m.Action, 0 AS Lvl
        FROM @ManualIds m
        UNION ALL
        SELECT t.RootID, c.DM_ManualID, t.Action, t.Lvl + 1
        FROM tree t
        JOIN dbo.DM_Manual c ON c.ParentId = t.NodeID AND c.DM_ManualID <> t.NodeID
        WHERE t.Lvl < 20
    )
    INSERT @items (VesselID, ItemType, ItemID, Action)
    SELECT DISTINCT a.VesselID, N'Manual', t.RootID, t.Action
    FROM tree t
    JOIN dbo.DM_Manual_ToAck_Vsl a ON a.DM_ManualID = t.NodeID;

    IF EXISTS (SELECT 1 FROM @items) EXEC dbo.Sync_Enqueue @items, @Source;
END
GO

/* Circulars changed: every vessel the circular is assigned to. */
CREATE OR ALTER PROCEDURE dbo.Sync_EnqueueCirculars
    @CircularIds dbo.Sync_ItemList READONLY,  -- ItemID = CIR_MasterID, Action; VesselID ignored
    @Source      nvarchar(128)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @items dbo.Sync_ItemList;

    INSERT @items (VesselID, ItemType, ItemID, Action)
    SELECT DISTINCT a.VesselID, N'Circular', c.ItemID, c.Action
    FROM @CircularIds c
    JOIN dbo.CIR_ToAck_Vsl a ON a.CIR_MasterID = c.ItemID;

    IF EXISTS (SELECT 1 FROM @items) EXEC dbo.Sync_Enqueue @items, @Source;
END
GO

/* ---------------------------------------------------------------- Manuals ---- */

CREATE OR ALTER TRIGGER dbo.TR_DM_Manual_Sync ON dbo.DM_Manual
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @act char(1) = CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'U'
                                WHEN EXISTS (SELECT 1 FROM inserted) THEN 'I' ELSE 'D' END;
    DECLARE @ids dbo.Sync_ItemList;
    INSERT @ids (VesselID, ItemType, ItemID, Action)
    SELECT 0, N'Manual', DM_ManualID, @act FROM inserted
    UNION SELECT 0, N'Manual', DM_ManualID, @act FROM deleted;
    IF EXISTS (SELECT 1 FROM @ids) EXEC dbo.Sync_EnqueueManuals @ids, N'DM_Manual';
END
GO

CREATE OR ALTER TRIGGER dbo.TR_DM_ManualVersion_Sync ON dbo.DM_ManualVersion
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ids dbo.Sync_ItemList;
    INSERT @ids (VesselID, ItemType, ItemID, Action)
    SELECT 0, N'Manual', DM_ManualID, 'U' FROM inserted
    UNION SELECT 0, N'Manual', DM_ManualID, 'U' FROM deleted;
    IF EXISTS (SELECT 1 FROM @ids) EXEC dbo.Sync_EnqueueManuals @ids, N'DM_ManualVersion';
END
GO

CREATE OR ALTER TRIGGER dbo.TR_DM_Manual_Attachments_Sync ON dbo.DM_Manual_Attachments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ids dbo.Sync_ItemList;
    INSERT @ids (VesselID, ItemType, ItemID, Action)
    SELECT 0, N'Manual', DM_ManualID, 'U' FROM inserted
    UNION SELECT 0, N'Manual', DM_ManualID, 'U' FROM deleted;
    IF EXISTS (SELECT 1 FROM @ids) EXEC dbo.Sync_EnqueueManuals @ids, N'DM_Manual_Attachments';
END
GO

/* Assignment of a manual to a vessel (or -1 = all): that vessel gets the manual and the manuals above it. */
CREATE OR ALTER TRIGGER dbo.TR_DM_Manual_ToAck_Vsl_Sync ON dbo.DM_Manual_ToAck_Vsl
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @act char(1) = CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'U'
                                WHEN EXISTS (SELECT 1 FROM inserted) THEN 'I' ELSE 'D' END;
    DECLARE @items dbo.Sync_ItemList;

    ;WITH changed AS
    (
        SELECT VesselID, DM_ManualID FROM inserted
        UNION SELECT VesselID, DM_ManualID FROM deleted
    ),
    up AS
    (
        SELECT c.VesselID, c.DM_ManualID AS NodeID, 0 AS Lvl FROM changed c
        UNION ALL
        SELECT u.VesselID, p.ParentId, u.Lvl + 1
        FROM up u
        JOIN dbo.DM_Manual p ON p.DM_ManualID = u.NodeID
        WHERE p.ParentId <> 0 AND p.ParentId <> u.NodeID AND u.Lvl < 20
    )
    INSERT @items (VesselID, ItemType, ItemID, Action)
    SELECT DISTINCT VesselID, N'Manual', NodeID, @act FROM up;

    IF EXISTS (SELECT 1 FROM @items) EXEC dbo.Sync_Enqueue @items, N'DM_Manual_ToAck_Vsl';
END
GO

/* -------------------------------------------------------------- Circulars ---- */

CREATE OR ALTER TRIGGER dbo.TR_CIR_Master_Sync ON dbo.CIR_Master
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @act char(1) = CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'U'
                                WHEN EXISTS (SELECT 1 FROM inserted) THEN 'I' ELSE 'D' END;
    DECLARE @ids dbo.Sync_ItemList;
    INSERT @ids (VesselID, ItemType, ItemID, Action)
    SELECT 0, N'Circular', CIR_MasterID, @act FROM inserted
    UNION SELECT 0, N'Circular', CIR_MasterID, @act FROM deleted;
    IF EXISTS (SELECT 1 FROM @ids) EXEC dbo.Sync_EnqueueCirculars @ids, N'CIR_Master';
END
GO

CREATE OR ALTER TRIGGER dbo.TR_CIR_Blob_Sync ON dbo.CIR_Blob
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ids dbo.Sync_ItemList;
    INSERT @ids (VesselID, ItemType, ItemID, Action)
    SELECT 0, N'Circular', CIR_MasterID, 'U' FROM inserted WHERE CIR_MasterID IS NOT NULL
    UNION SELECT 0, N'Circular', CIR_MasterID, 'U' FROM deleted WHERE CIR_MasterID IS NOT NULL;
    IF EXISTS (SELECT 1 FROM @ids) EXEC dbo.Sync_EnqueueCirculars @ids, N'CIR_Blob';
END
GO

/* Assignment of a circular to a vessel (or -1 = all vessels). */
CREATE OR ALTER TRIGGER dbo.TR_CIR_ToAck_Vsl_Sync ON dbo.CIR_ToAck_Vsl
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @act char(1) = CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'U'
                                WHEN EXISTS (SELECT 1 FROM inserted) THEN 'I' ELSE 'D' END;
    DECLARE @items dbo.Sync_ItemList;
    INSERT @items (VesselID, ItemType, ItemID, Action)
    SELECT VesselID, N'Circular', CIR_MasterID, @act FROM inserted
    UNION SELECT VesselID, N'Circular', CIR_MasterID, @act FROM deleted;
    IF EXISTS (SELECT 1 FROM @items) EXEC dbo.Sync_Enqueue @items, N'CIR_ToAck_Vsl';
END
GO

/* ----------------------------------------------------------- Crew, categories ---- */

CREATE OR ALTER TRIGGER dbo.TR_Vsl_Staff_Sync ON dbo.Vsl_Staff
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @items dbo.Sync_ItemList;
    INSERT @items (VesselID, ItemType, ItemID, Action)
    SELECT VesselID, N'Crew', 0, 'U' FROM inserted
    UNION SELECT VesselID, N'Crew', 0, 'U' FROM deleted;
    IF EXISTS (SELECT 1 FROM @items) EXEC dbo.Sync_Enqueue @items, N'Vsl_Staff';
END
GO

CREATE OR ALTER TRIGGER dbo.TR_DM_Category_Sync ON dbo.DM_Category
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    DECLARE @items dbo.Sync_ItemList;
    INSERT @items (VesselID, ItemType, ItemID, Action) VALUES (-1, N'Categories', 0, 'U');
    EXEC dbo.Sync_Enqueue @items, N'DM_Category';
END
GO

CREATE OR ALTER TRIGGER dbo.TR_CIR_Category_Sync ON dbo.CIR_Category
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;
    DECLARE @items dbo.Sync_ItemList;
    INSERT @items (VesselID, ItemType, ItemID, Action) VALUES (-1, N'Categories', 0, 'U');
    EXEC dbo.Sync_Enqueue @items, N'CIR_Category';
END
GO

PRINT 'Sync_Outbox and its triggers are ready.';
GO
