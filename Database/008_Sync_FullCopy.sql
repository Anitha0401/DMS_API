/* =====================================================================================
   008_Sync_FullCopy.sql  -  run on the OFFICE (HQ) database, after 006_VesselSyncOutbox.sql

   One-time full copy: gives every vessel (or one) every manual and circular that exists now,
   whether or not it is assigned to that vessel in DM_Manual_ToAck_Vsl / CIR_ToAck_Vsl.

   What still applies (only the vessel assignment is ignored):
     - manuals:   not deleted and with a released version; the manuals above them go too
     - circulars: not deleted and not Temp or Draft
   Items copied this way have no ranks to acknowledge on board (status "Sent"), unless they are
   also assigned to the vessel. Once copied they stay up to date on board, and are removed from
   the vessel if they are later deleted (or a manual loses its released version) at HQ.
   Manuals and circulars created after this script follow the normal assignment rules.

   How it works: the snapshot is kept in dbo.Sync_FullCopyItem, which the office sync reads like
   an assignment, and a Sync_Outbox row is written for each item so the next pull sends it.

   Settings:
     @VesselID  -1 = every active vessel, or one VesselID
     @Apply      0 = preview the counts only, 1 = take the snapshot and queue the items

   Safe to run again: it only adds items that are not in the snapshot yet.
   To undo for a vessel:  DELETE FROM dbo.Sync_FullCopyItem WHERE VesselID = <id>;
   (its next sync then removes the items it holds only because of the copy).
   ===================================================================================== */

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NOT NULL AND OBJECT_ID('dbo.Sync_FullCopyItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_FullCopyItem
    (
        VesselID  int           NOT NULL,
        ItemType  nvarchar(20)  NOT NULL,   -- Manual | Circular
        ItemID    bigint        NOT NULL,   -- DM_ManualID / CIR_MasterID
        AddedUtc  datetime2(3)  NOT NULL CONSTRAINT DF_Sync_FullCopyItem_AddedUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Sync_FullCopyItem PRIMARY KEY (VesselID, ItemType, ItemID)
    );
END
GO

DECLARE @VesselID int = -1;
DECLARE @Apply    bit = 0;

IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NULL OR OBJECT_ID('dbo.Sync_Enqueue', 'P') IS NULL
   OR OBJECT_ID('dbo.DM_Manual', 'U') IS NULL
BEGIN
    RAISERROR('This is not the office database, or 006_VesselSyncOutbox.sql has not been run on it.', 16, 1);
    RETURN;
END

DECLARE @vessels TABLE (VesselID int PRIMARY KEY);
INSERT @vessels (VesselID)
SELECT v.VesselID
  FROM dbo.V_Vessel v
 WHERE v.VesselID > 0 AND ISNULL(v.IsRetired, 0) = 0
   AND (@VesselID = -1 OR v.VesselID = @VesselID);

IF NOT EXISTS (SELECT 1 FROM @vessels)
BEGIN
    RAISERROR('No active vessel with VesselID %d in V_Vessel.', 16, 1, @VesselID);
    RETURN;
END

-- Everything that exists now, per vessel
DECLARE @items dbo.Sync_ItemList;

INSERT @items (VesselID, ItemType, ItemID, Action)
SELECT v.VesselID, N'Manual', m.DM_ManualID, 'U'
  FROM @vessels v
 CROSS JOIN dbo.DM_Manual m
 WHERE ISNULL(m.IsDeleted, 0) <> 1
   AND EXISTS (SELECT 1 FROM dbo.DM_ManualVersion mv WHERE mv.DM_ManualID = m.DM_ManualID AND mv.IsActive = 1);

INSERT @items (VesselID, ItemType, ItemID, Action)
SELECT v.VesselID, N'Circular', c.CIR_MasterID, 'U'
  FROM @vessels v
 CROSS JOIN dbo.CIR_Master c
 WHERE ISNULL(c.IsDeleted, 0) <> 1
   AND ISNULL(c.Status, N'') NOT IN (N'Temp', N'Draft');

-- 1. Preview: per vessel, what the copy covers and what is new compared with a previous run
SELECT i.VesselID,
       SUM(CASE WHEN i.ItemType = N'Manual'   THEN 1 ELSE 0 END) AS Manuals,
       SUM(CASE WHEN i.ItemType = N'Circular' THEN 1 ELSE 0 END) AS Circulars,
       SUM(CASE WHEN f.ItemID IS NULL         THEN 1 ELSE 0 END) AS NotCopiedBefore
  FROM @items i
  LEFT JOIN dbo.Sync_FullCopyItem f
         ON f.VesselID = i.VesselID AND f.ItemType = i.ItemType AND f.ItemID = i.ItemID
 GROUP BY i.VesselID
 ORDER BY i.VesselID;

-- 2. Take the snapshot and queue the items
IF @Apply = 1
BEGIN
    BEGIN TRAN;

    INSERT dbo.Sync_FullCopyItem (VesselID, ItemType, ItemID)
    SELECT i.VesselID, i.ItemType, i.ItemID
      FROM @items i
     WHERE NOT EXISTS (SELECT 1 FROM dbo.Sync_FullCopyItem f
                        WHERE f.VesselID = i.VesselID AND f.ItemType = i.ItemType AND f.ItemID = i.ItemID);

    DECLARE @source nvarchar(128) = N'Script: 008_Sync_FullCopy';
    EXEC dbo.Sync_Enqueue @Items = @items, @Source = @source;

    COMMIT;

    SELECT VesselID, COUNT(*) AS ItemsInSnapshot
      FROM dbo.Sync_FullCopyItem
     WHERE VesselID IN (SELECT VesselID FROM @vessels)
     GROUP BY VesselID
     ORDER BY VesselID;
END
ELSE
    PRINT 'Preview only: nothing written. Set @Apply = 1 and run again to copy everything to the vessels.';
