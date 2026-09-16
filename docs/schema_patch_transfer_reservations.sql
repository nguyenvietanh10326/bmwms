SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

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
