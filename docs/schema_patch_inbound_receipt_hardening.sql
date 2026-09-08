/*
  BMWMS - hardening luong kiem nhan/putaway Inbound.
  Script idempotent, chay tren database hien huu truoc khi test man Receipt/Putaway moi.

  Thay doi co chu dich:
  - ProductFixedLocations la goi y/uu tien, khong phai rang buoc cam Staff chon bin hop le khac.
  - Van giu bat bien: dung kho, BIN cho phep putaway, hang hong/cach ly vao QUARANTINE.
*/
SET XACT_ABORT ON;
GO

CREATE OR ALTER TRIGGER dbo.trg_InboundOrderDetails_Validate
ON dbo.InboundOrderDetails
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS receipt
        JOIN dbo.InboundOrders AS inboundOrder
          ON inboundOrder.InboundOrderID = receipt.InboundOrderID
        JOIN dbo.StorageLocations AS location
          ON location.StorageLocationID = receipt.StorageLocationID
        WHERE location.WarehouseID <> inboundOrder.WarehouseID
    )
        THROW 51001, N'Vi tri xac nhan nhap khong thuoc kho cua phieu nhap.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS receipt
        JOIN dbo.StorageLocations AS location
          ON location.StorageLocationID = receipt.StorageLocationID
        WHERE receipt.ConditionStatus = 'GOOD'
          AND location.LocationType = 'BIN'
          AND (location.IsPutawayAllowed = 0 OR location.Status IN ('BLOCKED','INACTIVE'))
    )
        THROW 51002, N'Hang tot chi duoc cat vao bin dang hoat dong va cho phep putaway.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS receipt
        JOIN dbo.StorageLocations AS location
          ON location.StorageLocationID = receipt.StorageLocationID
        WHERE receipt.ConditionStatus IN ('DAMAGED','QUARANTINED')
          AND location.LocationType <> 'QUARANTINE'
    )
        THROW 51003, N'Hang hong/cach ly phai duoc ghi nhan tai vi tri QUARANTINE.', 1;
END;
GO

/* Kiem tra nhanh constraint trang thai PO ma code su dung khi nhan du. */
IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.PurchaseOrders')
      AND name = N'CK_PurchaseOrders_Status'
      AND definition NOT LIKE N'%COMPLETED%'
)
    THROW 51020, N'CK_PurchaseOrders_Status chua cho phep COMPLETED. Hay dong bo Database_v3.sql truoc khi test.', 1;
GO
