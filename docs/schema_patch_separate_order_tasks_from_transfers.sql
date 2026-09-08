/*
    Tách tác nghiệp nhận/xếp hàng và lấy/xuất hàng khỏi phân hệ điều chuyển.
    Chạy một lần trên CSDL đã từng dùng cơ chế TR-WF-* tự động.
*/
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LegacyWorkflowTransfers TABLE (TransferOrderID BIGINT PRIMARY KEY);

    INSERT INTO @LegacyWorkflowTransfers (TransferOrderID)
    SELECT TransferOrderID
    FROM dbo.TransferOrders
    WHERE TransferOrderNumber LIKE 'TR-WF-%'
       OR Notes LIKE '[[]AUTO_INBOUND[]]%'
       OR Notes LIKE '[[]AUTO_OUTBOUND[]]%';

    /* PO/SO là chứng từ nguồn của phiếu nhập/xuất, không phải nguồn của phiếu điều chuyển. */
    UPDATE dbo.InboundOrders
    SET TransferOrderID = NULL
    WHERE SourceType IN ('PURCHASE_ORDER', 'SALES_RETURN')
      AND TransferOrderID IS NOT NULL;

    UPDATE dbo.OutboundOrders
    SET TransferOrderID = NULL
    WHERE SourceType IN ('SALES_ORDER', 'PURCHASE_RETURN')
      AND TransferOrderID IS NOT NULL;

    /* Chỉ xóa dữ liệu TR-WF cũ chưa từng phát sinh bút toán điều chuyển. */
    DELETE detail
    FROM dbo.TransferOrderDetails AS detail
    INNER JOIN @LegacyWorkflowTransfers AS legacy
        ON legacy.TransferOrderID = detail.TransferOrderID
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.InventoryTransactions AS transactionRow
        WHERE transactionRow.TransferOrderDetailID = detail.TransferOrderDetailID
    );

    DELETE transferRow
    FROM dbo.TransferOrders AS transferRow
    INNER JOIN @LegacyWorkflowTransfers AS legacy
        ON legacy.TransferOrderID = transferRow.TransferOrderID
    WHERE NOT EXISTS
          (SELECT 1 FROM dbo.TransferOrderDetails AS detail WHERE detail.TransferOrderID = transferRow.TransferOrderID)
      AND NOT EXISTS
          (SELECT 1 FROM dbo.InboundOrders AS inboundOrder WHERE inboundOrder.TransferOrderID = transferRow.TransferOrderID)
      AND NOT EXISTS
          (SELECT 1 FROM dbo.OutboundOrders AS outboundOrder WHERE outboundOrder.TransferOrderID = transferRow.TransferOrderID);

    IF EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.InboundOrders')
          AND name = N'CK_InboundOrders_SourceReference'
    )
        ALTER TABLE dbo.InboundOrders DROP CONSTRAINT CK_InboundOrders_SourceReference;

    ALTER TABLE dbo.InboundOrders WITH CHECK ADD CONSTRAINT CK_InboundOrders_SourceReference CHECK
    (
        (SourceType = 'PURCHASE_ORDER' AND PurchaseOrderID IS NOT NULL AND SalesOrderID IS NULL AND TransferOrderID IS NULL)
        OR
        (SourceType = 'SALES_RETURN' AND PurchaseOrderID IS NULL AND SalesOrderID IS NOT NULL AND TransferOrderID IS NULL)
        OR
        (SourceType = 'TRANSFER_ORDER' AND PurchaseOrderID IS NULL AND SalesOrderID IS NULL AND TransferOrderID IS NOT NULL)
    );
    ALTER TABLE dbo.InboundOrders CHECK CONSTRAINT CK_InboundOrders_SourceReference;

    IF EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.OutboundOrders')
          AND name = N'CK_OutboundOrders_SourceReference'
    )
        ALTER TABLE dbo.OutboundOrders DROP CONSTRAINT CK_OutboundOrders_SourceReference;

    ALTER TABLE dbo.OutboundOrders WITH CHECK ADD CONSTRAINT CK_OutboundOrders_SourceReference CHECK
    (
        (SourceType = 'SALES_ORDER' AND SalesOrderID IS NOT NULL AND PurchaseOrderID IS NULL AND TransferOrderID IS NULL)
        OR
        (SourceType = 'PURCHASE_RETURN' AND SalesOrderID IS NULL AND PurchaseOrderID IS NOT NULL AND TransferOrderID IS NULL)
        OR
        (SourceType = 'TRANSFER_ORDER' AND SalesOrderID IS NULL AND PurchaseOrderID IS NULL AND TransferOrderID IS NOT NULL)
    );
    ALTER TABLE dbo.OutboundOrders CHECK CONSTRAINT CK_OutboundOrders_SourceReference;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
