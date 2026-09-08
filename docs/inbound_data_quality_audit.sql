/* Read-only: khong sua/xoa du lieu. Chay truoc va sau khi test luong Inbound. */
SET NOCOUNT ON;

-- 1. Phiếu đang kiểm nhận nhưng số thực giao đã đạt/vượt dự kiến.
SELECT inboundOrder.InboundOrderNumber, item.InboundOrderItemID, product.ProductCode,
       item.ExpectedQuantity, item.ReceivedQuantity
FROM dbo.InboundOrderItems AS item
JOIN dbo.InboundOrders AS inboundOrder ON inboundOrder.InboundOrderID = item.InboundOrderID
JOIN dbo.Products AS product ON product.ProductID = item.ProductID
WHERE inboundOrder.Status = 'IN_PROGRESS'
  AND item.ReceivedQuantity >= item.ExpectedQuantity;

-- 2. Header item lệch tổng chi tiết kiểm nhận.
SELECT inboundOrder.InboundOrderNumber, item.InboundOrderItemID, product.ProductCode,
       item.ReceivedQuantity AS HeaderDeliveredQuantity,
       COALESCE(SUM(detail.ReceivedQuantity), 0) AS ReceiptDetailQuantity
FROM dbo.InboundOrderItems AS item
JOIN dbo.InboundOrders AS inboundOrder ON inboundOrder.InboundOrderID = item.InboundOrderID
JOIN dbo.Products AS product ON product.ProductID = item.ProductID
LEFT JOIN dbo.InboundOrderDetails AS detail ON detail.InboundOrderItemID = item.InboundOrderItemID
GROUP BY inboundOrder.InboundOrderNumber, item.InboundOrderItemID, product.ProductCode, item.ReceivedQuantity
HAVING item.ReceivedQuantity <> COALESCE(SUM(detail.ReceivedQuantity), 0);

-- 3. Lớp nhập cũ NO-LOT bị dùng chung qua nhiều phiếu/ngày; dữ liệu này làm sai tuổi tồn FIFO/LIFO.
SELECT lot.ProductLotID, lot.ProductID, lot.LotNumber,
       COUNT(DISTINCT detail.InboundOrderID) AS InboundOrderCount,
       MIN(detail.RecordedAt) AS FirstReceiptAt,
       MAX(detail.RecordedAt) AS LastReceiptAt
FROM dbo.ProductLots AS lot
JOIN dbo.InboundOrderDetails AS detail ON detail.ProductLotID = lot.ProductLotID
WHERE lot.LotNumber LIKE N'NO-LOT-%'
GROUP BY lot.ProductLotID, lot.ProductID, lot.LotNumber
HAVING COUNT(DISTINCT detail.InboundOrderID) > 1
    OR CONVERT(date, MIN(detail.RecordedAt)) <> CONVERT(date, MAX(detail.RecordedAt));

-- 4. Hàng tốt ở bin nhưng chưa có giao dịch INBOUND (putaway chưa post ledger).
SELECT inboundOrder.InboundOrderNumber, detail.InboundOrderDetailID, product.ProductCode,
       location.LocationCode, detail.ReceivedQuantity
FROM dbo.InboundOrderDetails AS detail
JOIN dbo.InboundOrders AS inboundOrder ON inboundOrder.InboundOrderID = detail.InboundOrderID
JOIN dbo.Products AS product ON product.ProductID = detail.ProductID
JOIN dbo.StorageLocations AS location ON location.StorageLocationID = detail.StorageLocationID
LEFT JOIN dbo.InventoryTransactions AS transactionRow
  ON transactionRow.InboundOrderDetailID = detail.InboundOrderDetailID
WHERE detail.ConditionStatus = 'GOOD'
  AND location.LocationType = 'BIN'
  AND transactionRow.InventoryTransactionID IS NULL;
