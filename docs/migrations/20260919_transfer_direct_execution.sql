SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;

IF EXISTS
(
    SELECT 1
    FROM dbo.InventoryReservations AS reservation
    JOIN dbo.InventoryTransactions AS reserveTransaction
      ON reserveTransaction.InventoryReservationID = reservation.InventoryReservationID
     AND reserveTransaction.TransactionType = 'RESERVE'
     AND reserveTransaction.TransferOrderDetailID IS NOT NULL
    JOIN dbo.TransferOrderDetails AS detail
      ON detail.TransferOrderDetailID = reserveTransaction.TransferOrderDetailID
    JOIN dbo.TransferOrders AS transferOrder
      ON transferOrder.TransferOrderID = detail.TransferOrderID
    WHERE transferOrder.Status = 'APPROVED'
      AND reservation.Status IN ('ACTIVE', 'PARTIALLY_CONSUMED')
      AND reservation.ReservedQuantity > reservation.ConsumedQuantity
      AND EXISTS
      (
          SELECT 1
          FROM dbo.InventoryTransactions AS releasedTransaction
          WHERE releasedTransaction.InventoryReservationID = reservation.InventoryReservationID
            AND releasedTransaction.TransactionType = 'RELEASE_RESERVATION'
      )
)
    THROW 51140, N'Du lieu reservation Transfer khong nhat quan; da co giao dich release nhung reservation van hoat dong.', 1;

INSERT dbo.InventoryTransactions
(
    TransactionType,
    ProductID,
    StorageLocationID,
    ProductLotID,
    OnHandDelta,
    ReservedDelta,
    TransferOrderDetailID,
    InventoryReservationID,
    PerformedByUserID,
    TransactionAt,
    Notes
)
SELECT
    'RELEASE_RESERVATION',
    reservation.ProductID,
    reservation.StorageLocationID,
    reservation.ProductLotID,
    0,
    -(reservation.ReservedQuantity - reservation.ConsumedQuantity),
    reserveTransaction.TransferOrderDetailID,
    reservation.InventoryReservationID,
    COALESCE(transferOrder.ApprovedByUserID, transferOrder.CreatedByUserID),
    SYSUTCDATETIME(),
    N'Chuyen doi Transfer sang luong thuc hien truc tiep, khong giu ton.'
FROM dbo.InventoryReservations AS reservation
JOIN dbo.InventoryTransactions AS reserveTransaction
  ON reserveTransaction.InventoryReservationID = reservation.InventoryReservationID
 AND reserveTransaction.TransactionType = 'RESERVE'
 AND reserveTransaction.TransferOrderDetailID IS NOT NULL
JOIN dbo.TransferOrderDetails AS detail
  ON detail.TransferOrderDetailID = reserveTransaction.TransferOrderDetailID
JOIN dbo.TransferOrders AS transferOrder
  ON transferOrder.TransferOrderID = detail.TransferOrderID
WHERE transferOrder.Status = 'APPROVED'
  AND reservation.Status IN ('ACTIVE', 'PARTIALLY_CONSUMED')
  AND reservation.ReservedQuantity > reservation.ConsumedQuantity;

UPDATE reservation
SET Status = 'RELEASED',
    ReleasedAt = SYSUTCDATETIME()
FROM dbo.InventoryReservations AS reservation
JOIN dbo.InventoryTransactions AS reserveTransaction
  ON reserveTransaction.InventoryReservationID = reservation.InventoryReservationID
 AND reserveTransaction.TransactionType = 'RESERVE'
 AND reserveTransaction.TransferOrderDetailID IS NOT NULL
JOIN dbo.TransferOrderDetails AS detail
  ON detail.TransferOrderDetailID = reserveTransaction.TransferOrderDetailID
JOIN dbo.TransferOrders AS transferOrder
  ON transferOrder.TransferOrderID = detail.TransferOrderID
WHERE transferOrder.Status = 'APPROVED'
  AND reservation.Status IN ('ACTIVE', 'PARTIALLY_CONSUMED');

UPDATE dbo.TransferOrders
SET Status = 'DRAFT',
    Notes = LEFT(
        CASE
            WHEN Notes IS NULL OR LTRIM(RTRIM(Notes)) = '' THEN
                N'[DIRECT_EXECUTION] Bo buoc duyet; da giai phong hold cu va dua phieu ve cho thuc hien.'
            ELSE
                Notes + CHAR(13) + CHAR(10) +
                N'[DIRECT_EXECUTION] Bo buoc duyet; da giai phong hold cu va dua phieu ve cho thuc hien.'
        END,
        1000)
WHERE Status = 'APPROVED';

COMMIT TRANSACTION;
GO
