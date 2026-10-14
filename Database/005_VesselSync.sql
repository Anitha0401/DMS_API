/* =====================================================================================
   DMS office database: vessel sync tables
   Run once on the office (HQ) database, the one in ConnectionStrings:LocalDBConnection.
   Safe to run again: every object is created only when it is missing.

   Sync_Vessel      one row per vessel: last contact, last good sync, what the ship reports
   Sync_Log         one row per sync call (Down = office to vessel, Up = vessel to office)
   Sync_VesselKey   the vessel's sync key (only a SHA-256 hash is stored)
   Sync_VesselItem  what each vessel has received: per item, the version key sent and confirmed
   ===================================================================================== */

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.Sync_Vessel', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_Vessel
    (
        VesselID          int            NOT NULL CONSTRAINT PK_Sync_Vessel PRIMARY KEY,
        LastContactUtc    datetime2(3)   NULL,   -- any call from the vessel (pull, push, heartbeat)
        LastSuccessUtc    datetime2(3)   NULL,   -- last pull or push that completed
        LastPullUtc       datetime2(3)   NULL,
        LastPushUtc       datetime2(3)   NULL,
        LastStatus        nvarchar(20)   NULL,   -- OK | Failed
        LastError         nvarchar(1000) NULL,
        PendingOnShip     int            NOT NULL CONSTRAINT DF_Sync_Vessel_PendingOnShip DEFAULT (0), -- acknowledgements waiting on board
        CurrentActivity   nvarchar(100)  NULL,   -- e.g. "Downloading attachments"
        ProgressPercent   int            NULL,   -- 0-100 while the ship is busy, NULL when idle
        AppVersion        nvarchar(50)   NULL
    );
END
GO

IF OBJECT_ID('dbo.Sync_Log', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_Log
    (
        SyncLogID    bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sync_Log PRIMARY KEY,
        VesselID     int            NOT NULL,
        Direction    nvarchar(10)   NOT NULL,    -- Down | Up
        StartedUtc   datetime2(3)   NOT NULL,
        FinishedUtc  datetime2(3)   NULL,
        ItemsCount   int            NOT NULL CONSTRAINT DF_Sync_Log_ItemsCount DEFAULT (0),
        Status       nvarchar(20)   NOT NULL,    -- OK | Failed
        Error        nvarchar(1000) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sync_Log_Vessel_Started' AND object_id = OBJECT_ID('dbo.Sync_Log'))
    CREATE INDEX IX_Sync_Log_Vessel_Started ON dbo.Sync_Log (VesselID, StartedUtc DESC);
GO

IF OBJECT_ID('dbo.Sync_VesselKey', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_VesselKey
    (
        VesselID     int            NOT NULL CONSTRAINT PK_Sync_VesselKey PRIMARY KEY,
        KeyHash      char(64)       NOT NULL,    -- SHA-256 of the key, upper-case hex. The key itself is never stored.
        IsActive     bit            NOT NULL CONSTRAINT DF_Sync_VesselKey_IsActive DEFAULT (1),
        CreatedUtc   datetime2(3)   NOT NULL,
        CreatedBy    nvarchar(150)  NULL,
        LastUsedUtc  datetime2(3)   NULL
    );
END
GO

IF OBJECT_ID('dbo.Sync_VesselItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sync_VesselItem
    (
        VesselID      int           NOT NULL,
        ItemType      nvarchar(20)  NOT NULL,    -- Manual | Circular | Crew | Categories
        ItemID        bigint        NOT NULL,    -- 0 for Crew and Categories (sent as whole lists)
        PendingKey    char(64)      NULL,        -- version sent, not yet confirmed by the vessel ('DELETED' = removal sent)
        PendingUtc    datetime2(3)  NULL,
        ConfirmedKey  char(64)      NULL,        -- version the vessel has
        ConfirmedUtc  datetime2(3)  NULL,
        CONSTRAINT PK_Sync_VesselItem PRIMARY KEY (VesselID, ItemType, ItemID)
    );
END
GO

/* Indexes that make the per-vessel queries cheap. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CIR_ToAck_Vsl_Vessel' AND object_id = OBJECT_ID('dbo.CIR_ToAck_Vsl'))
    CREATE INDEX IX_CIR_ToAck_Vsl_Vessel ON dbo.CIR_ToAck_Vsl (VesselID, CIR_MasterID) INCLUDE (UserRank, IsActive);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DM_Manual_ToAck_Vsl_Vessel' AND object_id = OBJECT_ID('dbo.DM_Manual_ToAck_Vsl'))
    CREATE INDEX IX_DM_Manual_ToAck_Vsl_Vessel ON dbo.DM_Manual_ToAck_Vsl (VesselID, DM_ManualID) INCLUDE (UserRank, IsActive);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vsl_Staff_Vessel' AND object_id = OBJECT_ID('dbo.Vsl_Staff'))
    CREATE INDEX IX_Vsl_Staff_Vessel ON dbo.Vsl_Staff (VesselID);
GO

PRINT 'Vessel sync tables are ready.';
GO
