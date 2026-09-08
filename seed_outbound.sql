USE BMWMS;

-- 1. Thêm một Sales Order đã được CONFIRMED
INSERT INTO SalesOrders (SalesOrderNumber, CustomerID, OrderDate, ExpectedIssueDate, Status, Notes, CreatedByUserID, CreatedAt)
VALUES ('SO-TEST-' + SUBSTRING(CAST(NEWID() AS VARCHAR(36)), 1, 8), 1, GETDATE(), GETDATE(), 'CONFIRMED', N'Đơn hàng xuất kho test', 1, GETDATE());

DECLARE @SalesOrderId BIGINT = SCOPE_IDENTITY();

-- 2. Thêm Sales Order Detail
INSERT INTO SalesOrderDetails (SalesOrderID, ProductID, OrderedQuantity, ReservedQuantity, FulfilledQuantity, UnitPrice, Notes)
VALUES (@SalesOrderId, 3, 50, 50, 0, 100000, N'Sản phẩm test');

-- 3. Tạo luôn một Outbound Order để test
INSERT INTO OutboundOrders (OutboundOrderNumber, WarehouseID, SourceType, SalesOrderID, ExpectedIssueDate, Status, Notes, CreatedByUserID, AssignedToUserID, CreatedAt)
VALUES ('OUT-TEST-' + SUBSTRING(CAST(NEWID() AS VARCHAR(36)), 1, 8), 1, 'SALES_ORDER', @SalesOrderId, GETDATE(), 'ASSIGNED', N'Lệnh xuất kho test', 1, 1, GETDATE());

DECLARE @OutboundOrderId BIGINT = SCOPE_IDENTITY();

-- 4. Tạo chi tiết Outbound Order
INSERT INTO OutboundOrderItems (OutboundOrderID, ProductID, RequestedQuantity, IssuedQuantity, Notes)
VALUES (@OutboundOrderId, 3, 50, 0, N'Test chi tiết xuất kho');
