SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/*
    The shared Update15-9 schema predates the redesigned Transfer workflow.
    Keep the legacy ASSIGNED/IN_PROGRESS rows readable while enabling
    DRAFT -> APPROVED -> COMPLETED for new internal transfers.
*/
IF OBJECT_ID(N'dbo.TransferOrders', N'U') IS NULL
    THROW 51001, 'Missing dbo.TransferOrders. Apply the shared BMWMS schema first.', 1;

IF COL_LENGTH(N'dbo.TransferOrders', N'ApprovedByUserID') IS NULL
    ALTER TABLE dbo.TransferOrders ADD ApprovedByUserID BIGINT NULL;

IF COL_LENGTH(N'dbo.TransferOrders', N'ApprovedAt') IS NULL
    ALTER TABLE dbo.TransferOrders ADD ApprovedAt DATETIME2(7) NULL;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_TransferOrders_ApprovedBy'
      AND parent_object_id = OBJECT_ID(N'dbo.TransferOrders')
)
    ALTER TABLE dbo.TransferOrders WITH CHECK
        ADD CONSTRAINT FK_TransferOrders_ApprovedBy
        FOREIGN KEY (ApprovedByUserID) REFERENCES dbo.Users(UserID);

IF EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_TransferOrders_Status'
      AND parent_object_id = OBJECT_ID(N'dbo.TransferOrders')
)
    ALTER TABLE dbo.TransferOrders DROP CONSTRAINT CK_TransferOrders_Status;

ALTER TABLE dbo.TransferOrders WITH CHECK
    ADD CONSTRAINT CK_TransferOrders_Status CHECK
    (
        Status IN ('DRAFT', 'APPROVED', 'ASSIGNED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')
    );

IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_InventoryReservations_ExactlyOneSource'
      AND parent_object_id = OBJECT_ID(N'dbo.InventoryReservations')
)
BEGIN
    ALTER TABLE dbo.InventoryReservations
        DROP CONSTRAINT CK_InventoryReservations_ExactlyOneSource;
END;
GO

/*
    Transfer approvals reserve stock directly from source bins, but the current
    reservation table only has explicit source columns for sales/outbound flows.
    Until a dedicated TransferOrderDetailID column is added to InventoryReservations,
    allow internal transfer reservations to have both order-source columns NULL.
    The related InventoryTransactions rows still carry TransferOrderDetailID.
*/
ALTER TABLE dbo.InventoryReservations WITH CHECK
    ADD CONSTRAINT CK_InventoryReservations_ExactlyOneSource CHECK
    (
        (SalesOrderDetailID IS NOT NULL AND OutboundOrderItemID IS NULL)
        OR (SalesOrderDetailID IS NULL AND OutboundOrderItemID IS NOT NULL)
        OR (SalesOrderDetailID IS NULL AND OutboundOrderItemID IS NULL)
    );
GO

COMMIT TRANSACTION;
GO
