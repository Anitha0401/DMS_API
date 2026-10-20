/* =====================================================================================================
   015_BackendFixes.sql  -  run on the OFFICE (HQ) database. Safe to run more than once.

   1. dbo.Notification_Read
        Manual and circular notifications are shared by all office users. Until now "mark as read" set one
        IsRead flag on the notification, so one user reading it hid it as new for everybody. This table keeps
        read/unread per user. The API uses it as soon as it exists (before that, the old shared flag is used).

   2. DM_Manual_ApprovedList.DM_ManualVersionID and .IsVoid
        Approvals were kept per manual only. When a released manual was edited (new version), every role that
        had approved an earlier version was skipped, and a manual that was sent back kept its approvals. Each
        approval now records the version it was given for; a send back or a deleted draft version marks that
        version's approvals void (they stay as history). The API counts only the current version's approvals
        once these columns exist (before that, the old behaviour applies).

   The office-only table is not in TableID_Setup and is never sent to vessels.
   ===================================================================================================== */
SET NOCOUNT ON;
GO

-- 1. Per-user read state of notifications ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Notification_Read', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notification_Read
    (
        Module          varchar(3)   NOT NULL,   -- 'DM' = DM_ManualNotification, 'CIR' = CIR_Notification
        NotificationID  bigint       NOT NULL,   -- DM_ManualNotificationId / CIR_NotificationID
        UserID          varchar(50)  NOT NULL,   -- HQ_Users.UserID
        ReadOn          datetime2(0) NOT NULL CONSTRAINT DF_Notification_Read_ReadOn DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Notification_Read PRIMARY KEY CLUSTERED (Module, UserID, NotificationID)
    );
    PRINT 'Created dbo.Notification_Read.';
END;
GO

-- 2. Approvals tied to a manual version ------------------------------------------------------------
IF COL_LENGTH('dbo.DM_Manual_ApprovedList', 'DM_ManualVersionID') IS NULL
BEGIN
    ALTER TABLE dbo.DM_Manual_ApprovedList ADD DM_ManualVersionID bigint NULL;
    PRINT 'Added DM_Manual_ApprovedList.DM_ManualVersionID.';
END;
GO

IF COL_LENGTH('dbo.DM_Manual_ApprovedList', 'IsVoid') IS NULL
BEGIN
    ALTER TABLE dbo.DM_Manual_ApprovedList ADD IsVoid int NULL
        CONSTRAINT DF_DM_Manual_ApprovedList_IsVoid DEFAULT (0);
    PRINT 'Added DM_Manual_ApprovedList.IsVoid.';
END;
GO

-- Manuals in their approval flow right now (between Draft 100 and Release 150): the approvals given since their
-- current version was made belong to that version, so the flow carries on where it is. Older approvals stay
-- without a version: history only, no longer counted.
UPDATE a
   SET a.DM_ManualVersionID = v.DM_ManualVersionID,
       a.IsVoid = 0
  FROM dbo.DM_Manual_ApprovedList a
  JOIN dbo.DM_Manual m ON m.DM_ManualID = a.DM_ManualID
 CROSS APPLY (SELECT TOP (1) x.DM_ManualVersionID, x.CreatedDate
                FROM dbo.DM_ManualVersion x
               WHERE x.DM_ManualID = m.DM_ManualID AND x.IsActive = 1 AND ISNULL(x.IsDisabled, 0) = 0
               ORDER BY x.DM_ManualVersionID DESC) v
 WHERE a.DM_ManualVersionID IS NULL
   AND m.DM_StatusID > 100 AND m.DM_StatusID < 150
   AND (v.CreatedDate IS NULL OR a.ApprovedDate IS NULL OR a.ApprovedDate >= v.CreatedDate);
PRINT CONCAT(@@ROWCOUNT, ' approval(s) of manuals now in approval linked to their current version.');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.DM_Manual_ApprovedList') AND name = N'IX_DM_Manual_ApprovedList_Version')
    CREATE INDEX IX_DM_Manual_ApprovedList_Version ON dbo.DM_Manual_ApprovedList (DM_ManualID, DM_ManualVersionID) INCLUDE (UserRole, HQDept, IsVoid);
GO

-- The approval list is office data. If it is set up to go to vessels, their databases need the two new columns too.
IF COL_LENGTH('dbo.TableID_Setup', 'IsForVsl') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.TableID_Setup WHERE TableName = 'DM_Manual_ApprovedList' AND IsForVsl = 1)
    PRINT 'NOTE: DM_Manual_ApprovedList is sent to vessels (TableID_Setup.IsForVsl = 1). Add DM_ManualVersionID bigint NULL and IsVoid int NULL to it in each vessel database.';
GO
