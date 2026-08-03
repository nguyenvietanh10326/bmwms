USE [BMWMS];
GO

-- Xóa dữ liệu cũ nếu chạy lại
DELETE FROM PurchaseOrderDetails WHERE PurchaseOrderID IN (SELECT PurchaseOrderID FROM PurchaseOrders WHERE PurchaseOrderNumber LIKE 'PO-SUP008%');
DELETE FROM InboundOrderItems WHERE InboundOrderId IN (SELECT InboundOrderId FROM InboundOrders WHERE InboundOrderNumber LIKE 'IN-SUP008%');
DELETE FROM InboundOrders WHERE InboundOrderNumber LIKE 'IN-SUP008%';
DELETE FROM PurchaseOrders WHERE PurchaseOrderNumber LIKE 'PO-SUP008%';

-- Tạo PurchaseOrder 1 (Đã hoàn thành)
INSERT INTO PurchaseOrders (PurchaseOrderNumber, SupplierId, OrderDate, ExpectedDeliveryDate, Status, CreatedByUserId, CreatedAt)
VALUES ('PO-SUP008-01', 6, GETDATE(), DATEADD(day, 7, GETDATE()), 'COMPLETED', 1, GETDATE());

DECLARE @PO1 BIGINT = SCOPE_IDENTITY();

INSERT INTO PurchaseOrderDetails (PurchaseOrderID, ProductID, OrderedQuantity, UnitPrice)
VALUES (@PO1, 1, 100, 50000);

-- Tạo InboundOrder 1
INSERT INTO InboundOrders (InboundOrderNumber, PurchaseOrderId, WarehouseId, SourceType, ExpectedReceiptDate, Status, CreatedByUserId, CreatedAt)
VALUES ('IN-SUP008-01', @PO1, 1, 'PURCHASE_ORDER', DATEADD(day, 7, GETDATE()), 'COMPLETED', 1, GETDATE());

DECLARE @IN1 BIGINT = SCOPE_IDENTITY();

INSERT INTO InboundOrderItems (InboundOrderId, ProductId, ExpectedQuantity, ReceivedQuantity, DamagedQuantity, ShortageQuantity)
VALUES (@IN1, 1, 100, 100, 0, 0);

-- Tạo PurchaseOrder 2 (Đang xử lý)
INSERT INTO PurchaseOrders (PurchaseOrderNumber, SupplierId, OrderDate, ExpectedDeliveryDate, Status, CreatedByUserId, CreatedAt)
VALUES ('PO-SUP008-02', 6, GETDATE(), DATEADD(day, 14, GETDATE()), 'CONFIRMED', 1, GETDATE());

DECLARE @PO2 BIGINT = SCOPE_IDENTITY();

INSERT INTO PurchaseOrderDetails (PurchaseOrderID, ProductID, OrderedQuantity, UnitPrice)
VALUES (@PO2, 1, 500, 50000);

-- Tạo InboundOrder 2 (Đang chờ)
INSERT INTO InboundOrders (InboundOrderNumber, PurchaseOrderId, WarehouseId, SourceType, ExpectedReceiptDate, Status, CreatedByUserId, CreatedAt)
VALUES ('IN-SUP008-02', @PO2, 1, 'PURCHASE_ORDER', DATEADD(day, 14, GETDATE()), 'ASSIGNED', 1, GETDATE());

DECLARE @IN2 BIGINT = SCOPE_IDENTITY();

INSERT INTO InboundOrderItems (InboundOrderId, ProductId, ExpectedQuantity, ReceivedQuantity, DamagedQuantity, ShortageQuantity)
VALUES (@IN2, 1, 500, 0, 0, 0);

GO
