SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.StocktakeLocationLocks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StocktakeLocationLocks
    (
        StorageLocationID  BIGINT NOT NULL,
        StocktakeSessionID BIGINT NOT NULL,
        LockedByUserID     BIGINT NOT NULL,
        LockedAt           DATETIME2(0) NOT NULL,
        CONSTRAINT PK_StocktakeLocationLocks PRIMARY KEY (StorageLocationID),
        CONSTRAINT UQ_StocktakeLocationLocks_SessionLocation UNIQUE (StocktakeSessionID, StorageLocationID),
        CONSTRAINT FK_StocktakeLocationLocks_Location FOREIGN KEY (StorageLocationID) REFERENCES dbo.StorageLocations(StorageLocationID),
        CONSTRAINT FK_StocktakeLocationLocks_Session FOREIGN KEY (StocktakeSessionID) REFERENCES dbo.StocktakeSessions(StocktakeSessionID),
        CONSTRAINT FK_StocktakeLocationLocks_User FOREIGN KEY (LockedByUserID) REFERENCES dbo.Users(UserID)
    );
    CREATE INDEX IX_StocktakeLocationLocks_Session ON dbo.StocktakeLocationLocks(StocktakeSessionID);
END;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_StocktakeSessions_Status')
    ALTER TABLE dbo.StocktakeSessions DROP CONSTRAINT CK_StocktakeSessions_Status;

ALTER TABLE dbo.StocktakeSessions WITH CHECK ADD CONSTRAINT CK_StocktakeSessions_Status CHECK
(
    Status IN ('SCHEDULED','IN_PROGRESS','COUNTED','PENDING_APPROVAL','COMPLETED','CANCELLED','REJECTED')
);

COMMIT TRANSACTION;
GO

CREATE OR ALTER TRIGGER dbo.trg_InventoryTransactions_BlockStocktakeLockedLocation
ON dbo.InventoryTransactions
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.StocktakeLocationLocks l ON l.StorageLocationID = i.StorageLocationID
        LEFT JOIN dbo.StocktakeItems si ON si.StocktakeItemID = i.StocktakeItemID
        WHERE i.TransactionType <> 'STOCKTAKE_ADJUSTMENT'
           OR si.StocktakeSessionID IS NULL
           OR si.StocktakeSessionID <> l.StocktakeSessionID
    )
    BEGIN
        THROW 51130, N'Vị trí đang bị khóa bởi phiếu kiểm kho.', 1;
    END;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_Inventory_BlockStocktakeLockedLocation
ON dbo.Inventory
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @StocktakeSessionID BIGINT = TRY_CAST(SESSION_CONTEXT(N'StocktakeSessionID') AS BIGINT);

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT StorageLocationID FROM inserted
            UNION
            SELECT StorageLocationID FROM deleted
        ) affected
        JOIN dbo.StocktakeLocationLocks l ON l.StorageLocationID = affected.StorageLocationID
        WHERE @StocktakeSessionID IS NULL OR l.StocktakeSessionID <> @StocktakeSessionID
    )
        THROW 51131, N'Không thể cập nhật tồn kho vì vị trí đang được kiểm kho.', 1;
END;
GO
