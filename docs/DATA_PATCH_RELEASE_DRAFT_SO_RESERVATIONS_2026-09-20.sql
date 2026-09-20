/*
BMWMS - one-time, idempotent data reconciliation.

Business rule: a DRAFT sales order records demand only. Inventory is reserved
only when Warehouse Manager approves the SO.

Prerequisite: inventory transaction triggers must exist and be enabled.
This script releases legacy active reservations created for draft SOs through
the immutable inventory ledger; it never updates dbo.Inventory directly.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.trg_InventoryTransactions_ApplyToInventory', N'TR') IS NULL
    THROW 51200, N'Thiếu trigger áp dụng sổ giao dịch tồn kho.', 1;
IF EXISTS
(
    SELECT 1 FROM sys.triggers
    WHERE object_id = OBJECT_ID(N'dbo.trg_InventoryTransactions_ApplyToInventory')
      AND is_disabled = 1
)
    THROW 51201, N'Trigger áp dụng tồn kho đang bị tắt.', 1;

DECLARE @PerformedByUserID BIGINT =
(
    SELECT TOP (1) u.UserID
    FROM dbo.Users u
    JOIN dbo.Roles r ON r.RoleID = u.RoleID
    WHERE r.RoleCode = 'SYSTEM_ADMIN' AND u.Status = 'ACTIVE'
    ORDER BY u.UserID
);
IF @PerformedByUserID IS NULL
    THROW 51202, N'Không có quản trị viên đang hoạt động để ghi lịch sử giải phóng.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.InventoryReservations ir
    JOIN dbo.SalesOrderDetails sod ON sod.SalesOrderDetailID = ir.SalesOrderDetailID
    JOIN dbo.SalesOrders so ON so.SalesOrderID = sod.SalesOrderID
    JOIN dbo.StocktakeLocationLocks sl ON sl.StorageLocationID = ir.StorageLocationID
    WHERE so.Status IN ('DRAFT', 'PENDING_CONFIRMATION')
      AND ir.Status IN ('ACTIVE', 'PARTIALLY_CONSUMED')
      AND ir.ReservedQuantity > ir.ConsumedQuantity
)
    THROW 51203, N'Có vị trí đang kiểm kê. Hoàn tất/hủy kiểm kê trước khi giải phóng giữ chỗ của SO nháp.', 1;

BEGIN TRANSACTION;

DECLARE @Targets TABLE
(
    InventoryReservationID BIGINT PRIMARY KEY,
    ProductID BIGINT NOT NULL,
    StorageLocationID BIGINT NOT NULL,
    ProductLotID BIGINT NOT NULL,
    ReleaseQuantity DECIMAL(18,4) NOT NULL
);

INSERT @Targets (InventoryReservationID, ProductID, StorageLocationID, ProductLotID, ReleaseQuantity)
SELECT ir.InventoryReservationID, ir.ProductID, ir.StorageLocationID, ir.ProductLotID,
       ir.ReservedQuantity - ir.ConsumedQuantity
FROM dbo.InventoryReservations ir WITH (UPDLOCK, HOLDLOCK)
JOIN dbo.SalesOrderDetails sod ON sod.SalesOrderDetailID = ir.SalesOrderDetailID
JOIN dbo.SalesOrders so ON so.SalesOrderID = sod.SalesOrderID
WHERE so.Status IN ('DRAFT', 'PENDING_CONFIRMATION')
  AND ir.Status IN ('ACTIVE', 'PARTIALLY_CONSUMED')
  AND ir.ReservedQuantity > ir.ConsumedQuantity;

INSERT dbo.InventoryTransactions
(
    TransactionType, ProductID, StorageLocationID, ProductLotID,
    OnHandDelta, ReservedDelta, InventoryReservationID,
    PerformedByUserID, TransactionAt, Notes
)
SELECT 'RELEASE_RESERVATION', ProductID, StorageLocationID, ProductLotID,
       0, -ReleaseQuantity, InventoryReservationID,
       @PerformedByUserID, SYSUTCDATETIME(),
       N'Giải phóng giữ chỗ cũ: SO vẫn ở trạng thái nháp; chỉ giữ tồn sau khi Manager duyệt.'
FROM @Targets;

UPDATE ir
SET ir.Status = 'RELEASED', ir.ReleasedAt = SYSUTCDATETIME()
FROM dbo.InventoryReservations ir
JOIN @Targets t ON t.InventoryReservationID = ir.InventoryReservationID;

UPDATE sod
SET sod.ReservedQuantity = 0
FROM dbo.SalesOrderDetails sod
JOIN dbo.SalesOrders so ON so.SalesOrderID = sod.SalesOrderID
WHERE so.Status IN ('DRAFT', 'PENDING_CONFIRMATION');

DECLARE @ReleasedReservations INT = (SELECT COUNT(*) FROM @Targets);
DECLARE @ReleasedQuantity DECIMAL(18,4) = (SELECT COALESCE(SUM(ReleaseQuantity), 0) FROM @Targets);

COMMIT TRANSACTION;

SELECT @ReleasedReservations AS ReleasedReservationCount,
       @ReleasedQuantity AS ReleasedQuantity,
       N'Hoàn tất. Có thể chạy lại an toàn; lần sau kết quả sẽ bằng 0.' AS Result;
