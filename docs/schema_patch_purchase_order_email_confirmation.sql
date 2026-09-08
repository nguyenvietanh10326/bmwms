/*
  Tách hai hành động của PO:
  1. Gửi email cho NCC: PO vẫn DRAFT.
  2. Người dùng xác nhận đã gửi: DRAFT -> CONFIRMED.

  Script idempotent, chạy một lần trên database hiện hữu trước khi dùng UI/API mới.
*/
SET XACT_ABORT ON;
GO

IF COL_LENGTH(N'dbo.PurchaseOrders', N'SupplierEmailSentByUserID') IS NULL
    ALTER TABLE dbo.PurchaseOrders ADD SupplierEmailSentByUserID BIGINT NULL;

IF COL_LENGTH(N'dbo.PurchaseOrders', N'SupplierEmailSentAt') IS NULL
    ALTER TABLE dbo.PurchaseOrders ADD SupplierEmailSentAt DATETIME2(0) NULL;

IF COL_LENGTH(N'dbo.PurchaseOrders', N'SupplierEmailSentTo') IS NULL
    ALTER TABLE dbo.PurchaseOrders ADD SupplierEmailSentTo NVARCHAR(320) NULL;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_PurchaseOrders_SupplierEmailSentBy'
      AND parent_object_id = OBJECT_ID(N'dbo.PurchaseOrders')
)
    ALTER TABLE dbo.PurchaseOrders
        ADD CONSTRAINT FK_PurchaseOrders_SupplierEmailSentBy
        FOREIGN KEY (SupplierEmailSentByUserID) REFERENCES dbo.Users(UserID);
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PurchaseOrders')
      AND name = N'IX_PurchaseOrders_EmailConfirmation'
)
    CREATE INDEX IX_PurchaseOrders_EmailConfirmation
        ON dbo.PurchaseOrders(Status, SupplierEmailSentAt)
        INCLUDE (SupplierID, SupplierEmailSentByUserID, SupplierEmailSentTo);
GO
