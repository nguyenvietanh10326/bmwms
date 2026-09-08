/*
===============================================================================
 BMWMS - Building Materials Warehouse Management System
 Database To-Be v3.0 | Microsoft SQL Server 2019+

 Pham vi thiet ke:
 - Purchase Order -> Inbound Order; Sales Order -> Outbound Order.
 - Khong luu Invoice, Goods Receipt, Putaway Task, Picking Task.
 - Inventory la bang tong hop theo Product + Storage Location + Product Lot,
   duoc cap nhat tu InventoryTransactions.
 - Stock Adjustment khong la thuc the rieng; dieu chinh phat sinh tu
   StocktakeItems va InventoryTransactions.
 - Manager tao yeu cau mat hang/so luong; Staff xac nhan vi tri, lo va so luong
   thuc te tren cac bang *OrderDetails.
 - Van hanh hien tai dung mot Warehouse chinh; schema van cho phep khai bao kho
   dich/nguon de xu ly Transfer Order ma khong phai sua cau truc database.
===============================================================================
*/

USE [master];
GO

IF DB_ID(N'BMWMS') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [BMWMS]');
END;
GO

/*
  Deferred copy of reporting views. The executable definitions are placed at
  the end of this script, after all dependent tables have been created.

-- Deferred reporting definitions (commented out).

CREATE OR ALTER VIEW dbo.vw_InventoryAvailability
AS
SELECT
    w.WarehouseID,
    w.WarehouseCode,
    w.WarehouseName,
    sl.StorageLocationID,
    sl.LocationCode,
    sl.LocationType,
    p.ProductID,
    p.ProductCode,
    p.ProductName,
    u.UnitCode,
    pl.ProductLotID,
    pl.LotNumber,
    pl.FirstReceivedDate,
    pl.ExpiryDate,
    i.OnHandQuantity,
    i.ReservedQuantity,
    CASE
        WHEN p.Status <> 'ACTIVE'
          OR pl.Status <> 'AVAILABLE'
          OR sl.IsPickable = 0
          OR sl.Status IN ('BLOCKED','INACTIVE')
          OR sl.LocationType = 'QUARANTINE'
        THEN CONVERT(DECIMAL(18,4), 0)
        ELSE i.AvailableQuantity
    END AS AvailableQuantity,
    p.RotationMethod,
    i.LastUpdatedAt
FROM dbo.Inventory i
JOIN dbo.Products p ON p.ProductID = i.ProductID
JOIN dbo.UnitsOfMeasure u ON u.UnitOfMeasureID = p.UnitOfMeasureID
JOIN dbo.ProductLots pl ON pl.ProductLotID = i.ProductLotID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
JOIN dbo.Warehouses w ON w.WarehouseID = sl.WarehouseID;
GO

CREATE OR ALTER VIEW dbo.vw_ProductLocationLookup
AS
SELECT
    ProductID, ProductCode, ProductName,
    WarehouseID, WarehouseCode, WarehouseName,
    StorageLocationID, LocationCode,
    ProductLotID, LotNumber, ExpiryDate,
    OnHandQuantity, ReservedQuantity, AvailableQuantity, UnitCode
FROM dbo.vw_InventoryAvailability
WHERE OnHandQuantity > 0;
GO

CREATE OR ALTER VIEW dbo.vw_ProductTransactionHistory
AS
SELECT
    t.InventoryTransactionID,
    t.TransactionAt,
    t.TransactionType,
    t.ProductID,
    p.ProductCode,
    p.ProductName,
    sl.WarehouseID,
    sl.StorageLocationID,
    sl.LocationCode,
    t.ProductLotID,
    pl.LotNumber,
    t.OnHandDelta,
    t.ReservedDelta,
    t.InboundOrderDetailID,
    t.OutboundOrderDetailID,
    t.TransferOrderDetailID,
    t.StocktakeItemID,
    t.InventoryReservationID,
    t.PerformedByUserID,
    u.FullName AS PerformedBy,
    t.Notes
FROM dbo.InventoryTransactions t
JOIN dbo.Products p ON p.ProductID = t.ProductID
JOIN dbo.ProductLots pl ON pl.ProductLotID = t.ProductLotID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = t.StorageLocationID
JOIN dbo.Users u ON u.UserID = t.PerformedByUserID;
GO

CREATE OR ALTER VIEW dbo.vw_LowStockAlerts
AS
WITH AvailableByWarehouse AS
(
    SELECT sl.WarehouseID, i.ProductID,
           SUM
           (
               CASE WHEN sl.IsPickable = 1
                          AND sl.Status NOT IN ('BLOCKED','INACTIVE')
                          AND sl.LocationType <> 'QUARANTINE'
                     THEN i.AvailableQuantity ELSE 0 END
           ) AS AvailableQuantity
    FROM dbo.Inventory i
    JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
    GROUP BY sl.WarehouseID, i.ProductID
)
SELECT
    policy.WarehouseID,
    w.WarehouseCode,
    policy.ProductID,
    p.ProductCode,
    p.ProductName,
    policy.MinimumStockQuantity,
    COALESCE(a.AvailableQuantity, 0) AS AvailableQuantity,
    policy.MinimumStockQuantity - COALESCE(a.AvailableQuantity, 0) AS ShortageQuantity
FROM dbo.ProductWarehousePolicies policy
JOIN dbo.Warehouses w ON w.WarehouseID = policy.WarehouseID
JOIN dbo.Products p ON p.ProductID = policy.ProductID
LEFT JOIN AvailableByWarehouse a ON a.WarehouseID = policy.WarehouseID AND a.ProductID = policy.ProductID
WHERE p.Status = 'ACTIVE'
  AND COALESCE(a.AvailableQuantity, 0) < policy.MinimumStockQuantity;
GO

CREATE OR ALTER VIEW dbo.vw_ExpiringLotAlerts
AS
SELECT
    sl.WarehouseID,
    w.WarehouseCode,
    i.ProductID,
    p.ProductCode,
    p.ProductName,
    i.ProductLotID,
    pl.LotNumber,
    pl.ExpiryDate,
    DATEDIFF(DAY, CONVERT(DATE, SYSUTCDATETIME()), pl.ExpiryDate) AS DaysToExpiry,
    SUM(i.OnHandQuantity) AS OnHandQuantity,
    SUM(i.AvailableQuantity) AS AvailableQuantity,
    policy.ExpiryWarningDays
FROM dbo.Inventory i
JOIN dbo.Products p ON p.ProductID = i.ProductID
JOIN dbo.ProductLots pl ON pl.ProductLotID = i.ProductLotID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
JOIN dbo.Warehouses w ON w.WarehouseID = sl.WarehouseID
JOIN dbo.ProductWarehousePolicies policy ON policy.ProductID = i.ProductID AND policy.WarehouseID = sl.WarehouseID
WHERE pl.ExpiryDate IS NOT NULL
  AND pl.Status IN ('AVAILABLE','QUARANTINED')
  AND i.OnHandQuantity > 0
  AND DATEDIFF(DAY, CONVERT(DATE, SYSUTCDATETIME()), pl.ExpiryDate) <= policy.ExpiryWarningDays
GROUP BY sl.WarehouseID, w.WarehouseCode, i.ProductID, p.ProductCode, p.ProductName,
         i.ProductLotID, pl.LotNumber, pl.ExpiryDate, policy.ExpiryWarningDays;
GO

CREATE OR ALTER VIEW dbo.vw_OverdueOrders
AS
SELECT
    CAST('INBOUND_ORDER' AS VARCHAR(30)) AS DocumentType,
    InboundOrderID AS DocumentID,
    InboundOrderNumber AS DocumentNumber,
    WarehouseID,
    DueDate,
    DATEDIFF(DAY, DueDate, CONVERT(DATE, SYSUTCDATETIME())) AS DaysOverdue,
    Status
FROM dbo.InboundOrders
WHERE DueDate < CONVERT(DATE, SYSUTCDATETIME())
  AND Status NOT IN ('COMPLETED','CANCELLED')
UNION ALL
SELECT
    'OUTBOUND_ORDER', OutboundOrderID, OutboundOrderNumber, WarehouseID, DueDate,
    DATEDIFF(DAY, DueDate, CONVERT(DATE, SYSUTCDATETIME())), Status
FROM dbo.OutboundOrders
WHERE DueDate < CONVERT(DATE, SYSUTCDATETIME())
  AND Status NOT IN ('COMPLETED','CANCELLED');
GO

CREATE OR ALTER VIEW dbo.vw_InboundReport
AS
SELECT
    io.InboundOrderID,
    io.InboundOrderNumber,
    io.SourceType,
    io.PurchaseOrderID,
    io.SalesOrderID,
    io.TransferOrderID,
    io.ParentInboundOrderID,
    io.WarehouseID,
    io.ExpectedReceiptDate,
    io.Status,
    io.ConfirmedAt,
    ioi.ProductID,
    p.ProductCode,
    p.ProductName,
    ioi.ExpectedQuantity,
    ioi.ReceivedQuantity,
    ioi.DamagedQuantity,
    ioi.ShortageQuantity
FROM dbo.InboundOrders io
JOIN dbo.InboundOrderItems ioi ON ioi.InboundOrderID = io.InboundOrderID
JOIN dbo.Products p ON p.ProductID = ioi.ProductID;
GO

CREATE OR ALTER VIEW dbo.vw_OutboundReport
AS
SELECT
    oo.OutboundOrderID,
    oo.OutboundOrderNumber,
    oo.SourceType,
    oo.SalesOrderID,
    oo.PurchaseOrderID,
    oo.TransferOrderID,
    oo.WarehouseID,
    oo.ExpectedIssueDate,
    oo.Status,
    oo.ConfirmedAt,
    ooi.ProductID,
    p.ProductCode,
    p.ProductName,
    ooi.RequestedQuantity,
    ooi.IssuedQuantity
FROM dbo.OutboundOrders oo
JOIN dbo.OutboundOrderItems ooi ON ooi.OutboundOrderID = oo.OutboundOrderID
JOIN dbo.Products p ON p.ProductID = ooi.ProductID;
GO

CREATE OR ALTER VIEW dbo.vw_InventoryInOutSummary
AS
SELECT
    CONVERT(DATE, t.TransactionAt) AS TransactionDate,
    sl.WarehouseID,
    t.ProductID,
    p.ProductCode,
    p.ProductName,
    SUM(CASE WHEN t.TransactionType IN ('INBOUND','TRANSFER_IN') THEN t.OnHandDelta ELSE 0 END) AS InboundQuantity,
    SUM(CASE WHEN t.TransactionType IN ('OUTBOUND','TRANSFER_OUT') THEN -t.OnHandDelta ELSE 0 END) AS OutboundQuantity,
    SUM(CASE WHEN t.TransactionType = 'STOCKTAKE_ADJUSTMENT' THEN t.OnHandDelta ELSE 0 END) AS AdjustmentQuantity,
    SUM(t.OnHandDelta) AS NetMovementQuantity
FROM dbo.InventoryTransactions t
JOIN dbo.Products p ON p.ProductID = t.ProductID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = t.StorageLocationID
GROUP BY CONVERT(DATE, t.TransactionAt), sl.WarehouseID, t.ProductID, p.ProductCode, p.ProductName;
GO

CREATE OR ALTER VIEW dbo.vw_StocktakeResults
AS
SELECT
    ss.StocktakeSessionID,
    ss.StocktakeNumber,
    ss.WarehouseID,
    ss.PlannedDate,
    ss.Status,
    sl.StorageLocationID,
    sl.LocationCode,
    si.ProductID,
    p.ProductCode,
    p.ProductName,
    si.ProductLotID,
    pl.LotNumber,
    si.BookQuantity,
    si.CountedQuantity,
    si.DifferenceQuantity,
    si.AdjustmentQuantity,
    si.Resolution
FROM dbo.StocktakeSessions ss
JOIN dbo.StocktakeLocations stl ON stl.StocktakeSessionID = ss.StocktakeSessionID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = stl.StorageLocationID
LEFT JOIN dbo.StocktakeItems si
       ON si.StocktakeSessionID = stl.StocktakeSessionID
      AND si.StorageLocationID = stl.StorageLocationID
LEFT JOIN dbo.Products p ON p.ProductID = si.ProductID
LEFT JOIN dbo.ProductLots pl ON pl.ProductLotID = si.ProductLotID;
GO

CREATE OR ALTER VIEW dbo.vw_WarehouseDashboard
AS
SELECT
    w.WarehouseID,
    w.WarehouseCode,
    w.WarehouseName,
    COALESCE(stock.TotalOnHandQuantity, 0) AS TotalOnHandQuantity,
    COALESCE(stock.TotalAvailableQuantity, 0) AS TotalAvailableQuantity,
    COALESCE(stock.ProductCount, 0) AS ProductCountInStock,
    COALESCE(loc.BinCount, 0) AS BinCount,
    COALESCE(loc.OccupiedBinCount, 0) AS OccupiedBinCount,
    (SELECT COUNT(*) FROM dbo.InboundOrders io WHERE io.WarehouseID = w.WarehouseID AND io.Status NOT IN ('COMPLETED','CANCELLED')) AS PendingInboundCount,
    (SELECT COUNT(*) FROM dbo.OutboundOrders oo WHERE oo.WarehouseID = w.WarehouseID AND oo.Status NOT IN ('COMPLETED','CANCELLED')) AS PendingOutboundCount,
    (SELECT COUNT(*) FROM dbo.vw_LowStockAlerts a WHERE a.WarehouseID = w.WarehouseID) AS LowStockAlertCount,
    (SELECT COUNT(*) FROM dbo.vw_ExpiringLotAlerts a WHERE a.WarehouseID = w.WarehouseID) AS ExpiringLotAlertCount
FROM dbo.Warehouses w
OUTER APPLY
(
    SELECT SUM(i.OnHandQuantity) AS TotalOnHandQuantity,
           SUM(i.AvailableQuantity) AS TotalAvailableQuantity,
           COUNT(DISTINCT i.ProductID) AS ProductCount
    FROM dbo.Inventory i
    JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
    WHERE sl.WarehouseID = w.WarehouseID AND i.OnHandQuantity > 0
) stock
OUTER APPLY
(
    SELECT COUNT(*) AS BinCount,
           SUM(CASE WHEN Status = 'OCCUPIED' THEN 1 ELSE 0 END) AS OccupiedBinCount
    FROM dbo.StorageLocations
    WHERE WarehouseID = w.WarehouseID AND LocationType = 'BIN' AND Status <> 'INACTIVE'
) loc;
GO

CREATE OR ALTER VIEW dbo.vw_WarehouseKPI
AS
SELECT
    w.WarehouseID,
    w.WarehouseCode,
    CAST(AVG(CASE WHEN io.Status = 'COMPLETED' THEN DATEDIFF(MINUTE, io.CreatedAt, io.ConfirmedAt) * 1.0 END) / 60.0 AS DECIMAL(18,2)) AS AverageInboundProcessingHours,
    CAST(AVG(CASE WHEN oo.Status = 'COMPLETED' THEN DATEDIFF(MINUTE, oo.CreatedAt, oo.ConfirmedAt) * 1.0 END) / 60.0 AS DECIMAL(18,2)) AS AverageOutboundProcessingHours,
    CAST
    (
        100.0 * SUM(CASE WHEN io.Status = 'COMPLETED' AND (io.DueDate IS NULL OR CONVERT(DATE, io.ConfirmedAt) <= io.DueDate) THEN 1 ELSE 0 END)
        / NULLIF(SUM(CASE WHEN io.Status = 'COMPLETED' THEN 1 ELSE 0 END), 0)
        AS DECIMAL(6,2)
    ) AS InboundOnTimeRate,
    CAST
    (
        100.0 * SUM(CASE WHEN oo.Status = 'COMPLETED' AND (oo.DueDate IS NULL OR CONVERT(DATE, oo.ConfirmedAt) <= oo.DueDate) THEN 1 ELSE 0 END)
        / NULLIF(SUM(CASE WHEN oo.Status = 'COMPLETED' THEN 1 ELSE 0 END), 0)
        AS DECIMAL(6,2)
    ) AS OutboundOnTimeRate
FROM dbo.Warehouses w
LEFT JOIN dbo.InboundOrders io ON io.WarehouseID = w.WarehouseID
LEFT JOIN dbo.OutboundOrders oo ON oo.WarehouseID = w.WarehouseID
GROUP BY w.WarehouseID, w.WarehouseCode;
GO


USE [BMWMS];
GO

*/

USE [BMWMS];
GO

/*=============================================================================
 04. NHA CUNG CAP, KHACH HANG VA SAN PHAM CUNG UNG
=============================================================================*/

IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Suppliers
    (
        SupplierID          BIGINT IDENTITY(1,1) NOT NULL,
        SupplierCode        VARCHAR(50) NOT NULL,
        SupplierName        NVARCHAR(250) NOT NULL,
        PhoneNumber         VARCHAR(20) NOT NULL,
        Email               VARCHAR(255) NULL,
        Address             NVARCHAR(500) NOT NULL,
        RepresentativeName  NVARCHAR(200) NOT NULL,
        TaxCode             VARCHAR(50) NOT NULL,
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_Suppliers_Status DEFAULT ('ACTIVE'),
        CreatedByUserID     BIGINT NOT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedByUserID     BIGINT NULL,
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_Suppliers PRIMARY KEY (SupplierID),
        CONSTRAINT UQ_Suppliers_Code UNIQUE (SupplierCode),
        CONSTRAINT UQ_Suppliers_TaxCode UNIQUE (TaxCode),
        CONSTRAINT CK_Suppliers_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerID          BIGINT IDENTITY(1,1) NOT NULL,
        CustomerCode        VARCHAR(50) NOT NULL,
        CustomerName        NVARCHAR(250) NOT NULL,
        PhoneNumber         VARCHAR(20) NOT NULL,
        Email               VARCHAR(255) NULL,
        Address             NVARCHAR(500) NOT NULL,
        TaxCode             VARCHAR(50) NULL,
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_Customers_Status DEFAULT ('ACTIVE'),
        CreatedByUserID     BIGINT NOT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_Customers PRIMARY KEY (CustomerID),
        CONSTRAINT UQ_Customers_Code UNIQUE (CustomerCode),
        CONSTRAINT CK_Customers_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Customers_TaxCode' AND object_id = OBJECT_ID(N'dbo.Customers'))
    CREATE UNIQUE INDEX UX_Customers_TaxCode ON dbo.Customers(TaxCode) WHERE TaxCode IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.SupplierProducts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierProducts
    (
        SupplierID          BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        SupplierProductCode VARCHAR(100) NULL,
        LastPurchasePrice   DECIMAL(19,4) NULL,
        LeadTimeDays        INT NULL,
        IsPreferred         BIT NOT NULL CONSTRAINT DF_SupplierProducts_Preferred DEFAULT (0),
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_SupplierProducts_Status DEFAULT ('ACTIVE'),
        CONSTRAINT PK_SupplierProducts PRIMARY KEY (SupplierID, ProductID),
        CONSTRAINT FK_SupplierProducts_Supplier FOREIGN KEY (SupplierID) REFERENCES dbo.Suppliers(SupplierID),
        CONSTRAINT CK_SupplierProducts_Price CHECK (LastPurchasePrice IS NULL OR LastPurchasePrice >= 0),
        CONSTRAINT CK_SupplierProducts_LeadTime CHECK (LeadTimeDays IS NULL OR LeadTimeDays >= 0),
        CONSTRAINT CK_SupplierProducts_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
    );
END;
GO

/*=============================================================================
 05. DON MUA HANG VA DON BAN HANG
=============================================================================*/

IF OBJECT_ID(N'dbo.PurchaseOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrders
    (
        PurchaseOrderID     BIGINT IDENTITY(1,1) NOT NULL,
        PurchaseOrderNumber VARCHAR(50) NOT NULL,
        SupplierID          BIGINT NOT NULL,
        OrderDate           DATE NOT NULL,
        ExpectedDeliveryDate DATE NULL,
        Status              VARCHAR(30) NOT NULL CONSTRAINT DF_PurchaseOrders_Status DEFAULT ('DRAFT'),
        Notes               NVARCHAR(2000) NULL,
        CreatedByUserID     BIGINT NOT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_PurchaseOrders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ConfirmedByUserID   BIGINT NULL,
        ConfirmedAt         DATETIME2(0) NULL,
        SupplierEmailSentByUserID BIGINT NULL,
        SupplierEmailSentAt DATETIME2(0) NULL,
        SupplierEmailSentTo NVARCHAR(320) NULL,
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_PurchaseOrders PRIMARY KEY (PurchaseOrderID),
        CONSTRAINT UQ_PurchaseOrders_Number UNIQUE (PurchaseOrderNumber),
        CONSTRAINT FK_PurchaseOrders_Supplier FOREIGN KEY (SupplierID) REFERENCES dbo.Suppliers(SupplierID),
        CONSTRAINT CK_PurchaseOrders_Status CHECK (Status IN ('DRAFT','CONFIRMED','PARTIALLY_RECEIVED','COMPLETED','CANCELLED','CLOSED')),
        CONSTRAINT CK_PurchaseOrders_Dates CHECK (ExpectedDeliveryDate IS NULL OR ExpectedDeliveryDate >= OrderDate)
    );
END;
GO

IF COL_LENGTH(N'dbo.PurchaseOrders', N'SupplierEmailSentByUserID') IS NULL
    ALTER TABLE dbo.PurchaseOrders ADD SupplierEmailSentByUserID BIGINT NULL;
IF COL_LENGTH(N'dbo.PurchaseOrders', N'SupplierEmailSentAt') IS NULL
    ALTER TABLE dbo.PurchaseOrders ADD SupplierEmailSentAt DATETIME2(0) NULL;
IF COL_LENGTH(N'dbo.PurchaseOrders', N'SupplierEmailSentTo') IS NULL
    ALTER TABLE dbo.PurchaseOrders ADD SupplierEmailSentTo NVARCHAR(320) NULL;
GO

IF OBJECT_ID(N'dbo.PurchaseOrderDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrderDetails
    (
        PurchaseOrderDetailID BIGINT IDENTITY(1,1) NOT NULL,
        PurchaseOrderID     BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        OrderedQuantity     DECIMAL(18,4) NOT NULL,
        UnitPrice           DECIMAL(19,4) NULL,
        Notes               NVARCHAR(1000) NULL,
        CONSTRAINT PK_PurchaseOrderDetails PRIMARY KEY (PurchaseOrderDetailID),
        CONSTRAINT UQ_PurchaseOrderDetails_Product UNIQUE (PurchaseOrderID, ProductID),
        CONSTRAINT UQ_PurchaseOrderDetails_IDProduct UNIQUE (PurchaseOrderDetailID, ProductID),
        CONSTRAINT FK_PurchaseOrderDetails_Order FOREIGN KEY (PurchaseOrderID) REFERENCES dbo.PurchaseOrders(PurchaseOrderID),
        CONSTRAINT CK_PurchaseOrderDetails_Quantity CHECK (OrderedQuantity > 0),
        CONSTRAINT CK_PurchaseOrderDetails_Price CHECK (UnitPrice IS NULL OR UnitPrice >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.SalesOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrders
    (
        SalesOrderID        BIGINT IDENTITY(1,1) NOT NULL,
        SalesOrderNumber    VARCHAR(50) NOT NULL,
        CustomerID          BIGINT NOT NULL,
        OrderDate           DATE NOT NULL,
        ExpectedIssueDate   DATE NULL,
        Status              VARCHAR(30) NOT NULL CONSTRAINT DF_SalesOrders_Status DEFAULT ('DRAFT'),
        Notes               NVARCHAR(2000) NULL,
        CreatedByUserID     BIGINT NOT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_SalesOrders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ConfirmedByUserID   BIGINT NULL,
        ConfirmedAt         DATETIME2(0) NULL,
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_SalesOrders PRIMARY KEY (SalesOrderID),
        CONSTRAINT UQ_SalesOrders_Number UNIQUE (SalesOrderNumber),
        CONSTRAINT FK_SalesOrders_Customer FOREIGN KEY (CustomerID) REFERENCES dbo.Customers(CustomerID),
        CONSTRAINT CK_SalesOrders_Status CHECK (Status IN ('DRAFT','CONFIRMED','ALLOCATED','PARTIALLY_FULFILLED','FULFILLED','CANCELLED','CLOSED')),
        CONSTRAINT CK_SalesOrders_Dates CHECK (ExpectedIssueDate IS NULL OR ExpectedIssueDate >= OrderDate)
    );
END;
GO

IF OBJECT_ID(N'dbo.SalesOrderDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrderDetails
    (
        SalesOrderDetailID  BIGINT IDENTITY(1,1) NOT NULL,
        SalesOrderID        BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        OrderedQuantity     DECIMAL(18,4) NOT NULL,
        ReservedQuantity    DECIMAL(18,4) NOT NULL CONSTRAINT DF_SalesOrderDetails_Reserved DEFAULT (0),
        FulfilledQuantity   DECIMAL(18,4) NOT NULL CONSTRAINT DF_SalesOrderDetails_Fulfilled DEFAULT (0),
        UnitPrice           DECIMAL(19,4) NULL,
        Notes               NVARCHAR(1000) NULL,
        CONSTRAINT PK_SalesOrderDetails PRIMARY KEY (SalesOrderDetailID),
        CONSTRAINT UQ_SalesOrderDetails_Product UNIQUE (SalesOrderID, ProductID),
        CONSTRAINT UQ_SalesOrderDetails_IDProduct UNIQUE (SalesOrderDetailID, ProductID),
        CONSTRAINT FK_SalesOrderDetails_Order FOREIGN KEY (SalesOrderID) REFERENCES dbo.SalesOrders(SalesOrderID),
        CONSTRAINT CK_SalesOrderDetails_Quantity CHECK (OrderedQuantity > 0),
        CONSTRAINT CK_SalesOrderDetails_Reserved CHECK (ReservedQuantity >= 0 AND ReservedQuantity <= OrderedQuantity),
        CONSTRAINT CK_SalesOrderDetails_Fulfilled CHECK (FulfilledQuantity >= 0 AND FulfilledQuantity <= OrderedQuantity),
        CONSTRAINT CK_SalesOrderDetails_Price CHECK (UnitPrice IS NULL OR UnitPrice >= 0)
    );
END;
GO

/* Quan he giu ton cho don ban da xac nhan; khong tham chieu InventoryID. */
IF OBJECT_ID(N'dbo.InventoryReservations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryReservations
    (
        InventoryReservationID BIGINT IDENTITY(1,1) NOT NULL,
        SalesOrderDetailID  BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        StorageLocationID   BIGINT NOT NULL,
        ProductLotID        BIGINT NOT NULL,
        ReservedQuantity    DECIMAL(18,4) NOT NULL,
        ConsumedQuantity    DECIMAL(18,4) NOT NULL CONSTRAINT DF_InventoryReservations_Consumed DEFAULT (0),
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_InventoryReservations_Status DEFAULT ('ACTIVE'),
        ReservedByUserID    BIGINT NOT NULL,
        ReservedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_InventoryReservations_At DEFAULT (SYSUTCDATETIME()),
        ReleasedAt          DATETIME2(0) NULL,
        CONSTRAINT PK_InventoryReservations PRIMARY KEY (InventoryReservationID),
        CONSTRAINT CK_InventoryReservations_Quantity CHECK (ReservedQuantity > 0),
        CONSTRAINT CK_InventoryReservations_Consumed CHECK (ConsumedQuantity >= 0 AND ConsumedQuantity <= ReservedQuantity),
        CONSTRAINT CK_InventoryReservations_Status CHECK (Status IN ('ACTIVE','PARTIALLY_CONSUMED','CONSUMED','RELEASED'))
    );
END;
GO

/*=============================================================================
 06. TRANSFER ORDER
=============================================================================*/

IF OBJECT_ID(N'dbo.TransferOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransferOrders
    (
        TransferOrderID     BIGINT IDENTITY(1,1) NOT NULL,
        TransferOrderNumber VARCHAR(50) NOT NULL,
        TransferType        VARCHAR(30) NOT NULL,
        SourceWarehouseID   BIGINT NOT NULL,
        DestinationWarehouseID BIGINT NOT NULL,
        RequestedDate       DATE NOT NULL,
        DueDate             DATE NULL,
        Status              VARCHAR(30) NOT NULL CONSTRAINT DF_TransferOrders_Status DEFAULT ('DRAFT'),
        Notes               NVARCHAR(2000) NULL,
        CreatedByUserID     BIGINT NOT NULL,
        AssignedToUserID    BIGINT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_TransferOrders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ConfirmedByUserID   BIGINT NULL,
        ConfirmedAt         DATETIME2(0) NULL,
        CONSTRAINT PK_TransferOrders PRIMARY KEY (TransferOrderID),
        CONSTRAINT UQ_TransferOrders_Number UNIQUE (TransferOrderNumber),
        CONSTRAINT CK_TransferOrders_Type CHECK (TransferType IN ('INTERNAL_LOCATION','INTER_WAREHOUSE')),
        CONSTRAINT CK_TransferOrders_Status CHECK (Status IN ('DRAFT','ASSIGNED','IN_PROGRESS','COMPLETED','CANCELLED')),
        CONSTRAINT CK_TransferOrders_Warehouse CHECK
        (
            (TransferType = 'INTERNAL_LOCATION' AND SourceWarehouseID = DestinationWarehouseID)
            OR
            (TransferType = 'INTER_WAREHOUSE' AND SourceWarehouseID <> DestinationWarehouseID)
        ),
        CONSTRAINT CK_TransferOrders_DueDate CHECK (DueDate IS NULL OR DueDate >= RequestedDate)
    );
END;
GO

/* TransferOrderDetails la quan he MOVES giua Transfer Order, Product va 2 Location. */
IF OBJECT_ID(N'dbo.TransferOrderDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransferOrderDetails
    (
        TransferOrderDetailID BIGINT IDENTITY(1,1) NOT NULL,
        TransferOrderID     BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        ProductLotID        BIGINT NULL,
        SourceLocationID    BIGINT NULL,
        DestinationLocationID BIGINT NULL,
        RequestedQuantity   DECIMAL(18,4) NOT NULL,
        MovedQuantity       DECIMAL(18,4) NOT NULL CONSTRAINT DF_TransferOrderDetails_Moved DEFAULT (0),
        StaffNote           NVARCHAR(1000) NULL,
        ConfirmedByUserID   BIGINT NULL,
        ConfirmedAt         DATETIME2(0) NULL,
        CONSTRAINT PK_TransferOrderDetails PRIMARY KEY (TransferOrderDetailID),
        CONSTRAINT FK_TransferOrderDetails_Order FOREIGN KEY (TransferOrderID) REFERENCES dbo.TransferOrders(TransferOrderID),
        CONSTRAINT CK_TransferOrderDetails_Requested CHECK (RequestedQuantity > 0),
        CONSTRAINT CK_TransferOrderDetails_Moved CHECK (MovedQuantity >= 0 AND MovedQuantity <= RequestedQuantity),
        CONSTRAINT CK_TransferOrderDetails_Locations CHECK
        (
            SourceLocationID IS NULL OR DestinationLocationID IS NULL OR SourceLocationID <> DestinationLocationID
        )
    );
END;
GO


USE [BMWMS];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

/*=============================================================================
 01. NGUOI DUNG, VAI TRO VA BAO MAT
=============================================================================*/

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleID              INT IDENTITY(1,1) NOT NULL,
        RoleCode            VARCHAR(50) NOT NULL,
        RoleName            NVARCHAR(150) NOT NULL,
        Description         NVARCHAR(500) NULL,
        IsSystemRole        BIT NOT NULL CONSTRAINT DF_Roles_IsSystemRole DEFAULT (1),
        IsActive            BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Roles PRIMARY KEY (RoleID),
        CONSTRAINT UQ_Roles_RoleCode UNIQUE (RoleCode)
    );
END;
GO

IF OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions
    (
        PermissionID       INT IDENTITY(1,1) NOT NULL,
        PermissionCode     VARCHAR(20) NOT NULL,
        PermissionName     NVARCHAR(250) NOT NULL,
        ModuleCode         VARCHAR(10) NOT NULL,
        IsActive           BIT NOT NULL CONSTRAINT DF_Permissions_IsActive DEFAULT (1),
        CONSTRAINT PK_Permissions PRIMARY KEY (PermissionID),
        CONSTRAINT UQ_Permissions_PermissionCode UNIQUE (PermissionCode)
    );
END;
GO

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        RoleID             INT NOT NULL,
        PermissionID       INT NOT NULL,
        GrantedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_RolePermissions_GrantedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleID, PermissionID),
        CONSTRAINT FK_RolePermissions_Role FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID),
        CONSTRAINT FK_RolePermissions_Permission FOREIGN KEY (PermissionID) REFERENCES dbo.Permissions(PermissionID)
    );
END;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserID              BIGINT IDENTITY(1,1) NOT NULL,
        RoleID              INT NOT NULL,
        Username            VARCHAR(100) NOT NULL,
        Email               VARCHAR(255) NOT NULL,
        PasswordHash        NVARCHAR(500) NOT NULL,
        FullName            NVARCHAR(200) NOT NULL,
        PhoneNumber         VARCHAR(20) NULL,
        AvatarUrl           NVARCHAR(1000) NULL,
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_Users_Status DEFAULT ('ACTIVE'),
        FailedLoginCount    INT NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT (0),
        LockedUntil         DATETIME2(0) NULL,
        LastLoginAt         DATETIME2(0) NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_Users PRIMARY KEY (UserID),
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT FK_Users_Role FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID),
        CONSTRAINT CK_Users_Status CHECK (Status IN ('ACTIVE','LOCKED','INACTIVE')),
        CONSTRAINT CK_Users_FailedLoginCount CHECK (FailedLoginCount >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.UserSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSessions
    (
        SessionID           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_UserSessions_ID DEFAULT (NEWSEQUENTIALID()),
        UserID              BIGINT NOT NULL,
        RefreshTokenHash    VARBINARY(64) NOT NULL,
        IpAddress           VARCHAR(45) NULL,
        UserAgent           NVARCHAR(1000) NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_UserSessions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ExpiresAt           DATETIME2(0) NOT NULL,
        RevokedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_UserSessions PRIMARY KEY (SessionID),
        CONSTRAINT FK_UserSessions_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT UQ_UserSessions_TokenHash UNIQUE (RefreshTokenHash),
        CONSTRAINT CK_UserSessions_Expiry CHECK (ExpiresAt > CreatedAt)
    );
END;
GO

IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetTokens
    (
        PasswordResetTokenID BIGINT IDENTITY(1,1) NOT NULL,
        UserID              BIGINT NOT NULL,
        TokenHash           VARBINARY(64) NOT NULL,
        RequestedAt         DATETIME2(0) NOT NULL CONSTRAINT DF_PasswordResetTokens_RequestedAt DEFAULT (SYSUTCDATETIME()),
        ExpiresAt           DATETIME2(0) NOT NULL,
        UsedAt              DATETIME2(0) NULL,
        CONSTRAINT PK_PasswordResetTokens PRIMARY KEY (PasswordResetTokenID),
        CONSTRAINT FK_PasswordResetTokens_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT UQ_PasswordResetTokens_TokenHash UNIQUE (TokenHash),
        CONSTRAINT CK_PasswordResetTokens_Expiry CHECK (ExpiresAt > RequestedAt)
    );
END;
GO

/*=============================================================================
 02. KHO, KHU VUC, KE VA VI TRI LUU TRU CO DINH
=============================================================================*/

IF OBJECT_ID(N'dbo.Warehouses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Warehouses
    (
        WarehouseID         BIGINT IDENTITY(1,1) NOT NULL,
        WarehouseCode       VARCHAR(50) NOT NULL,
        WarehouseName       NVARCHAR(200) NOT NULL,
        Address             NVARCHAR(500) NOT NULL,
        PhoneNumber         VARCHAR(20) NULL,
        IsPrimary           BIT NOT NULL CONSTRAINT DF_Warehouses_IsPrimary DEFAULT (0),
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_Warehouses_Status DEFAULT ('ACTIVE'),
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Warehouses_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_Warehouses PRIMARY KEY (WarehouseID),
        CONSTRAINT UQ_Warehouses_Code UNIQUE (WarehouseCode),
        CONSTRAINT CK_Warehouses_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Warehouses_Primary' AND object_id = OBJECT_ID(N'dbo.Warehouses'))
    CREATE UNIQUE INDEX UX_Warehouses_Primary ON dbo.Warehouses(IsPrimary) WHERE IsPrimary = 1;
GO

IF OBJECT_ID(N'dbo.WarehouseZones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarehouseZones
    (
        ZoneID              BIGINT IDENTITY(1,1) NOT NULL,
        WarehouseID         BIGINT NOT NULL,
        ZoneCode            VARCHAR(50) NOT NULL,
        ZoneName            NVARCHAR(200) NOT NULL,
        Description         NVARCHAR(500) NULL,
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_WarehouseZones_Status DEFAULT ('ACTIVE'),
        CONSTRAINT PK_WarehouseZones PRIMARY KEY (ZoneID),
        CONSTRAINT UQ_WarehouseZones_Code UNIQUE (WarehouseID, ZoneCode),
        CONSTRAINT UQ_WarehouseZones_ZoneWarehouse UNIQUE (ZoneID, WarehouseID),
        CONSTRAINT FK_WarehouseZones_Warehouse FOREIGN KEY (WarehouseID) REFERENCES dbo.Warehouses(WarehouseID),
        CONSTRAINT CK_WarehouseZones_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
    );
END;
GO

IF OBJECT_ID(N'dbo.StorageRacks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StorageRacks
    (
        RackID              BIGINT IDENTITY(1,1) NOT NULL,
        WarehouseID         BIGINT NOT NULL,
        ZoneID              BIGINT NOT NULL,
        RackCode            VARCHAR(50) NOT NULL,
        RackName            NVARCHAR(200) NOT NULL,
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_StorageRacks_Status DEFAULT ('ACTIVE'),
        CONSTRAINT PK_StorageRacks PRIMARY KEY (RackID),
        CONSTRAINT UQ_StorageRacks_Code UNIQUE (WarehouseID, RackCode),
        CONSTRAINT UQ_StorageRacks_RackWarehouse UNIQUE (RackID, WarehouseID),
        CONSTRAINT FK_StorageRacks_ZoneWarehouse FOREIGN KEY (ZoneID, WarehouseID)
            REFERENCES dbo.WarehouseZones(ZoneID, WarehouseID),
        CONSTRAINT CK_StorageRacks_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
    );
END;
GO

IF OBJECT_ID(N'dbo.StorageLocations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StorageLocations
    (
        StorageLocationID   BIGINT IDENTITY(1,1) NOT NULL,
        WarehouseID         BIGINT NOT NULL,
        RackID              BIGINT NULL,
        LocationCode        VARCHAR(80) NOT NULL,
        LocationName        NVARCHAR(200) NULL,
        LocationType        VARCHAR(20) NOT NULL CONSTRAINT DF_StorageLocations_Type DEFAULT ('BIN'),
        AreaSquareMeter     DECIMAL(18,2) NULL,
        MaxWeightKg         DECIMAL(18,4) NULL,
        MaxVolumeM3         DECIMAL(18,4) NULL,
        IsPutawayAllowed    BIT NOT NULL CONSTRAINT DF_StorageLocations_Putaway DEFAULT (1),
        IsPickable          BIT NOT NULL CONSTRAINT DF_StorageLocations_Pickable DEFAULT (1),
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_StorageLocations_Status DEFAULT ('AVAILABLE'),
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_StorageLocations_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_StorageLocations PRIMARY KEY (StorageLocationID),
        CONSTRAINT UQ_StorageLocations_Code UNIQUE (WarehouseID, LocationCode),
        CONSTRAINT UQ_StorageLocations_LocationWarehouse UNIQUE (StorageLocationID, WarehouseID),
        CONSTRAINT FK_StorageLocations_Warehouse FOREIGN KEY (WarehouseID) REFERENCES dbo.Warehouses(WarehouseID),
        CONSTRAINT FK_StorageLocations_RackWarehouse FOREIGN KEY (RackID, WarehouseID)
            REFERENCES dbo.StorageRacks(RackID, WarehouseID),
        CONSTRAINT CK_StorageLocations_Type CHECK (LocationType IN ('BIN','RECEIVING','STAGING','QUARANTINE','DISPATCH')),
        CONSTRAINT CK_StorageLocations_Status CHECK (Status IN ('AVAILABLE','OCCUPIED','BLOCKED','INACTIVE')),
        CONSTRAINT CK_StorageLocations_Area CHECK (AreaSquareMeter IS NULL OR AreaSquareMeter > 0),
        CONSTRAINT CK_StorageLocations_MaxWeight CHECK (MaxWeightKg IS NULL OR MaxWeightKg > 0),
        CONSTRAINT CK_StorageLocations_MaxVolume CHECK (MaxVolumeM3 IS NULL OR MaxVolumeM3 > 0)
    );
END;
GO

/*=============================================================================
 03. SAN PHAM, NHOM SAN PHAM VA THUOC TINH DONG
=============================================================================*/

IF OBJECT_ID(N'dbo.UnitsOfMeasure', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UnitsOfMeasure
    (
        UnitOfMeasureID     INT IDENTITY(1,1) NOT NULL,
        UnitCode            VARCHAR(30) NOT NULL,
        UnitName            NVARCHAR(100) NOT NULL,
        QuantityScale       TINYINT NOT NULL CONSTRAINT DF_UnitsOfMeasure_QuantityScale DEFAULT (0),
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_UnitsOfMeasure_Status DEFAULT ('ACTIVE'),
        CONSTRAINT PK_UnitsOfMeasure PRIMARY KEY (UnitOfMeasureID),
        CONSTRAINT UQ_UnitsOfMeasure_Code UNIQUE (UnitCode),
        CONSTRAINT CK_UnitsOfMeasure_Status CHECK (Status IN ('ACTIVE','INACTIVE')),
        CONSTRAINT CK_UnitsOfMeasure_QuantityScale CHECK (QuantityScale BETWEEN 0 AND 4)
    );
END;
GO

IF COL_LENGTH(N'dbo.UnitsOfMeasure', N'QuantityScale') IS NULL
BEGIN
    ALTER TABLE dbo.UnitsOfMeasure
        ADD QuantityScale TINYINT NOT NULL
            CONSTRAINT DF_UnitsOfMeasure_QuantityScale DEFAULT (0) WITH VALUES;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_UnitsOfMeasure_QuantityScale')
    EXEC(N'ALTER TABLE dbo.UnitsOfMeasure ADD CONSTRAINT CK_UnitsOfMeasure_QuantityScale
        CHECK (QuantityScale BETWEEN 0 AND 4);');
GO

IF OBJECT_ID(N'dbo.ProductGroups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductGroups
    (
        ProductGroupID      BIGINT IDENTITY(1,1) NOT NULL,
        ParentGroupID       BIGINT NULL,
        GroupCode           VARCHAR(50) NOT NULL,
        GroupName           NVARCHAR(200) NOT NULL,
        Description         NVARCHAR(1000) NULL,
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_ProductGroups_Status DEFAULT ('ACTIVE'),
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_ProductGroups_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_ProductGroups PRIMARY KEY (ProductGroupID),
        CONSTRAINT UQ_ProductGroups_Code UNIQUE (GroupCode),
        CONSTRAINT FK_ProductGroups_Parent FOREIGN KEY (ParentGroupID) REFERENCES dbo.ProductGroups(ProductGroupID),
        CONSTRAINT CK_ProductGroups_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductAttributes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductAttributes
    (
        ProductAttributeID  BIGINT IDENTITY(1,1) NOT NULL,
        AttributeCode       VARCHAR(50) NOT NULL,
        AttributeName       NVARCHAR(200) NOT NULL,
        DataType            VARCHAR(20) NOT NULL,
        UnitLabel           NVARCHAR(50) NULL,
        Description         NVARCHAR(500) NULL,
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_ProductAttributes_Status DEFAULT ('ACTIVE'),
        CONSTRAINT PK_ProductAttributes PRIMARY KEY (ProductAttributeID),
        CONSTRAINT UQ_ProductAttributes_Code UNIQUE (AttributeCode),
        CONSTRAINT CK_ProductAttributes_DataType CHECK (DataType IN ('TEXT','NUMBER','DATE','BOOLEAN','OPTION')),
        CONSTRAINT CK_ProductAttributes_Status CHECK (Status IN ('ACTIVE','INACTIVE'))
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductAttributeOptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductAttributeOptions
    (
        ProductAttributeOptionID BIGINT IDENTITY(1,1) NOT NULL,
        ProductAttributeID  BIGINT NOT NULL,
        OptionCode          VARCHAR(50) NOT NULL,
        OptionValue         NVARCHAR(200) NOT NULL,
        DisplayOrder        INT NOT NULL CONSTRAINT DF_ProductAttributeOptions_Order DEFAULT (0),
        IsActive            BIT NOT NULL CONSTRAINT DF_ProductAttributeOptions_Active DEFAULT (1),
        CONSTRAINT PK_ProductAttributeOptions PRIMARY KEY (ProductAttributeOptionID),
        CONSTRAINT UQ_ProductAttributeOptions_Code UNIQUE (ProductAttributeID, OptionCode),
        CONSTRAINT FK_ProductAttributeOptions_Attribute FOREIGN KEY (ProductAttributeID)
            REFERENCES dbo.ProductAttributes(ProductAttributeID),
        CONSTRAINT CK_ProductAttributeOptions_Order CHECK (DisplayOrder >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductGroupAttributes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductGroupAttributes
    (
        ProductGroupID      BIGINT NOT NULL,
        ProductAttributeID  BIGINT NOT NULL,
        IsRequired          BIT NOT NULL CONSTRAINT DF_ProductGroupAttributes_Required DEFAULT (0),
        DisplayOrder        INT NOT NULL CONSTRAINT DF_ProductGroupAttributes_Order DEFAULT (0),
        DefaultValue        NVARCHAR(1000) NULL,
        CONSTRAINT PK_ProductGroupAttributes PRIMARY KEY (ProductGroupID, ProductAttributeID),
        CONSTRAINT FK_ProductGroupAttributes_Group FOREIGN KEY (ProductGroupID) REFERENCES dbo.ProductGroups(ProductGroupID),
        CONSTRAINT FK_ProductGroupAttributes_Attribute FOREIGN KEY (ProductAttributeID) REFERENCES dbo.ProductAttributes(ProductAttributeID),
        CONSTRAINT CK_ProductGroupAttributes_Order CHECK (DisplayOrder >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductID           BIGINT IDENTITY(1,1) NOT NULL,
        ProductGroupID      BIGINT NOT NULL,
        UnitOfMeasureID     INT NOT NULL,
        ProductCode         VARCHAR(80) NOT NULL,
        ProductName         NVARCHAR(250) NOT NULL,
        Barcode             VARCHAR(100) NULL,
        Description         NVARCHAR(2000) NULL,
        RotationMethod      VARCHAR(10) NOT NULL CONSTRAINT DF_Products_Rotation DEFAULT ('FIFO'),
        TrackLot            BIT NOT NULL CONSTRAINT DF_Products_TrackLot DEFAULT (1),
        TrackExpiry         BIT NOT NULL CONSTRAINT DF_Products_TrackExpiry DEFAULT (0),
        DefaultShelfLifeDays INT NULL,
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_Products_Status DEFAULT ('ACTIVE'),
        CreatedByUserID     BIGINT NOT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedByUserID     BIGINT NULL,
        UpdatedAt           DATETIME2(0) NULL,
        CONSTRAINT PK_Products PRIMARY KEY (ProductID),
        CONSTRAINT UQ_Products_Code UNIQUE (ProductCode),
        CONSTRAINT FK_Products_Group FOREIGN KEY (ProductGroupID) REFERENCES dbo.ProductGroups(ProductGroupID),
        CONSTRAINT FK_Products_Unit FOREIGN KEY (UnitOfMeasureID) REFERENCES dbo.UnitsOfMeasure(UnitOfMeasureID),
        CONSTRAINT FK_Products_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_Products_UpdatedBy FOREIGN KEY (UpdatedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_Products_Rotation CHECK (RotationMethod IN ('FIFO','FEFO')),
        CONSTRAINT CK_Products_Status CHECK (Status IN ('ACTIVE','INACTIVE')),
        CONSTRAINT CK_Products_ShelfLife CHECK (DefaultShelfLifeDays IS NULL OR DefaultShelfLifeDays > 0),
        CONSTRAINT CK_Products_ExpiryTracking CHECK (TrackExpiry = 0 OR TrackLot = 1)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductAttributeValues', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductAttributeValues
    (
        ProductID           BIGINT NOT NULL,
        ProductAttributeID  BIGINT NOT NULL,
        AttributeValue      NVARCHAR(1000) NOT NULL,
        UpdatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_ProductAttributeValues_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ProductAttributeValues PRIMARY KEY (ProductID, ProductAttributeID),
        CONSTRAINT FK_ProductAttributeValues_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID),
        CONSTRAINT FK_ProductAttributeValues_Attribute FOREIGN KEY (ProductAttributeID) REFERENCES dbo.ProductAttributes(ProductAttributeID)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductLots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductLots
    (
        ProductLotID        BIGINT IDENTITY(1,1) NOT NULL,
        ProductID           BIGINT NOT NULL,
        LotNumber           VARCHAR(100) NOT NULL,
        ManufactureDate     DATE NULL,
        ExpiryDate          DATE NULL,
        FirstReceivedDate   DATE NOT NULL CONSTRAINT DF_ProductLots_ReceivedDate DEFAULT (CONVERT(DATE, SYSUTCDATETIME())),
        Status              VARCHAR(20) NOT NULL CONSTRAINT DF_ProductLots_Status DEFAULT ('AVAILABLE'),
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_ProductLots_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ProductLots PRIMARY KEY (ProductLotID),
        CONSTRAINT UQ_ProductLots_ProductLot UNIQUE (ProductID, LotNumber),
        CONSTRAINT UQ_ProductLots_LotProduct UNIQUE (ProductLotID, ProductID),
        CONSTRAINT FK_ProductLots_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID),
        CONSTRAINT CK_ProductLots_Status CHECK (Status IN ('AVAILABLE','QUARANTINED','EXPIRED','CLOSED')),
        CONSTRAINT CK_ProductLots_Dates CHECK (ExpiryDate IS NULL OR ManufactureDate IS NULL OR ExpiryDate >= ManufactureDate)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductFixedLocations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductFixedLocations
    (
        ProductID           BIGINT NOT NULL,
        StorageLocationID   BIGINT NOT NULL,
        Priority            INT NOT NULL CONSTRAINT DF_ProductFixedLocations_Priority DEFAULT (1),
        IsDefault           BIT NOT NULL CONSTRAINT DF_ProductFixedLocations_Default DEFAULT (0),
        IsActive            BIT NOT NULL CONSTRAINT DF_ProductFixedLocations_Active DEFAULT (1),
        CONSTRAINT PK_ProductFixedLocations PRIMARY KEY (ProductID, StorageLocationID),
        CONSTRAINT FK_ProductFixedLocations_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID),
        CONSTRAINT FK_ProductFixedLocations_Location FOREIGN KEY (StorageLocationID) REFERENCES dbo.StorageLocations(StorageLocationID),
        CONSTRAINT CK_ProductFixedLocations_Priority CHECK (Priority > 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.ProductWarehousePolicies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductWarehousePolicies
    (
        ProductID           BIGINT NOT NULL,
        WarehouseID         BIGINT NOT NULL,
        MinimumStockQuantity DECIMAL(18,4) NOT NULL CONSTRAINT DF_ProductWarehousePolicies_Min DEFAULT (0),
        ExpiryWarningDays   INT NOT NULL CONSTRAINT DF_ProductWarehousePolicies_Expiry DEFAULT (30),
        CONSTRAINT PK_ProductWarehousePolicies PRIMARY KEY (ProductID, WarehouseID),
        CONSTRAINT FK_ProductWarehousePolicies_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID),
        CONSTRAINT FK_ProductWarehousePolicies_Warehouse FOREIGN KEY (WarehouseID) REFERENCES dbo.Warehouses(WarehouseID),
        CONSTRAINT CK_ProductWarehousePolicies_Min CHECK (MinimumStockQuantity >= 0),
        CONSTRAINT CK_ProductWarehousePolicies_Expiry CHECK (ExpiryWarningDays >= 0)
    );
END;
GO

/* Cac FK duoc bo sung sau khi toan bo bang master da ton tai. */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Suppliers_CreatedBy')
    ALTER TABLE dbo.Suppliers ADD CONSTRAINT FK_Suppliers_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Suppliers_UpdatedBy')
    ALTER TABLE dbo.Suppliers ADD CONSTRAINT FK_Suppliers_UpdatedBy FOREIGN KEY (UpdatedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Customers_CreatedBy')
    ALTER TABLE dbo.Customers ADD CONSTRAINT FK_Customers_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SupplierProducts_Product')
    ALTER TABLE dbo.SupplierProducts ADD CONSTRAINT FK_SupplierProducts_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrders_CreatedBy')
    ALTER TABLE dbo.PurchaseOrders ADD CONSTRAINT FK_PurchaseOrders_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrders_ConfirmedBy')
    ALTER TABLE dbo.PurchaseOrders ADD CONSTRAINT FK_PurchaseOrders_ConfirmedBy FOREIGN KEY (ConfirmedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrders_SupplierEmailSentBy')
    ALTER TABLE dbo.PurchaseOrders ADD CONSTRAINT FK_PurchaseOrders_SupplierEmailSentBy FOREIGN KEY (SupplierEmailSentByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrderDetails_Product')
    ALTER TABLE dbo.PurchaseOrderDetails ADD CONSTRAINT FK_PurchaseOrderDetails_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalesOrders_CreatedBy')
    ALTER TABLE dbo.SalesOrders ADD CONSTRAINT FK_SalesOrders_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalesOrders_ConfirmedBy')
    ALTER TABLE dbo.SalesOrders ADD CONSTRAINT FK_SalesOrders_ConfirmedBy FOREIGN KEY (ConfirmedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalesOrderDetails_Product')
    ALTER TABLE dbo.SalesOrderDetails ADD CONSTRAINT FK_SalesOrderDetails_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventoryReservations_DetailProduct')
    ALTER TABLE dbo.InventoryReservations ADD CONSTRAINT FK_InventoryReservations_DetailProduct FOREIGN KEY (SalesOrderDetailID, ProductID) REFERENCES dbo.SalesOrderDetails(SalesOrderDetailID, ProductID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventoryReservations_Location')
    ALTER TABLE dbo.InventoryReservations ADD CONSTRAINT FK_InventoryReservations_Location FOREIGN KEY (StorageLocationID) REFERENCES dbo.StorageLocations(StorageLocationID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventoryReservations_LotProduct')
    ALTER TABLE dbo.InventoryReservations ADD CONSTRAINT FK_InventoryReservations_LotProduct FOREIGN KEY (ProductLotID, ProductID) REFERENCES dbo.ProductLots(ProductLotID, ProductID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventoryReservations_User')
    ALTER TABLE dbo.InventoryReservations ADD CONSTRAINT FK_InventoryReservations_User FOREIGN KEY (ReservedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrders_SourceWarehouse')
    ALTER TABLE dbo.TransferOrders ADD CONSTRAINT FK_TransferOrders_SourceWarehouse FOREIGN KEY (SourceWarehouseID) REFERENCES dbo.Warehouses(WarehouseID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrders_DestinationWarehouse')
    ALTER TABLE dbo.TransferOrders ADD CONSTRAINT FK_TransferOrders_DestinationWarehouse FOREIGN KEY (DestinationWarehouseID) REFERENCES dbo.Warehouses(WarehouseID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrders_CreatedBy')
    ALTER TABLE dbo.TransferOrders ADD CONSTRAINT FK_TransferOrders_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrders_AssignedTo')
    ALTER TABLE dbo.TransferOrders ADD CONSTRAINT FK_TransferOrders_AssignedTo FOREIGN KEY (AssignedToUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrders_ConfirmedBy')
    ALTER TABLE dbo.TransferOrders ADD CONSTRAINT FK_TransferOrders_ConfirmedBy FOREIGN KEY (ConfirmedByUserID) REFERENCES dbo.Users(UserID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrderDetails_Product')
    ALTER TABLE dbo.TransferOrderDetails ADD CONSTRAINT FK_TransferOrderDetails_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrderDetails_LotProduct')
    ALTER TABLE dbo.TransferOrderDetails ADD CONSTRAINT FK_TransferOrderDetails_LotProduct FOREIGN KEY (ProductLotID, ProductID) REFERENCES dbo.ProductLots(ProductLotID, ProductID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrderDetails_Source')
    ALTER TABLE dbo.TransferOrderDetails ADD CONSTRAINT FK_TransferOrderDetails_Source FOREIGN KEY (SourceLocationID) REFERENCES dbo.StorageLocations(StorageLocationID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrderDetails_Destination')
    ALTER TABLE dbo.TransferOrderDetails ADD CONSTRAINT FK_TransferOrderDetails_Destination FOREIGN KEY (DestinationLocationID) REFERENCES dbo.StorageLocations(StorageLocationID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TransferOrderDetails_ConfirmedBy')
    ALTER TABLE dbo.TransferOrderDetails ADD CONSTRAINT FK_TransferOrderDetails_ConfirmedBy FOREIGN KEY (ConfirmedByUserID) REFERENCES dbo.Users(UserID);
GO

/*=============================================================================
 07. INBOUND ORDER - KHONG CO GOODS RECEIPT/INVOICE
=============================================================================*/

IF OBJECT_ID(N'dbo.InboundOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InboundOrders
    (
        InboundOrderID      BIGINT IDENTITY(1,1) NOT NULL,
        InboundOrderNumber  VARCHAR(50) NOT NULL,
        WarehouseID         BIGINT NOT NULL,
        SourceType          VARCHAR(30) NOT NULL,
        PurchaseOrderID     BIGINT NULL,
        SalesOrderID        BIGINT NULL,
        TransferOrderID     BIGINT NULL,
        ParentInboundOrderID BIGINT NULL,
        ExpectedReceiptDate DATE NOT NULL,
        DueDate             DATE NULL,
        Status              VARCHAR(30) NOT NULL CONSTRAINT DF_InboundOrders_Status DEFAULT ('DRAFT'),
        Notes               NVARCHAR(2000) NULL,
        CreatedByUserID     BIGINT NOT NULL,
        AssignedToUserID    BIGINT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_InboundOrders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ConfirmedByUserID   BIGINT NULL,
        ConfirmedAt         DATETIME2(0) NULL,
        CancelledByUserID   BIGINT NULL,
        CancelledAt         DATETIME2(0) NULL,
        CancellationReason  NVARCHAR(1000) NULL,
        CONSTRAINT PK_InboundOrders PRIMARY KEY (InboundOrderID),
        CONSTRAINT UQ_InboundOrders_Number UNIQUE (InboundOrderNumber),
        CONSTRAINT FK_InboundOrders_Warehouse FOREIGN KEY (WarehouseID) REFERENCES dbo.Warehouses(WarehouseID),
        CONSTRAINT FK_InboundOrders_PurchaseOrder FOREIGN KEY (PurchaseOrderID) REFERENCES dbo.PurchaseOrders(PurchaseOrderID),
        CONSTRAINT FK_InboundOrders_SalesOrder FOREIGN KEY (SalesOrderID) REFERENCES dbo.SalesOrders(SalesOrderID),
        CONSTRAINT FK_InboundOrders_TransferOrder FOREIGN KEY (TransferOrderID) REFERENCES dbo.TransferOrders(TransferOrderID),
        CONSTRAINT FK_InboundOrders_Parent FOREIGN KEY (ParentInboundOrderID) REFERENCES dbo.InboundOrders(InboundOrderID),
        CONSTRAINT FK_InboundOrders_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_InboundOrders_AssignedTo FOREIGN KEY (AssignedToUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_InboundOrders_ConfirmedBy FOREIGN KEY (ConfirmedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_InboundOrders_CancelledBy FOREIGN KEY (CancelledByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_InboundOrders_SourceType CHECK (SourceType IN ('PURCHASE_ORDER','SALES_RETURN','TRANSFER_ORDER')),
        CONSTRAINT CK_InboundOrders_Status CHECK (Status IN ('DRAFT','ASSIGNED','IN_PROGRESS','COMPLETED','CANCELLED')),
        CONSTRAINT CK_InboundOrders_SourceReference CHECK
        (
            (SourceType = 'PURCHASE_ORDER' AND PurchaseOrderID IS NOT NULL AND SalesOrderID IS NULL AND TransferOrderID IS NULL)
            OR
            (SourceType = 'SALES_RETURN' AND PurchaseOrderID IS NULL AND SalesOrderID IS NOT NULL AND TransferOrderID IS NULL)
            OR
            (SourceType = 'TRANSFER_ORDER' AND PurchaseOrderID IS NULL AND SalesOrderID IS NULL AND TransferOrderID IS NOT NULL)
        ),
        CONSTRAINT CK_InboundOrders_DueDate CHECK (DueDate IS NULL OR DueDate >= ExpectedReceiptDate),
        CONSTRAINT CK_InboundOrders_Parent CHECK (ParentInboundOrderID IS NULL OR ParentInboundOrderID <> InboundOrderID)
    );
END;
GO

IF OBJECT_ID(N'dbo.InboundOrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InboundOrderItems
    (
        InboundOrderItemID  BIGINT IDENTITY(1,1) NOT NULL,
        InboundOrderID      BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        ExpectedQuantity    DECIMAL(18,4) NOT NULL,
        ReceivedQuantity    DECIMAL(18,4) NOT NULL CONSTRAINT DF_InboundOrderItems_Received DEFAULT (0),
        DamagedQuantity     DECIMAL(18,4) NOT NULL CONSTRAINT DF_InboundOrderItems_Damaged DEFAULT (0),
        ShortageQuantity    DECIMAL(18,4) NOT NULL CONSTRAINT DF_InboundOrderItems_Shortage DEFAULT (0),
        Notes               NVARCHAR(1000) NULL,
        CONSTRAINT PK_InboundOrderItems PRIMARY KEY (InboundOrderItemID),
        CONSTRAINT UQ_InboundOrderItems_Product UNIQUE (InboundOrderID, ProductID),
        CONSTRAINT UQ_InboundOrderItems_Composite UNIQUE (InboundOrderItemID, InboundOrderID, ProductID),
        CONSTRAINT FK_InboundOrderItems_Order FOREIGN KEY (InboundOrderID) REFERENCES dbo.InboundOrders(InboundOrderID),
        CONSTRAINT FK_InboundOrderItems_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID),
        CONSTRAINT CK_InboundOrderItems_Expected CHECK (ExpectedQuantity > 0),
        CONSTRAINT CK_InboundOrderItems_Received CHECK (ReceivedQuantity >= 0),
        CONSTRAINT CK_InboundOrderItems_Damaged CHECK (DamagedQuantity >= 0 AND DamagedQuantity <= ReceivedQuantity),
        CONSTRAINT CK_InboundOrderItems_Shortage CHECK (ShortageQuantity >= 0)
    );
END;
GO

/* Quan he 3 ben: Inbound Order - Product - Storage Location (+ Product Lot). */
IF OBJECT_ID(N'dbo.InboundOrderDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InboundOrderDetails
    (
        InboundOrderDetailID BIGINT IDENTITY(1,1) NOT NULL,
        InboundOrderItemID  BIGINT NOT NULL,
        InboundOrderID      BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        StorageLocationID   BIGINT NOT NULL,
        ProductLotID        BIGINT NOT NULL,
        ReceivedQuantity    DECIMAL(18,4) NOT NULL,
        ConditionStatus     VARCHAR(20) NOT NULL CONSTRAINT DF_InboundOrderDetails_Condition DEFAULT ('GOOD'),
        RecordedByUserID    BIGINT NOT NULL,
        RecordedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_InboundOrderDetails_RecordedAt DEFAULT (SYSUTCDATETIME()),
        Notes               NVARCHAR(1000) NULL,
        CONSTRAINT PK_InboundOrderDetails PRIMARY KEY (InboundOrderDetailID),
        CONSTRAINT FK_InboundOrderDetails_Item FOREIGN KEY (InboundOrderItemID, InboundOrderID, ProductID)
            REFERENCES dbo.InboundOrderItems(InboundOrderItemID, InboundOrderID, ProductID),
        CONSTRAINT FK_InboundOrderDetails_Location FOREIGN KEY (StorageLocationID) REFERENCES dbo.StorageLocations(StorageLocationID),
        CONSTRAINT FK_InboundOrderDetails_LotProduct FOREIGN KEY (ProductLotID, ProductID) REFERENCES dbo.ProductLots(ProductLotID, ProductID),
        CONSTRAINT FK_InboundOrderDetails_User FOREIGN KEY (RecordedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_InboundOrderDetails_Quantity CHECK (ReceivedQuantity > 0),
        CONSTRAINT CK_InboundOrderDetails_Condition CHECK (ConditionStatus IN ('GOOD','DAMAGED','QUARANTINED'))
    );
END;
GO

/*=============================================================================
 08. OUTBOUND ORDER
=============================================================================*/

IF OBJECT_ID(N'dbo.OutboundOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboundOrders
    (
        OutboundOrderID     BIGINT IDENTITY(1,1) NOT NULL,
        OutboundOrderNumber VARCHAR(50) NOT NULL,
        WarehouseID         BIGINT NOT NULL,
        SourceType          VARCHAR(30) NOT NULL,
        SalesOrderID        BIGINT NULL,
        PurchaseOrderID     BIGINT NULL,
        TransferOrderID     BIGINT NULL,
        ExpectedIssueDate   DATE NOT NULL,
        DueDate             DATE NULL,
        Status              VARCHAR(30) NOT NULL CONSTRAINT DF_OutboundOrders_Status DEFAULT ('DRAFT'),
        Notes               NVARCHAR(2000) NULL,
        CreatedByUserID     BIGINT NOT NULL,
        AssignedToUserID    BIGINT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_OutboundOrders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ConfirmedByUserID   BIGINT NULL,
        ConfirmedAt         DATETIME2(0) NULL,
        CancelledByUserID   BIGINT NULL,
        CancelledAt         DATETIME2(0) NULL,
        CancellationReason  NVARCHAR(1000) NULL,
        CONSTRAINT PK_OutboundOrders PRIMARY KEY (OutboundOrderID),
        CONSTRAINT UQ_OutboundOrders_Number UNIQUE (OutboundOrderNumber),
        CONSTRAINT FK_OutboundOrders_Warehouse FOREIGN KEY (WarehouseID) REFERENCES dbo.Warehouses(WarehouseID),
        CONSTRAINT FK_OutboundOrders_SalesOrder FOREIGN KEY (SalesOrderID) REFERENCES dbo.SalesOrders(SalesOrderID),
        CONSTRAINT FK_OutboundOrders_PurchaseOrder FOREIGN KEY (PurchaseOrderID) REFERENCES dbo.PurchaseOrders(PurchaseOrderID),
        CONSTRAINT FK_OutboundOrders_TransferOrder FOREIGN KEY (TransferOrderID) REFERENCES dbo.TransferOrders(TransferOrderID),
        CONSTRAINT FK_OutboundOrders_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_OutboundOrders_AssignedTo FOREIGN KEY (AssignedToUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_OutboundOrders_ConfirmedBy FOREIGN KEY (ConfirmedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_OutboundOrders_CancelledBy FOREIGN KEY (CancelledByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_OutboundOrders_SourceType CHECK (SourceType IN ('SALES_ORDER','PURCHASE_RETURN','TRANSFER_ORDER')),
        CONSTRAINT CK_OutboundOrders_Status CHECK (Status IN ('DRAFT','ASSIGNED','IN_PROGRESS','COMPLETED','CANCELLED')),
        CONSTRAINT CK_OutboundOrders_SourceReference CHECK
        (
            (SourceType = 'SALES_ORDER' AND SalesOrderID IS NOT NULL AND PurchaseOrderID IS NULL AND TransferOrderID IS NULL)
            OR
            (SourceType = 'PURCHASE_RETURN' AND SalesOrderID IS NULL AND PurchaseOrderID IS NOT NULL AND TransferOrderID IS NULL)
            OR
            (SourceType = 'TRANSFER_ORDER' AND SalesOrderID IS NULL AND PurchaseOrderID IS NULL AND TransferOrderID IS NOT NULL)
        ),
        CONSTRAINT CK_OutboundOrders_DueDate CHECK (DueDate IS NULL OR DueDate >= ExpectedIssueDate)
    );
END;
GO

IF OBJECT_ID(N'dbo.OutboundOrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboundOrderItems
    (
        OutboundOrderItemID BIGINT IDENTITY(1,1) NOT NULL,
        OutboundOrderID     BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        RequestedQuantity   DECIMAL(18,4) NOT NULL,
        IssuedQuantity      DECIMAL(18,4) NOT NULL CONSTRAINT DF_OutboundOrderItems_Issued DEFAULT (0),
        Notes               NVARCHAR(1000) NULL,
        CONSTRAINT PK_OutboundOrderItems PRIMARY KEY (OutboundOrderItemID),
        CONSTRAINT UQ_OutboundOrderItems_Product UNIQUE (OutboundOrderID, ProductID),
        CONSTRAINT UQ_OutboundOrderItems_Composite UNIQUE (OutboundOrderItemID, OutboundOrderID, ProductID),
        CONSTRAINT FK_OutboundOrderItems_Order FOREIGN KEY (OutboundOrderID) REFERENCES dbo.OutboundOrders(OutboundOrderID),
        CONSTRAINT FK_OutboundOrderItems_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID),
        CONSTRAINT CK_OutboundOrderItems_Requested CHECK (RequestedQuantity > 0),
        CONSTRAINT CK_OutboundOrderItems_Issued CHECK (IssuedQuantity >= 0 AND IssuedQuantity <= RequestedQuantity)
    );
END;
GO

/* Quan he 3 ben: Outbound Order - Product - Storage Location (+ Product Lot). */
IF OBJECT_ID(N'dbo.OutboundOrderDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboundOrderDetails
    (
        OutboundOrderDetailID BIGINT IDENTITY(1,1) NOT NULL,
        OutboundOrderItemID BIGINT NOT NULL,
        OutboundOrderID     BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        StorageLocationID   BIGINT NOT NULL,
        ProductLotID        BIGINT NOT NULL,
        InventoryReservationID BIGINT NULL,
        IssuedQuantity      DECIMAL(18,4) NOT NULL,
        RecordedByUserID    BIGINT NOT NULL,
        RecordedAt          DATETIME2(0) NOT NULL CONSTRAINT DF_OutboundOrderDetails_RecordedAt DEFAULT (SYSUTCDATETIME()),
        Notes               NVARCHAR(1000) NULL,
        CONSTRAINT PK_OutboundOrderDetails PRIMARY KEY (OutboundOrderDetailID),
        CONSTRAINT FK_OutboundOrderDetails_Item FOREIGN KEY (OutboundOrderItemID, OutboundOrderID, ProductID)
            REFERENCES dbo.OutboundOrderItems(OutboundOrderItemID, OutboundOrderID, ProductID),
        CONSTRAINT FK_OutboundOrderDetails_Location FOREIGN KEY (StorageLocationID) REFERENCES dbo.StorageLocations(StorageLocationID),
        CONSTRAINT FK_OutboundOrderDetails_LotProduct FOREIGN KEY (ProductLotID, ProductID) REFERENCES dbo.ProductLots(ProductLotID, ProductID),
        CONSTRAINT FK_OutboundOrderDetails_Reservation FOREIGN KEY (InventoryReservationID) REFERENCES dbo.InventoryReservations(InventoryReservationID),
        CONSTRAINT FK_OutboundOrderDetails_User FOREIGN KEY (RecordedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_OutboundOrderDetails_Quantity CHECK (IssuedQuantity > 0)
    );
END;
GO

/*=============================================================================
 09. KIEM KHO THEO TUNG BIN VA SAN PHAM
=============================================================================*/

IF OBJECT_ID(N'dbo.StocktakeSchedules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StocktakeSchedules
    (
        StocktakeScheduleID BIGINT IDENTITY(1,1) NOT NULL,
        WarehouseID         BIGINT NOT NULL,
        ScheduleName        NVARCHAR(200) NOT NULL,
        FrequencyType       VARCHAR(20) NOT NULL,
        DayOfWeek           TINYINT NULL,
        DayOfMonth          TINYINT NULL,
        StartDate           DATE NOT NULL,
        NextRunDate         DATE NULL,
        IsActive            BIT NOT NULL CONSTRAINT DF_StocktakeSchedules_Active DEFAULT (1),
        CreatedByUserID     BIGINT NOT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_StocktakeSchedules_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_StocktakeSchedules PRIMARY KEY (StocktakeScheduleID),
        CONSTRAINT FK_StocktakeSchedules_Warehouse FOREIGN KEY (WarehouseID) REFERENCES dbo.Warehouses(WarehouseID),
        CONSTRAINT FK_StocktakeSchedules_User FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_StocktakeSchedules_Frequency CHECK (FrequencyType IN ('WEEKLY','MONTHLY','QUARTERLY','YEARLY')),
        CONSTRAINT CK_StocktakeSchedules_DayOfWeek CHECK (DayOfWeek IS NULL OR DayOfWeek BETWEEN 1 AND 7),
        CONSTRAINT CK_StocktakeSchedules_DayOfMonth CHECK (DayOfMonth IS NULL OR DayOfMonth BETWEEN 1 AND 31),
        CONSTRAINT CK_StocktakeSchedules_FrequencyDay CHECK
        (
            (FrequencyType = 'WEEKLY' AND DayOfWeek IS NOT NULL AND DayOfMonth IS NULL)
            OR
            (FrequencyType IN ('MONTHLY','QUARTERLY','YEARLY') AND DayOfMonth IS NOT NULL AND DayOfWeek IS NULL)
        )
    );
END;
GO

IF OBJECT_ID(N'dbo.StocktakeSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StocktakeSessions
    (
        StocktakeSessionID  BIGINT IDENTITY(1,1) NOT NULL,
        StocktakeNumber     VARCHAR(50) NOT NULL,
        WarehouseID         BIGINT NOT NULL,
        StocktakeScheduleID BIGINT NULL,
        PlannedDate         DATE NOT NULL,
        Status              VARCHAR(30) NOT NULL CONSTRAINT DF_StocktakeSessions_Status DEFAULT ('SCHEDULED'),
        CreatedByUserID     BIGINT NOT NULL,
        AssignedToUserID    BIGINT NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_StocktakeSessions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        StartedAt           DATETIME2(0) NULL,
        SubmittedAt         DATETIME2(0) NULL,
        ApprovedByUserID    BIGINT NULL,
        ApprovedAt          DATETIME2(0) NULL,
        Notes               NVARCHAR(2000) NULL,
        CONSTRAINT PK_StocktakeSessions PRIMARY KEY (StocktakeSessionID),
        CONSTRAINT UQ_StocktakeSessions_Number UNIQUE (StocktakeNumber),
        CONSTRAINT FK_StocktakeSessions_Warehouse FOREIGN KEY (WarehouseID) REFERENCES dbo.Warehouses(WarehouseID),
        CONSTRAINT FK_StocktakeSessions_Schedule FOREIGN KEY (StocktakeScheduleID) REFERENCES dbo.StocktakeSchedules(StocktakeScheduleID),
        CONSTRAINT FK_StocktakeSessions_CreatedBy FOREIGN KEY (CreatedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_StocktakeSessions_AssignedTo FOREIGN KEY (AssignedToUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_StocktakeSessions_ApprovedBy FOREIGN KEY (ApprovedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_StocktakeSessions_Status CHECK (Status IN ('SCHEDULED','IN_PROGRESS','COUNTED','PENDING_APPROVAL','COMPLETED','CANCELLED'))
    );
END;
GO

/* COUNTS: Stocktake Session - Product - Storage Location (+ Product Lot). */
IF OBJECT_ID(N'dbo.StocktakeItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StocktakeItems
    (
        StocktakeItemID     BIGINT IDENTITY(1,1) NOT NULL,
        StocktakeSessionID  BIGINT NOT NULL,
        StorageLocationID   BIGINT NOT NULL,
        ProductID           BIGINT NOT NULL,
        ProductLotID        BIGINT NOT NULL,
        BookQuantity        DECIMAL(18,4) NOT NULL,
        CountedQuantity     DECIMAL(18,4) NULL,
        DifferenceQuantity AS (CASE WHEN CountedQuantity IS NULL THEN NULL ELSE CountedQuantity - BookQuantity END) PERSISTED,
        AdjustmentQuantity  DECIMAL(18,4) NULL,
        Resolution          VARCHAR(30) NULL,
        CountedByUserID     BIGINT NULL,
        CountedAt           DATETIME2(0) NULL,
        ApprovedByUserID    BIGINT NULL,
        ApprovedAt          DATETIME2(0) NULL,
        Notes               NVARCHAR(1000) NULL,
        CONSTRAINT PK_StocktakeItems PRIMARY KEY (StocktakeItemID),
        CONSTRAINT UQ_StocktakeItems_Target UNIQUE (StocktakeSessionID, StorageLocationID, ProductID, ProductLotID),
        CONSTRAINT FK_StocktakeItems_Session FOREIGN KEY (StocktakeSessionID) REFERENCES dbo.StocktakeSessions(StocktakeSessionID),
        CONSTRAINT FK_StocktakeItems_Location FOREIGN KEY (StorageLocationID) REFERENCES dbo.StorageLocations(StorageLocationID),
        CONSTRAINT FK_StocktakeItems_LotProduct FOREIGN KEY (ProductLotID, ProductID) REFERENCES dbo.ProductLots(ProductLotID, ProductID),
        CONSTRAINT FK_StocktakeItems_CountedBy FOREIGN KEY (CountedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT FK_StocktakeItems_ApprovedBy FOREIGN KEY (ApprovedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_StocktakeItems_Book CHECK (BookQuantity >= 0),
        CONSTRAINT CK_StocktakeItems_Counted CHECK (CountedQuantity IS NULL OR CountedQuantity >= 0),
        CONSTRAINT CK_StocktakeItems_Resolution CHECK (Resolution IS NULL OR Resolution IN ('ACCEPT_DIFFERENCE','RECOUNT','NO_ADJUSTMENT'))
    );
END;
GO

/* Danh sach Bin bat buoc phai di qua trong dot kiem kho. */
IF OBJECT_ID(N'dbo.StocktakeLocations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StocktakeLocations
    (
        StocktakeSessionID  BIGINT NOT NULL,
        StorageLocationID   BIGINT NOT NULL,
        CountStatus         VARCHAR(20) NOT NULL CONSTRAINT DF_StocktakeLocations_Status DEFAULT ('PENDING'),
        CountedByUserID     BIGINT NULL,
        CountedAt           DATETIME2(0) NULL,
        Notes               NVARCHAR(1000) NULL,
        CONSTRAINT PK_StocktakeLocations PRIMARY KEY (StocktakeSessionID, StorageLocationID),
        CONSTRAINT FK_StocktakeLocations_Session FOREIGN KEY (StocktakeSessionID) REFERENCES dbo.StocktakeSessions(StocktakeSessionID),
        CONSTRAINT FK_StocktakeLocations_Location FOREIGN KEY (StorageLocationID) REFERENCES dbo.StorageLocations(StorageLocationID),
        CONSTRAINT FK_StocktakeLocations_User FOREIGN KEY (CountedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_StocktakeLocations_Status CHECK (CountStatus IN ('PENDING','IN_PROGRESS','COUNTED','RECOUNT_REQUIRED'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StocktakeItems_SessionLocation')
    ALTER TABLE dbo.StocktakeItems ADD CONSTRAINT FK_StocktakeItems_SessionLocation
        FOREIGN KEY (StocktakeSessionID, StorageLocationID)
        REFERENCES dbo.StocktakeLocations(StocktakeSessionID, StorageLocationID);
GO

/*=============================================================================
 10. TON KHO HIEN THOI VA SO GIAO DICH BAT BIEN
=============================================================================*/

/* STORED AT: bang tong hop quan he Product - Storage Location - Product Lot. */
IF OBJECT_ID(N'dbo.Inventory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Inventory
    (
        InventoryID         BIGINT IDENTITY(1,1) NOT NULL,
        ProductID           BIGINT NOT NULL,
        StorageLocationID   BIGINT NOT NULL,
        ProductLotID        BIGINT NOT NULL,
        OnHandQuantity      DECIMAL(18,4) NOT NULL CONSTRAINT DF_Inventory_OnHand DEFAULT (0),
        ReservedQuantity    DECIMAL(18,4) NOT NULL CONSTRAINT DF_Inventory_Reserved DEFAULT (0),
        AvailableQuantity AS (OnHandQuantity - ReservedQuantity) PERSISTED,
        LastUpdatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_Inventory_Updated DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Inventory PRIMARY KEY (InventoryID),
        CONSTRAINT UQ_Inventory_Target UNIQUE (ProductID, StorageLocationID, ProductLotID),
        CONSTRAINT FK_Inventory_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID),
        CONSTRAINT FK_Inventory_Location FOREIGN KEY (StorageLocationID) REFERENCES dbo.StorageLocations(StorageLocationID),
        CONSTRAINT FK_Inventory_LotProduct FOREIGN KEY (ProductLotID, ProductID) REFERENCES dbo.ProductLots(ProductLotID, ProductID),
        CONSTRAINT CK_Inventory_OnHand CHECK (OnHandQuantity >= 0),
        CONSTRAINT CK_Inventory_Reserved CHECK (ReservedQuantity >= 0 AND ReservedQuantity <= OnHandQuantity)
    );
END;
GO

IF OBJECT_ID(N'dbo.InventoryTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryTransactions
    (
        InventoryTransactionID BIGINT IDENTITY(1,1) NOT NULL,
        TransactionType     VARCHAR(40) NOT NULL,
        ProductID           BIGINT NOT NULL,
        StorageLocationID   BIGINT NOT NULL,
        ProductLotID        BIGINT NOT NULL,
        OnHandDelta         DECIMAL(18,4) NOT NULL CONSTRAINT DF_InventoryTransactions_OnHand DEFAULT (0),
        ReservedDelta       DECIMAL(18,4) NOT NULL CONSTRAINT DF_InventoryTransactions_Reserved DEFAULT (0),
        InboundOrderDetailID BIGINT NULL,
        OutboundOrderDetailID BIGINT NULL,
        TransferOrderDetailID BIGINT NULL,
        StocktakeItemID     BIGINT NULL,
        InventoryReservationID BIGINT NULL,
        PerformedByUserID   BIGINT NOT NULL,
        TransactionAt       DATETIME2(0) NOT NULL CONSTRAINT DF_InventoryTransactions_At DEFAULT (SYSUTCDATETIME()),
        Notes               NVARCHAR(1000) NULL,
        CONSTRAINT PK_InventoryTransactions PRIMARY KEY (InventoryTransactionID),
        CONSTRAINT FK_InventoryTransactions_Product FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID),
        CONSTRAINT FK_InventoryTransactions_Location FOREIGN KEY (StorageLocationID) REFERENCES dbo.StorageLocations(StorageLocationID),
        CONSTRAINT FK_InventoryTransactions_LotProduct FOREIGN KEY (ProductLotID, ProductID) REFERENCES dbo.ProductLots(ProductLotID, ProductID),
        CONSTRAINT FK_InventoryTransactions_InboundDetail FOREIGN KEY (InboundOrderDetailID) REFERENCES dbo.InboundOrderDetails(InboundOrderDetailID),
        CONSTRAINT FK_InventoryTransactions_OutboundDetail FOREIGN KEY (OutboundOrderDetailID) REFERENCES dbo.OutboundOrderDetails(OutboundOrderDetailID),
        CONSTRAINT FK_InventoryTransactions_TransferDetail FOREIGN KEY (TransferOrderDetailID) REFERENCES dbo.TransferOrderDetails(TransferOrderDetailID),
        CONSTRAINT FK_InventoryTransactions_StocktakeItem FOREIGN KEY (StocktakeItemID) REFERENCES dbo.StocktakeItems(StocktakeItemID),
        CONSTRAINT FK_InventoryTransactions_Reservation FOREIGN KEY (InventoryReservationID) REFERENCES dbo.InventoryReservations(InventoryReservationID),
        CONSTRAINT FK_InventoryTransactions_User FOREIGN KEY (PerformedByUserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_InventoryTransactions_Type CHECK
        (
            TransactionType IN ('INBOUND','OUTBOUND','TRANSFER_OUT','TRANSFER_IN','STOCKTAKE_ADJUSTMENT','RESERVE','RELEASE_RESERVATION')
        ),
        CONSTRAINT CK_InventoryTransactions_Delta CHECK
        (
            (TransactionType = 'INBOUND' AND OnHandDelta > 0 AND ReservedDelta = 0)
            OR (TransactionType = 'OUTBOUND' AND OnHandDelta < 0 AND ReservedDelta <= 0)
            OR (TransactionType = 'TRANSFER_OUT' AND OnHandDelta < 0 AND ReservedDelta = 0)
            OR (TransactionType = 'TRANSFER_IN' AND OnHandDelta > 0 AND ReservedDelta = 0)
            OR (TransactionType = 'STOCKTAKE_ADJUSTMENT' AND OnHandDelta <> 0 AND ReservedDelta = 0)
            OR (TransactionType = 'RESERVE' AND OnHandDelta = 0 AND ReservedDelta > 0)
            OR (TransactionType = 'RELEASE_RESERVATION' AND OnHandDelta = 0 AND ReservedDelta < 0)
        ),
        CONSTRAINT CK_InventoryTransactions_Source CHECK
        (
            (TransactionType = 'INBOUND' AND InboundOrderDetailID IS NOT NULL)
            OR (TransactionType = 'OUTBOUND' AND OutboundOrderDetailID IS NOT NULL)
            OR (TransactionType IN ('TRANSFER_OUT','TRANSFER_IN') AND TransferOrderDetailID IS NOT NULL)
            OR (TransactionType = 'STOCKTAKE_ADJUSTMENT' AND StocktakeItemID IS NOT NULL)
            OR (TransactionType IN ('RESERVE','RELEASE_RESERVATION') AND InventoryReservationID IS NOT NULL)
        )
    );
END;
GO

/*=============================================================================
 11. THONG BAO, AUDIT LOG
=============================================================================*/

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        NotificationID     BIGINT IDENTITY(1,1) NOT NULL,
        UserID              BIGINT NOT NULL,
        NotificationType    VARCHAR(40) NOT NULL,
        Title               NVARCHAR(250) NOT NULL,
        Message             NVARCHAR(2000) NOT NULL,
        ReferenceType       VARCHAR(50) NULL,
        ReferenceID         BIGINT NULL,
        IsRead              BIT NOT NULL CONSTRAINT DF_Notifications_Read DEFAULT (0),
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ReadAt              DATETIME2(0) NULL,
        CONSTRAINT PK_Notifications PRIMARY KEY (NotificationID),
        CONSTRAINT FK_Notifications_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_Notifications_Type CHECK (NotificationType IN ('WORK_ASSIGNMENT','LOW_STOCK','EXPIRY','OVERDUE','STOCKTAKE','SYSTEM'))
    );
END;
GO

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        AuditLogID          BIGINT IDENTITY(1,1) NOT NULL,
        UserID              BIGINT NULL,
        ActionType          VARCHAR(50) NOT NULL,
        EntityName          VARCHAR(100) NOT NULL,
        EntityID            NVARCHAR(100) NULL,
        OldValuesJson       NVARCHAR(MAX) NULL,
        NewValuesJson       NVARCHAR(MAX) NULL,
        IpAddress           VARCHAR(45) NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AuditLogs PRIMARY KEY (AuditLogID),
        CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
        CONSTRAINT CK_AuditLogs_OldJson CHECK (OldValuesJson IS NULL OR ISJSON(OldValuesJson) = 1),
        CONSTRAINT CK_AuditLogs_NewJson CHECK (NewValuesJson IS NULL OR ISJSON(NewValuesJson) = 1)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_CreatedAt' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs(CreatedAt DESC, AuditLogID DESC)
        INCLUDE (UserID, ActionType, EntityName, EntityID, IpAddress);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_UserDate' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_UserDate ON dbo.AuditLogs(UserID, CreatedAt DESC, AuditLogID DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_EntityDate' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_EntityDate ON dbo.AuditLogs(EntityName, EntityID, CreatedAt DESC, AuditLogID DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_ActionDate' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_ActionDate ON dbo.AuditLogs(ActionType, CreatedAt DESC, AuditLogID DESC);
GO

/*=============================================================================
 12. INDEX PHUC VU TRA CUU VA DAM BAO IDEMPOTENT
=============================================================================*/

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Inventory_LocationProduct' AND object_id = OBJECT_ID(N'dbo.Inventory'))
    CREATE INDEX IX_Inventory_LocationProduct ON dbo.Inventory(StorageLocationID, ProductID) INCLUDE (OnHandQuantity, ReservedQuantity, AvailableQuantity, ProductLotID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Inventory_ProductAvailable' AND object_id = OBJECT_ID(N'dbo.Inventory'))
    CREATE INDEX IX_Inventory_ProductAvailable ON dbo.Inventory(ProductID, AvailableQuantity) INCLUDE (StorageLocationID, ProductLotID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InventoryTransactions_ProductDate' AND object_id = OBJECT_ID(N'dbo.InventoryTransactions'))
    CREATE INDEX IX_InventoryTransactions_ProductDate ON dbo.InventoryTransactions(ProductID, TransactionAt DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InventoryTransactions_LocationDate' AND object_id = OBJECT_ID(N'dbo.InventoryTransactions'))
    CREATE INDEX IX_InventoryTransactions_LocationDate ON dbo.InventoryTransactions(StorageLocationID, TransactionAt DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InboundOrders_StatusDue' AND object_id = OBJECT_ID(N'dbo.InboundOrders'))
    CREATE INDEX IX_InboundOrders_StatusDue ON dbo.InboundOrders(Status, DueDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OutboundOrders_StatusDue' AND object_id = OBJECT_ID(N'dbo.OutboundOrders'))
    CREATE INDEX IX_OutboundOrders_StatusDue ON dbo.OutboundOrders(Status, DueDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notifications_UserUnread' AND object_id = OBJECT_ID(N'dbo.Notifications'))
    CREATE INDEX IX_Notifications_UserUnread ON dbo.Notifications(UserID, IsRead, CreatedAt DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductLots_Expiry' AND object_id = OBJECT_ID(N'dbo.ProductLots'))
    CREATE INDEX IX_ProductLots_Expiry ON dbo.ProductLots(ExpiryDate, Status) INCLUDE (ProductID, LotNumber);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_InventoryTransactions_InboundDetail' AND object_id = OBJECT_ID(N'dbo.InventoryTransactions'))
    CREATE UNIQUE INDEX UX_InventoryTransactions_InboundDetail ON dbo.InventoryTransactions(InboundOrderDetailID)
    WHERE InboundOrderDetailID IS NOT NULL AND TransactionType = 'INBOUND';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_InventoryTransactions_OutboundDetail' AND object_id = OBJECT_ID(N'dbo.InventoryTransactions'))
    CREATE UNIQUE INDEX UX_InventoryTransactions_OutboundDetail ON dbo.InventoryTransactions(OutboundOrderDetailID)
    WHERE OutboundOrderDetailID IS NOT NULL AND TransactionType = 'OUTBOUND';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_InventoryTransactions_StocktakeItem' AND object_id = OBJECT_ID(N'dbo.InventoryTransactions'))
    CREATE UNIQUE INDEX UX_InventoryTransactions_StocktakeItem ON dbo.InventoryTransactions(StocktakeItemID)
    WHERE StocktakeItemID IS NOT NULL AND TransactionType = 'STOCKTAKE_ADJUSTMENT';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_InventoryTransactions_TransferType' AND object_id = OBJECT_ID(N'dbo.InventoryTransactions'))
BEGIN
    CREATE UNIQUE INDEX UX_InventoryTransactions_TransferType ON dbo.InventoryTransactions(TransferOrderDetailID, TransactionType)
    WHERE TransferOrderDetailID IS NOT NULL;
END;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Products_Barcode' AND parent_object_id = OBJECT_ID(N'dbo.Products'))
    ALTER TABLE dbo.Products DROP CONSTRAINT UQ_Products_Barcode;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Products_Barcode' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE UNIQUE INDEX UX_Products_Barcode ON dbo.Products(Barcode) WHERE Barcode IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ProductFixedLocations_Default' AND object_id = OBJECT_ID(N'dbo.ProductFixedLocations'))
    CREATE UNIQUE INDEX UX_ProductFixedLocations_Default ON dbo.ProductFixedLocations(ProductID) WHERE IsDefault = 1 AND IsActive = 1;
GO

/*=============================================================================
 13. TRIGGER BAO VE NGHIEP VU VA CAP NHAT INVENTORY
=============================================================================*/

CREATE OR ALTER TRIGGER dbo.trg_InboundOrders_ValidateSupplement
ON dbo.InboundOrders
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.InboundOrders parent ON parent.InboundOrderID = i.ParentInboundOrderID
        WHERE i.ParentInboundOrderID IS NOT NULL
          AND
          (
              i.SourceType <> 'PURCHASE_ORDER'
              OR parent.SourceType <> 'PURCHASE_ORDER'
              OR i.PurchaseOrderID <> parent.PurchaseOrderID
          )
    )
        THROW 51000, N'Phieu nhap bo sung phai tham chieu cung Purchase Order voi phieu nhap goc.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_InboundOrderItems_ValidateSource
ON dbo.InboundOrderItems
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted item
        JOIN dbo.InboundOrders io ON io.InboundOrderID = item.InboundOrderID
        WHERE (io.SourceType = 'PURCHASE_ORDER' AND NOT EXISTS
              (SELECT 1 FROM dbo.PurchaseOrderDetails d WHERE d.PurchaseOrderID = io.PurchaseOrderID AND d.ProductID = item.ProductID))
           OR (io.SourceType = 'SALES_RETURN' AND NOT EXISTS
              (SELECT 1 FROM dbo.SalesOrderDetails d WHERE d.SalesOrderID = io.SalesOrderID AND d.ProductID = item.ProductID))
           OR (io.SourceType = 'TRANSFER_ORDER' AND NOT EXISTS
              (SELECT 1 FROM dbo.TransferOrderDetails d WHERE d.TransferOrderID = io.TransferOrderID AND d.ProductID = item.ProductID))
    )
        THROW 51008, N'San pham tren phieu nhap khong ton tai trong chung tu tham chieu.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_OutboundOrderItems_ValidateSource
ON dbo.OutboundOrderItems
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted item
        JOIN dbo.OutboundOrders oo ON oo.OutboundOrderID = item.OutboundOrderID
        WHERE (oo.SourceType = 'SALES_ORDER' AND NOT EXISTS
              (SELECT 1 FROM dbo.SalesOrderDetails d WHERE d.SalesOrderID = oo.SalesOrderID AND d.ProductID = item.ProductID))
           OR (oo.SourceType = 'PURCHASE_RETURN' AND NOT EXISTS
              (SELECT 1 FROM dbo.PurchaseOrderDetails d WHERE d.PurchaseOrderID = oo.PurchaseOrderID AND d.ProductID = item.ProductID))
           OR (oo.SourceType = 'TRANSFER_ORDER' AND NOT EXISTS
              (SELECT 1 FROM dbo.TransferOrderDetails d WHERE d.TransferOrderID = oo.TransferOrderID AND d.ProductID = item.ProductID))
    )
        THROW 51009, N'San pham tren phieu xuat khong ton tai trong chung tu tham chieu.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_ProductAttributeValues_Validate
ON dbo.ProductAttributeValues
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.Products p ON p.ProductID = i.ProductID
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.ProductGroupAttributes pga
            WHERE pga.ProductGroupID = p.ProductGroupID
              AND pga.ProductAttributeID = i.ProductAttributeID
        )
    )
        THROW 51010, N'Thuoc tinh khong duoc cau hinh cho Product Group cua san pham.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.ProductAttributes pa ON pa.ProductAttributeID = i.ProductAttributeID
        WHERE pa.DataType = 'OPTION'
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.ProductAttributeOptions o
              WHERE o.ProductAttributeID = i.ProductAttributeID
                AND o.OptionCode = i.AttributeValue
                AND o.IsActive = 1
          )
    )
        THROW 51011, N'Gia tri thuoc tinh OPTION khong nam trong danh sach cau hinh.', 1;
END;
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
        FROM inserted i
        JOIN dbo.InboundOrders io ON io.InboundOrderID = i.InboundOrderID
        JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
        WHERE sl.WarehouseID <> io.WarehouseID
    )
        THROW 51001, N'Vi tri xac nhan nhap khong thuoc kho cua phieu nhap.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
        WHERE i.ConditionStatus = 'GOOD'
          AND sl.LocationType = 'BIN'
          AND (sl.IsPutawayAllowed = 0 OR sl.Status IN ('BLOCKED','INACTIVE'))
    )
        THROW 51002, N'Hang tot chi duoc cat vao bin dang hoat dong va cho phep putaway.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
        WHERE i.ConditionStatus IN ('DAMAGED','QUARANTINED')
          AND sl.LocationType <> 'QUARANTINE'
    )
        THROW 51003, N'Hang hong/cach ly phai duoc ghi nhan tai vi tri QUARANTINE.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_OutboundOrderDetails_Validate
ON dbo.OutboundOrderDetails
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.OutboundOrders oo ON oo.OutboundOrderID = i.OutboundOrderID
        JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
        WHERE sl.WarehouseID <> oo.WarehouseID OR sl.IsPickable = 0 OR sl.Status IN ('BLOCKED','INACTIVE')
    )
        THROW 51004, N'Vi tri lay hang khong hop le hoac khong thuoc kho cua phieu xuat.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        WHERE i.InventoryReservationID IS NOT NULL
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.InventoryReservations r
              WHERE r.InventoryReservationID = i.InventoryReservationID
                AND r.ProductID = i.ProductID
                AND r.StorageLocationID = i.StorageLocationID
                AND r.ProductLotID = i.ProductLotID
                AND r.Status IN ('ACTIVE','PARTIALLY_CONSUMED')
          )
    )
        THROW 51005, N'Chi tiet xuat khong khop voi ban ghi giu ton.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_TransferOrderDetails_Validate
ON dbo.TransferOrderDetails
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.TransferOrders t ON t.TransferOrderID = i.TransferOrderID
        LEFT JOIN dbo.StorageLocations src ON src.StorageLocationID = i.SourceLocationID
        LEFT JOIN dbo.StorageLocations dst ON dst.StorageLocationID = i.DestinationLocationID
        WHERE (i.SourceLocationID IS NOT NULL AND src.WarehouseID <> t.SourceWarehouseID)
           OR (i.DestinationLocationID IS NOT NULL AND dst.WarehouseID <> t.DestinationWarehouseID)
    )
        THROW 51006, N'Vi tri nguon/dich khong thuoc kho nguon/dich cua phieu dieu chuyen.', 1;
END;
GO

/* Moi INSERT giao dich se cap nhat bang tong hop Inventory. */
CREATE OR ALTER TRIGGER dbo.trg_InventoryTransactions_ApplyToInventory
ON dbo.InventoryTransactions
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Delta AS
    (
        SELECT ProductID, StorageLocationID, ProductLotID,
               SUM(OnHandDelta) AS OnHandDelta,
               SUM(ReservedDelta) AS ReservedDelta
        FROM inserted
        GROUP BY ProductID, StorageLocationID, ProductLotID
    )
    MERGE dbo.Inventory WITH (HOLDLOCK) AS Target
    USING Delta AS Source
       ON Target.ProductID = Source.ProductID
      AND Target.StorageLocationID = Source.StorageLocationID
      AND Target.ProductLotID = Source.ProductLotID
    WHEN MATCHED THEN
        UPDATE SET
            OnHandQuantity = Target.OnHandQuantity + Source.OnHandDelta,
            ReservedQuantity = Target.ReservedQuantity + Source.ReservedDelta,
            LastUpdatedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (ProductID, StorageLocationID, ProductLotID, OnHandQuantity, ReservedQuantity, LastUpdatedAt)
        VALUES (Source.ProductID, Source.StorageLocationID, Source.ProductLotID,
                Source.OnHandDelta, Source.ReservedDelta, SYSUTCDATETIME());
END;
GO

/* Ledger bat bien: khong sua/xoa giao dich da ghi so. */
CREATE OR ALTER TRIGGER dbo.trg_InventoryTransactions_Immutable
ON dbo.InventoryTransactions
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51007, N'InventoryTransactions la so giao dich bat bien; khong duoc UPDATE hoac DELETE.', 1;
END;
GO

/*=============================================================================
 14. STORED PROCEDURE NGHIEP VU COT LOI
=============================================================================*/

CREATE OR ALTER PROCEDURE dbo.usp_CheckInventoryAvailability
    @ProductID        BIGINT,
    @RequiredQuantity DECIMAL(18,4) = NULL,
    @WarehouseID      BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @RequiredQuantity IS NOT NULL AND @RequiredQuantity <= 0
        THROW 51101, N'So luong can kiem tra phai lon hon 0.', 1;

    ;WITH AvailableStock AS
    (
        SELECT i.ProductID, i.StorageLocationID, sl.LocationCode, sl.WarehouseID,
               i.ProductLotID, pl.LotNumber, pl.FirstReceivedDate, pl.ExpiryDate,
               i.OnHandQuantity, i.ReservedQuantity, i.AvailableQuantity,
               p.RotationMethod,
               SUM(i.AvailableQuantity) OVER () AS TotalAvailableQuantity,
               SUM(i.AvailableQuantity) OVER
               (
                   ORDER BY
                       CASE WHEN p.RotationMethod = 'FEFO' THEN pl.ExpiryDate END,
                       pl.FirstReceivedDate,
                       i.InventoryID
                   ROWS UNBOUNDED PRECEDING
               ) AS RunningAvailableQuantity
        FROM dbo.Inventory i
        JOIN dbo.Products p ON p.ProductID = i.ProductID
        JOIN dbo.ProductLots pl ON pl.ProductLotID = i.ProductLotID
        JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
        WHERE i.ProductID = @ProductID
          AND (@WarehouseID IS NULL OR sl.WarehouseID = @WarehouseID)
          AND i.AvailableQuantity > 0
          AND p.Status = 'ACTIVE'
          AND pl.Status = 'AVAILABLE'
          AND sl.IsPickable = 1
          AND sl.Status NOT IN ('BLOCKED','INACTIVE')
          AND sl.LocationType <> 'QUARANTINE'
    )
    SELECT *,
           CASE WHEN @RequiredQuantity IS NULL OR TotalAvailableQuantity >= @RequiredQuantity THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsSufficient
    FROM AvailableStock
    ORDER BY
        CASE WHEN RotationMethod = 'FEFO' THEN ExpiryDate END,
        FirstReceivedDate,
        StorageLocationID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ReserveSalesOrder
    @SalesOrderID BIGINT,
    @UserID       BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @InitialStatus VARCHAR(30);
        SELECT @InitialStatus = Status
        FROM dbo.SalesOrders WITH (UPDLOCK, HOLDLOCK)
        WHERE SalesOrderID = @SalesOrderID;

        IF @InitialStatus NOT IN ('DRAFT','CONFIRMED')
            THROW 51102, N'Chi don ban DRAFT hoac CONFIRMED moi duoc giu ton.', 1;

        DECLARE @SalesOrderDetailID BIGINT, @ProductID BIGINT, @Need DECIMAL(18,4);
        DECLARE DetailCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT SalesOrderDetailID, ProductID, OrderedQuantity - ReservedQuantity
            FROM dbo.SalesOrderDetails
            WHERE SalesOrderID = @SalesOrderID AND OrderedQuantity > ReservedQuantity;

        OPEN DetailCursor;
        FETCH NEXT FROM DetailCursor INTO @SalesOrderDetailID, @ProductID, @Need;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @LocationID BIGINT, @LotID BIGINT, @Available DECIMAL(18,4), @Take DECIMAL(18,4), @ReservationID BIGINT;

            DECLARE StockCursor CURSOR LOCAL FAST_FORWARD FOR
                SELECT i.StorageLocationID, i.ProductLotID, i.AvailableQuantity
                FROM dbo.Inventory i WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.ProductLots pl ON pl.ProductLotID = i.ProductLotID
                JOIN dbo.Products p ON p.ProductID = i.ProductID
                JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
                WHERE i.ProductID = @ProductID
                  AND i.AvailableQuantity > 0
                  AND pl.Status = 'AVAILABLE'
                  AND sl.IsPickable = 1
                  AND sl.Status NOT IN ('BLOCKED','INACTIVE')
                  AND sl.LocationType <> 'QUARANTINE'
                ORDER BY
                    CASE WHEN p.RotationMethod = 'FEFO' THEN pl.ExpiryDate END,
                    pl.FirstReceivedDate,
                    i.InventoryID;

            OPEN StockCursor;
            FETCH NEXT FROM StockCursor INTO @LocationID, @LotID, @Available;

            WHILE @@FETCH_STATUS = 0 AND @Need > 0
            BEGIN
                SET @Take = CASE WHEN @Available >= @Need THEN @Need ELSE @Available END;

                INSERT dbo.InventoryReservations
                (
                    SalesOrderDetailID, ProductID, StorageLocationID, ProductLotID,
                    ReservedQuantity, ReservedByUserID
                )
                VALUES
                (
                    @SalesOrderDetailID, @ProductID, @LocationID, @LotID,
                    @Take, @UserID
                );

                SET @ReservationID = SCOPE_IDENTITY();

                INSERT dbo.InventoryTransactions
                (
                    TransactionType, ProductID, StorageLocationID, ProductLotID,
                    OnHandDelta, ReservedDelta, InventoryReservationID, PerformedByUserID,
                    Notes
                )
                VALUES
                (
                    'RESERVE', @ProductID, @LocationID, @LotID,
                    0, @Take, @ReservationID, @UserID,
                    N'Giu ton cho Sales Order'
                );

                SET @Need = @Need - @Take;
                FETCH NEXT FROM StockCursor INTO @LocationID, @LotID, @Available;
            END;

            CLOSE StockCursor;
            DEALLOCATE StockCursor;

            IF @Need > 0
                THROW 51103, N'Ton kha dung khong du de giu cho toan bo Sales Order.', 1;

            UPDATE sod
            SET ReservedQuantity = x.ReservedQuantity
            FROM dbo.SalesOrderDetails sod
            CROSS APPLY
            (
                SELECT COALESCE(SUM(r.ReservedQuantity - r.ConsumedQuantity), 0) AS ReservedQuantity
                FROM dbo.InventoryReservations r
                WHERE r.SalesOrderDetailID = sod.SalesOrderDetailID
                  AND r.Status IN ('ACTIVE','PARTIALLY_CONSUMED')
            ) x
            WHERE sod.SalesOrderDetailID = @SalesOrderDetailID;

            FETCH NEXT FROM DetailCursor INTO @SalesOrderDetailID, @ProductID, @Need;
        END;

        CLOSE DetailCursor;
        DEALLOCATE DetailCursor;

        UPDATE dbo.SalesOrders
        SET Status = @InitialStatus, UpdatedAt = SYSUTCDATETIME()
        WHERE SalesOrderID = @SalesOrderID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local','StockCursor') >= -1
        BEGIN
            IF CURSOR_STATUS('local','StockCursor') > -1 CLOSE StockCursor;
            DEALLOCATE StockCursor;
        END;
        IF CURSOR_STATUS('local','DetailCursor') >= -1
        BEGIN
            IF CURSOR_STATUS('local','DetailCursor') > -1 CLOSE DetailCursor;
            DEALLOCATE DetailCursor;
        END;
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ReleaseSalesOrderReservations
    @SalesOrderID BIGINT,
    @UserID       BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT dbo.InventoryTransactions
        (
            TransactionType, ProductID, StorageLocationID, ProductLotID,
            OnHandDelta, ReservedDelta, InventoryReservationID, PerformedByUserID,
            Notes
        )
        SELECT 'RELEASE_RESERVATION', r.ProductID, r.StorageLocationID, r.ProductLotID,
               0, -(r.ReservedQuantity - r.ConsumedQuantity), r.InventoryReservationID,
               @UserID, N'Giai phong giu ton Sales Order'
        FROM dbo.InventoryReservations r
        JOIN dbo.SalesOrderDetails sod ON sod.SalesOrderDetailID = r.SalesOrderDetailID
        WHERE sod.SalesOrderID = @SalesOrderID
          AND r.Status IN ('ACTIVE','PARTIALLY_CONSUMED')
          AND r.ReservedQuantity > r.ConsumedQuantity;

        UPDATE r
        SET Status = 'RELEASED', ReleasedAt = SYSUTCDATETIME()
        FROM dbo.InventoryReservations r
        JOIN dbo.SalesOrderDetails sod ON sod.SalesOrderDetailID = r.SalesOrderDetailID
        WHERE sod.SalesOrderID = @SalesOrderID
          AND r.Status IN ('ACTIVE','PARTIALLY_CONSUMED');

        UPDATE dbo.SalesOrderDetails
        SET ReservedQuantity = 0
        WHERE SalesOrderID = @SalesOrderID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConfirmInboundOrder
    @InboundOrderID BIGINT,
    @UserID         BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.InboundOrders WITH (UPDLOCK, HOLDLOCK)
            WHERE InboundOrderID = @InboundOrderID
              AND Status IN ('DRAFT','ASSIGNED','IN_PROGRESS')
        )
            THROW 51104, N'Phieu nhap khong o trang thai cho phep xac nhan.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.InboundOrderDetails WHERE InboundOrderID = @InboundOrderID)
            THROW 51105, N'Phieu nhap chua co chi tiet vi tri/lo/so luong thuc nhan.', 1;

        INSERT dbo.InventoryTransactions
        (
            TransactionType, ProductID, StorageLocationID, ProductLotID,
            OnHandDelta, ReservedDelta, InboundOrderDetailID, PerformedByUserID,
            Notes
        )
        SELECT 'INBOUND', d.ProductID, d.StorageLocationID, d.ProductLotID,
               d.ReceivedQuantity, 0, d.InboundOrderDetailID, @UserID,
               CONCAT(N'Xac nhan ', io.InboundOrderNumber)
        FROM dbo.InboundOrderDetails d
        JOIN dbo.InboundOrders io ON io.InboundOrderID = d.InboundOrderID
        WHERE d.InboundOrderID = @InboundOrderID
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.InventoryTransactions t
              WHERE t.InboundOrderDetailID = d.InboundOrderDetailID
                AND t.TransactionType = 'INBOUND'
          );

        UPDATE i
        SET ReceivedQuantity = x.ReceivedQuantity,
            DamagedQuantity = x.DamagedQuantity,
            ShortageQuantity = CASE WHEN i.ExpectedQuantity > x.ReceivedQuantity
                                    THEN i.ExpectedQuantity - x.ReceivedQuantity ELSE 0 END
        FROM dbo.InboundOrderItems i
        CROSS APPLY
        (
            SELECT COALESCE(SUM(d.ReceivedQuantity),0) AS ReceivedQuantity,
                   COALESCE(SUM(CASE WHEN d.ConditionStatus IN ('DAMAGED','QUARANTINED') THEN d.ReceivedQuantity ELSE 0 END),0) AS DamagedQuantity
            FROM dbo.InboundOrderDetails d
            WHERE d.InboundOrderItemID = i.InboundOrderItemID
        ) x
        WHERE i.InboundOrderID = @InboundOrderID;

        UPDATE dbo.InboundOrders
        SET Status = 'COMPLETED', ConfirmedByUserID = @UserID, ConfirmedAt = SYSUTCDATETIME()
        WHERE InboundOrderID = @InboundOrderID;

        DECLARE @PurchaseOrderID BIGINT =
        (
            SELECT PurchaseOrderID FROM dbo.InboundOrders
            WHERE InboundOrderID = @InboundOrderID AND SourceType = 'PURCHASE_ORDER'
        );

        IF @PurchaseOrderID IS NOT NULL
        BEGIN
            ;WITH Ordered AS
            (
                SELECT SUM(OrderedQuantity) AS Quantity
                FROM dbo.PurchaseOrderDetails
                WHERE PurchaseOrderID = @PurchaseOrderID
            ), Received AS
            (
                SELECT COALESCE(SUM(ioi.ReceivedQuantity),0) AS Quantity
                FROM dbo.InboundOrders io
                JOIN dbo.InboundOrderItems ioi ON ioi.InboundOrderID = io.InboundOrderID
                WHERE io.PurchaseOrderID = @PurchaseOrderID AND io.Status = 'COMPLETED'
            )
            UPDATE dbo.PurchaseOrders
            SET Status = CASE WHEN r.Quantity >= o.Quantity THEN 'COMPLETED' ELSE 'PARTIALLY_RECEIVED' END,
                UpdatedAt = SYSUTCDATETIME()
            FROM Ordered o CROSS JOIN Received r
            WHERE PurchaseOrderID = @PurchaseOrderID;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConfirmOutboundOrder
    @OutboundOrderID BIGINT,
    @UserID          BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.OutboundOrders WITH (UPDLOCK, HOLDLOCK)
            WHERE OutboundOrderID = @OutboundOrderID
              AND Status IN ('DRAFT','ASSIGNED','IN_PROGRESS')
        )
            THROW 51106, N'Phieu xuat khong o trang thai cho phep xac nhan.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.OutboundOrderDetails WHERE OutboundOrderID = @OutboundOrderID)
            THROW 51107, N'Phieu xuat chua co chi tiet vi tri/lo/so luong thuc xuat.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.OutboundOrderDetails d
            JOIN dbo.InventoryReservations r ON r.InventoryReservationID = d.InventoryReservationID
            WHERE d.OutboundOrderID = @OutboundOrderID
            GROUP BY r.InventoryReservationID, r.ReservedQuantity, r.ConsumedQuantity
            HAVING SUM(d.IssuedQuantity) > r.ReservedQuantity - r.ConsumedQuantity
        )
            THROW 51108, N'So luong xuat vuot so luong dang duoc giu.', 1;

        INSERT dbo.InventoryTransactions
        (
            TransactionType, ProductID, StorageLocationID, ProductLotID,
            OnHandDelta, ReservedDelta, OutboundOrderDetailID,
            InventoryReservationID, PerformedByUserID, Notes
        )
        SELECT 'OUTBOUND', d.ProductID, d.StorageLocationID, d.ProductLotID,
               -d.IssuedQuantity,
               CASE WHEN d.InventoryReservationID IS NULL THEN 0 ELSE -d.IssuedQuantity END,
               d.OutboundOrderDetailID, d.InventoryReservationID, @UserID,
               CONCAT(N'Xac nhan ', oo.OutboundOrderNumber)
        FROM dbo.OutboundOrderDetails d
        JOIN dbo.OutboundOrders oo ON oo.OutboundOrderID = d.OutboundOrderID
        WHERE d.OutboundOrderID = @OutboundOrderID
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.InventoryTransactions t
              WHERE t.OutboundOrderDetailID = d.OutboundOrderDetailID
                AND t.TransactionType = 'OUTBOUND'
          );

        UPDATE r
        SET ConsumedQuantity = r.ConsumedQuantity + x.IssuedQuantity,
            Status = CASE WHEN r.ConsumedQuantity + x.IssuedQuantity >= r.ReservedQuantity
                          THEN 'CONSUMED' ELSE 'PARTIALLY_CONSUMED' END
        FROM dbo.InventoryReservations r
        JOIN
        (
            SELECT InventoryReservationID, SUM(IssuedQuantity) AS IssuedQuantity
            FROM dbo.OutboundOrderDetails
            WHERE OutboundOrderID = @OutboundOrderID AND InventoryReservationID IS NOT NULL
            GROUP BY InventoryReservationID
        ) x ON x.InventoryReservationID = r.InventoryReservationID;

        UPDATE i
        SET IssuedQuantity = x.IssuedQuantity
        FROM dbo.OutboundOrderItems i
        CROSS APPLY
        (
            SELECT COALESCE(SUM(d.IssuedQuantity),0) AS IssuedQuantity
            FROM dbo.OutboundOrderDetails d
            WHERE d.OutboundOrderItemID = i.OutboundOrderItemID
        ) x
        WHERE i.OutboundOrderID = @OutboundOrderID;

        UPDATE dbo.OutboundOrders
        SET Status = 'COMPLETED', ConfirmedByUserID = @UserID, ConfirmedAt = SYSUTCDATETIME()
        WHERE OutboundOrderID = @OutboundOrderID;

        DECLARE @SalesOrderID BIGINT =
        (
            SELECT SalesOrderID FROM dbo.OutboundOrders
            WHERE OutboundOrderID = @OutboundOrderID AND SourceType = 'SALES_ORDER'
        );

        IF @SalesOrderID IS NOT NULL
        BEGIN
            UPDATE sod
            SET FulfilledQuantity = x.FulfilledQuantity,
                ReservedQuantity = x.ActiveReservedQuantity
            FROM dbo.SalesOrderDetails sod
            CROSS APPLY
            (
                SELECT
                    COALESCE
                    (
                        (SELECT SUM(ood.IssuedQuantity)
                         FROM dbo.OutboundOrders oo
                         JOIN dbo.OutboundOrderItems ooi ON ooi.OutboundOrderID = oo.OutboundOrderID
                         JOIN dbo.OutboundOrderDetails ood ON ood.OutboundOrderItemID = ooi.OutboundOrderItemID
                         WHERE oo.SalesOrderID = @SalesOrderID
                           AND oo.Status = 'COMPLETED'
                           AND ooi.ProductID = sod.ProductID), 0
                    ) AS FulfilledQuantity,
                    COALESCE
                    (
                        (SELECT SUM(r.ReservedQuantity - r.ConsumedQuantity)
                         FROM dbo.InventoryReservations r
                         WHERE r.SalesOrderDetailID = sod.SalesOrderDetailID
                           AND r.Status IN ('ACTIVE','PARTIALLY_CONSUMED')), 0
                    ) AS ActiveReservedQuantity
            ) x
            WHERE sod.SalesOrderID = @SalesOrderID;

            UPDATE so
            SET Status = CASE
                            WHEN NOT EXISTS
                            (
                                SELECT 1 FROM dbo.SalesOrderDetails d
                                WHERE d.SalesOrderID = so.SalesOrderID AND d.FulfilledQuantity < d.OrderedQuantity
                            ) THEN 'FULFILLED'
                            ELSE 'PARTIALLY_FULFILLED'
                         END,
                UpdatedAt = SYSUTCDATETIME()
            FROM dbo.SalesOrders so
            WHERE so.SalesOrderID = @SalesOrderID;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConfirmInternalTransfer
    @TransferOrderID BIGINT,
    @UserID          BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.TransferOrders WITH (UPDLOCK, HOLDLOCK)
            WHERE TransferOrderID = @TransferOrderID
              AND TransferType = 'INTERNAL_LOCATION'
              AND Status IN ('DRAFT','ASSIGNED','IN_PROGRESS')
        )
            THROW 51109, N'Chi Transfer Order noi bo moi duoc xac nhan bang thu tuc nay.', 1;

        IF EXISTS
        (
            SELECT 1 FROM dbo.TransferOrderDetails
            WHERE TransferOrderID = @TransferOrderID
              AND (SourceLocationID IS NULL OR DestinationLocationID IS NULL OR ProductLotID IS NULL OR MovedQuantity <= 0)
        )
            THROW 51110, N'Chi tiet dieu chuyen chua du vi tri nguon, dich, lo va so luong.', 1;

        INSERT dbo.InventoryTransactions
        (
            TransactionType, ProductID, StorageLocationID, ProductLotID,
            OnHandDelta, ReservedDelta, TransferOrderDetailID, PerformedByUserID, Notes
        )
        SELECT 'TRANSFER_OUT', d.ProductID, d.SourceLocationID, d.ProductLotID,
               -d.MovedQuantity, 0, d.TransferOrderDetailID, @UserID,
               CONCAT(N'Xuat dieu chuyen ', t.TransferOrderNumber)
        FROM dbo.TransferOrderDetails d
        JOIN dbo.TransferOrders t ON t.TransferOrderID = d.TransferOrderID
        WHERE d.TransferOrderID = @TransferOrderID
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.InventoryTransactions it
              WHERE it.TransferOrderDetailID = d.TransferOrderDetailID AND it.TransactionType = 'TRANSFER_OUT'
          );

        INSERT dbo.InventoryTransactions
        (
            TransactionType, ProductID, StorageLocationID, ProductLotID,
            OnHandDelta, ReservedDelta, TransferOrderDetailID, PerformedByUserID, Notes
        )
        SELECT 'TRANSFER_IN', d.ProductID, d.DestinationLocationID, d.ProductLotID,
               d.MovedQuantity, 0, d.TransferOrderDetailID, @UserID,
               CONCAT(N'Nhap dieu chuyen ', t.TransferOrderNumber)
        FROM dbo.TransferOrderDetails d
        JOIN dbo.TransferOrders t ON t.TransferOrderID = d.TransferOrderID
        WHERE d.TransferOrderID = @TransferOrderID
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.InventoryTransactions it
              WHERE it.TransferOrderDetailID = d.TransferOrderDetailID AND it.TransactionType = 'TRANSFER_IN'
          );

        UPDATE dbo.TransferOrderDetails
        SET ConfirmedByUserID = @UserID, ConfirmedAt = SYSUTCDATETIME()
        WHERE TransferOrderID = @TransferOrderID;

        UPDATE dbo.TransferOrders
        SET Status = 'COMPLETED', ConfirmedByUserID = @UserID, ConfirmedAt = SYSUTCDATETIME()
        WHERE TransferOrderID = @TransferOrderID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_StartStocktakeSession
    @StocktakeSessionID BIGINT,
    @UserID             BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @WarehouseID BIGINT;

        SELECT @WarehouseID = WarehouseID
        FROM dbo.StocktakeSessions WITH (UPDLOCK, HOLDLOCK)
        WHERE StocktakeSessionID = @StocktakeSessionID AND Status = 'SCHEDULED';

        IF @WarehouseID IS NULL
            THROW 51111, N'Dot kiem kho khong ton tai hoac khong o trang thai SCHEDULED.', 1;

        INSERT dbo.StocktakeLocations
        (
            StocktakeSessionID, StorageLocationID
        )
        SELECT @StocktakeSessionID, sl.StorageLocationID
        FROM dbo.StorageLocations sl
        WHERE sl.WarehouseID = @WarehouseID
          AND sl.LocationType = 'BIN'
          AND sl.Status <> 'INACTIVE'
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.StocktakeLocations x
              WHERE x.StocktakeSessionID = @StocktakeSessionID
                AND x.StorageLocationID = sl.StorageLocationID
          );

        INSERT dbo.StocktakeItems
        (
            StocktakeSessionID, StorageLocationID, ProductID, ProductLotID, BookQuantity
        )
        SELECT @StocktakeSessionID, i.StorageLocationID, i.ProductID, i.ProductLotID, i.OnHandQuantity
        FROM dbo.Inventory i
        JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
        WHERE sl.WarehouseID = @WarehouseID
          AND i.OnHandQuantity > 0
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.StocktakeItems si
              WHERE si.StocktakeSessionID = @StocktakeSessionID
                AND si.StorageLocationID = i.StorageLocationID
                AND si.ProductID = i.ProductID
                AND si.ProductLotID = i.ProductLotID
          );

        UPDATE dbo.StocktakeSessions
        SET Status = 'IN_PROGRESS', StartedAt = SYSUTCDATETIME(), AssignedToUserID = COALESCE(AssignedToUserID, @UserID)
        WHERE StocktakeSessionID = @StocktakeSessionID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ApproveStocktakeSession
    @StocktakeSessionID BIGINT,
    @ManagerUserID      BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.StocktakeSessions WITH (UPDLOCK, HOLDLOCK)
            WHERE StocktakeSessionID = @StocktakeSessionID
              AND Status IN ('COUNTED','PENDING_APPROVAL')
        )
            THROW 51112, N'Dot kiem kho chua san sang de phe duyet.', 1;

        IF EXISTS
        (
            SELECT 1 FROM dbo.StocktakeItems
            WHERE StocktakeSessionID = @StocktakeSessionID AND CountedQuantity IS NULL
        )
            THROW 51113, N'Van con Bin/san pham chua nhap so luong thuc te.', 1;

        IF EXISTS
        (
            SELECT 1 FROM dbo.StocktakeLocations
            WHERE StocktakeSessionID = @StocktakeSessionID AND CountStatus <> 'COUNTED'
        )
            THROW 51114, N'Van con Bin chua hoan thanh kiem dem.', 1;

        UPDATE dbo.StocktakeItems
        SET AdjustmentQuantity = CASE WHEN Resolution = 'NO_ADJUSTMENT' THEN 0 ELSE DifferenceQuantity END,
            Resolution = COALESCE(Resolution, 'ACCEPT_DIFFERENCE'),
            ApprovedByUserID = @ManagerUserID,
            ApprovedAt = SYSUTCDATETIME()
        WHERE StocktakeSessionID = @StocktakeSessionID;

        INSERT dbo.InventoryTransactions
        (
            TransactionType, ProductID, StorageLocationID, ProductLotID,
            OnHandDelta, ReservedDelta, StocktakeItemID, PerformedByUserID, Notes
        )
        SELECT 'STOCKTAKE_ADJUSTMENT', ProductID, StorageLocationID, ProductLotID,
               AdjustmentQuantity, 0, StocktakeItemID, @ManagerUserID,
               N'Dieu chinh sau phe duyet kiem kho'
        FROM dbo.StocktakeItems si
        WHERE si.StocktakeSessionID = @StocktakeSessionID
          AND si.AdjustmentQuantity <> 0
          AND NOT EXISTS
          (
              SELECT 1 FROM dbo.InventoryTransactions t
              WHERE t.StocktakeItemID = si.StocktakeItemID
                AND t.TransactionType = 'STOCKTAKE_ADJUSTMENT'
          );

        UPDATE dbo.StocktakeSessions
        SET Status = 'COMPLETED', ApprovedByUserID = @ManagerUserID, ApprovedAt = SYSUTCDATETIME()
        WHERE StocktakeSessionID = @StocktakeSessionID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/*=============================================================================
 15. VIEW TRA CUU, BAO CAO, DASHBOARD VA CANH BAO
=============================================================================*/

CREATE OR ALTER VIEW dbo.vw_InventoryAvailability
AS
SELECT
    w.WarehouseID, w.WarehouseCode, w.WarehouseName,
    sl.StorageLocationID, sl.LocationCode, sl.LocationType,
    p.ProductID, p.ProductCode, p.ProductName, u.UnitCode,
    pl.ProductLotID, pl.LotNumber, pl.FirstReceivedDate, pl.ExpiryDate,
    i.OnHandQuantity, i.ReservedQuantity,
    CASE
        WHEN p.Status <> 'ACTIVE' OR pl.Status <> 'AVAILABLE'
          OR sl.IsPickable = 0 OR sl.Status IN ('BLOCKED','INACTIVE')
          OR sl.LocationType = 'QUARANTINE'
        THEN CONVERT(DECIMAL(18,4), 0)
        ELSE i.AvailableQuantity
    END AS AvailableQuantity,
    p.RotationMethod, i.LastUpdatedAt
FROM dbo.Inventory i
JOIN dbo.Products p ON p.ProductID = i.ProductID
JOIN dbo.UnitsOfMeasure u ON u.UnitOfMeasureID = p.UnitOfMeasureID
JOIN dbo.ProductLots pl ON pl.ProductLotID = i.ProductLotID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
JOIN dbo.Warehouses w ON w.WarehouseID = sl.WarehouseID;
GO

CREATE OR ALTER VIEW dbo.vw_ProductLocationLookup
AS
SELECT
    ProductID, ProductCode, ProductName,
    WarehouseID, WarehouseCode, WarehouseName,
    StorageLocationID, LocationCode,
    ProductLotID, LotNumber, ExpiryDate,
    OnHandQuantity, ReservedQuantity, AvailableQuantity, UnitCode
FROM dbo.vw_InventoryAvailability
WHERE OnHandQuantity > 0;
GO

CREATE OR ALTER VIEW dbo.vw_ProductTransactionHistory
AS
SELECT
    t.InventoryTransactionID, t.TransactionAt, t.TransactionType,
    t.ProductID, p.ProductCode, p.ProductName,
    sl.WarehouseID, sl.StorageLocationID, sl.LocationCode,
    t.ProductLotID, pl.LotNumber,
    t.OnHandDelta, t.ReservedDelta,
    t.InboundOrderDetailID, t.OutboundOrderDetailID,
    t.TransferOrderDetailID, t.StocktakeItemID, t.InventoryReservationID,
    t.PerformedByUserID, u.FullName AS PerformedBy, t.Notes
FROM dbo.InventoryTransactions t
JOIN dbo.Products p ON p.ProductID = t.ProductID
JOIN dbo.ProductLots pl ON pl.ProductLotID = t.ProductLotID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = t.StorageLocationID
JOIN dbo.Users u ON u.UserID = t.PerformedByUserID;
GO

CREATE OR ALTER VIEW dbo.vw_LowStockAlerts
AS
WITH AvailableByWarehouse AS
(
    SELECT sl.WarehouseID, i.ProductID,
           SUM(CASE WHEN sl.IsPickable = 1
                          AND sl.Status NOT IN ('BLOCKED','INACTIVE')
                          AND sl.LocationType <> 'QUARANTINE'
                    THEN i.AvailableQuantity ELSE 0 END) AS AvailableQuantity
    FROM dbo.Inventory i
    JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
    GROUP BY sl.WarehouseID, i.ProductID
)
SELECT
    policy.WarehouseID, w.WarehouseCode,
    policy.ProductID, p.ProductCode, p.ProductName,
    policy.MinimumStockQuantity,
    COALESCE(a.AvailableQuantity, 0) AS AvailableQuantity,
    policy.MinimumStockQuantity - COALESCE(a.AvailableQuantity, 0) AS ShortageQuantity
FROM dbo.ProductWarehousePolicies policy
JOIN dbo.Warehouses w ON w.WarehouseID = policy.WarehouseID
JOIN dbo.Products p ON p.ProductID = policy.ProductID
LEFT JOIN AvailableByWarehouse a ON a.WarehouseID = policy.WarehouseID AND a.ProductID = policy.ProductID
WHERE p.Status = 'ACTIVE'
  AND COALESCE(a.AvailableQuantity, 0) < policy.MinimumStockQuantity;
GO

CREATE OR ALTER VIEW dbo.vw_ExpiringLotAlerts
AS
SELECT
    sl.WarehouseID, w.WarehouseCode,
    i.ProductID, p.ProductCode, p.ProductName,
    i.ProductLotID, pl.LotNumber, pl.ExpiryDate,
    DATEDIFF(DAY, CONVERT(DATE, SYSUTCDATETIME()), pl.ExpiryDate) AS DaysToExpiry,
    SUM(i.OnHandQuantity) AS OnHandQuantity,
    SUM(i.AvailableQuantity) AS AvailableQuantity,
    policy.ExpiryWarningDays
FROM dbo.Inventory i
JOIN dbo.Products p ON p.ProductID = i.ProductID
JOIN dbo.ProductLots pl ON pl.ProductLotID = i.ProductLotID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
JOIN dbo.Warehouses w ON w.WarehouseID = sl.WarehouseID
JOIN dbo.ProductWarehousePolicies policy ON policy.ProductID = i.ProductID AND policy.WarehouseID = sl.WarehouseID
WHERE pl.ExpiryDate IS NOT NULL
  AND pl.Status IN ('AVAILABLE','QUARANTINED')
  AND i.OnHandQuantity > 0
  AND DATEDIFF(DAY, CONVERT(DATE, SYSUTCDATETIME()), pl.ExpiryDate) <= policy.ExpiryWarningDays
GROUP BY sl.WarehouseID, w.WarehouseCode, i.ProductID, p.ProductCode, p.ProductName,
         i.ProductLotID, pl.LotNumber, pl.ExpiryDate, policy.ExpiryWarningDays;
GO

CREATE OR ALTER VIEW dbo.vw_OverdueOrders
AS
SELECT
    CAST('INBOUND_ORDER' AS VARCHAR(30)) AS DocumentType,
    InboundOrderID AS DocumentID, InboundOrderNumber AS DocumentNumber,
    WarehouseID, DueDate,
    DATEDIFF(DAY, DueDate, CONVERT(DATE, SYSUTCDATETIME())) AS DaysOverdue,
    Status
FROM dbo.InboundOrders
WHERE DueDate < CONVERT(DATE, SYSUTCDATETIME()) AND Status NOT IN ('COMPLETED','CANCELLED')
UNION ALL
SELECT
    'OUTBOUND_ORDER', OutboundOrderID, OutboundOrderNumber,
    WarehouseID, DueDate,
    DATEDIFF(DAY, DueDate, CONVERT(DATE, SYSUTCDATETIME())), Status
FROM dbo.OutboundOrders
WHERE DueDate < CONVERT(DATE, SYSUTCDATETIME()) AND Status NOT IN ('COMPLETED','CANCELLED');
GO

CREATE OR ALTER VIEW dbo.vw_InboundReport
AS
SELECT
    io.InboundOrderID, io.InboundOrderNumber, io.SourceType,
    io.PurchaseOrderID, io.SalesOrderID, io.TransferOrderID, io.ParentInboundOrderID,
    io.WarehouseID, io.ExpectedReceiptDate, io.Status, io.ConfirmedAt,
    ioi.ProductID, p.ProductCode, p.ProductName,
    ioi.ExpectedQuantity, ioi.ReceivedQuantity, ioi.DamagedQuantity, ioi.ShortageQuantity
FROM dbo.InboundOrders io
JOIN dbo.InboundOrderItems ioi ON ioi.InboundOrderID = io.InboundOrderID
JOIN dbo.Products p ON p.ProductID = ioi.ProductID;
GO

CREATE OR ALTER VIEW dbo.vw_OutboundReport
AS
SELECT
    oo.OutboundOrderID, oo.OutboundOrderNumber, oo.SourceType,
    oo.SalesOrderID, oo.PurchaseOrderID, oo.TransferOrderID,
    oo.WarehouseID, oo.ExpectedIssueDate, oo.Status, oo.ConfirmedAt,
    ooi.ProductID, p.ProductCode, p.ProductName,
    ooi.RequestedQuantity, ooi.IssuedQuantity
FROM dbo.OutboundOrders oo
JOIN dbo.OutboundOrderItems ooi ON ooi.OutboundOrderID = oo.OutboundOrderID
JOIN dbo.Products p ON p.ProductID = ooi.ProductID;
GO

CREATE OR ALTER VIEW dbo.vw_InventoryInOutSummary
AS
SELECT
    CONVERT(DATE, t.TransactionAt) AS TransactionDate,
    sl.WarehouseID, t.ProductID, p.ProductCode, p.ProductName,
    SUM(CASE WHEN t.TransactionType IN ('INBOUND','TRANSFER_IN') THEN t.OnHandDelta ELSE 0 END) AS InboundQuantity,
    SUM(CASE WHEN t.TransactionType IN ('OUTBOUND','TRANSFER_OUT') THEN -t.OnHandDelta ELSE 0 END) AS OutboundQuantity,
    SUM(CASE WHEN t.TransactionType = 'STOCKTAKE_ADJUSTMENT' THEN t.OnHandDelta ELSE 0 END) AS AdjustmentQuantity,
    SUM(t.OnHandDelta) AS NetMovementQuantity
FROM dbo.InventoryTransactions t
JOIN dbo.Products p ON p.ProductID = t.ProductID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = t.StorageLocationID
GROUP BY CONVERT(DATE, t.TransactionAt), sl.WarehouseID, t.ProductID, p.ProductCode, p.ProductName;
GO

CREATE OR ALTER VIEW dbo.vw_StocktakeResults
AS
SELECT
    ss.StocktakeSessionID, ss.StocktakeNumber, ss.WarehouseID,
    ss.PlannedDate, ss.Status,
    sl.StorageLocationID, sl.LocationCode, stl.CountStatus,
    si.ProductID, p.ProductCode, p.ProductName,
    si.ProductLotID, pl.LotNumber,
    si.BookQuantity, si.CountedQuantity, si.DifferenceQuantity,
    si.AdjustmentQuantity, si.Resolution
FROM dbo.StocktakeSessions ss
JOIN dbo.StocktakeLocations stl ON stl.StocktakeSessionID = ss.StocktakeSessionID
JOIN dbo.StorageLocations sl ON sl.StorageLocationID = stl.StorageLocationID
LEFT JOIN dbo.StocktakeItems si
       ON si.StocktakeSessionID = stl.StocktakeSessionID
      AND si.StorageLocationID = stl.StorageLocationID
LEFT JOIN dbo.Products p ON p.ProductID = si.ProductID
LEFT JOIN dbo.ProductLots pl ON pl.ProductLotID = si.ProductLotID;
GO

CREATE OR ALTER VIEW dbo.vw_WarehouseDashboard
AS
SELECT
    w.WarehouseID, w.WarehouseCode, w.WarehouseName,
    COALESCE(stock.TotalOnHandQuantity, 0) AS TotalOnHandQuantity,
    COALESCE(stock.TotalAvailableQuantity, 0) AS TotalAvailableQuantity,
    COALESCE(stock.ProductCount, 0) AS ProductCountInStock,
    COALESCE(loc.BinCount, 0) AS BinCount,
    COALESCE(loc.OccupiedBinCount, 0) AS OccupiedBinCount,
    (SELECT COUNT(*) FROM dbo.InboundOrders io WHERE io.WarehouseID = w.WarehouseID AND io.Status NOT IN ('COMPLETED','CANCELLED')) AS PendingInboundCount,
    (SELECT COUNT(*) FROM dbo.OutboundOrders oo WHERE oo.WarehouseID = w.WarehouseID AND oo.Status NOT IN ('COMPLETED','CANCELLED')) AS PendingOutboundCount,
    (SELECT COUNT(*) FROM dbo.vw_LowStockAlerts a WHERE a.WarehouseID = w.WarehouseID) AS LowStockAlertCount,
    (SELECT COUNT(*) FROM dbo.vw_ExpiringLotAlerts a WHERE a.WarehouseID = w.WarehouseID) AS ExpiringLotAlertCount
FROM dbo.Warehouses w
OUTER APPLY
(
    SELECT SUM(i.OnHandQuantity) AS TotalOnHandQuantity,
           SUM(i.AvailableQuantity) AS TotalAvailableQuantity,
           COUNT(DISTINCT i.ProductID) AS ProductCount
    FROM dbo.Inventory i
    JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
    WHERE sl.WarehouseID = w.WarehouseID AND i.OnHandQuantity > 0
) stock
OUTER APPLY
(
    SELECT COUNT(*) AS BinCount,
           SUM(CASE WHEN Status = 'OCCUPIED' THEN 1 ELSE 0 END) AS OccupiedBinCount
    FROM dbo.StorageLocations
    WHERE WarehouseID = w.WarehouseID AND LocationType = 'BIN' AND Status <> 'INACTIVE'
) loc;
GO

CREATE OR ALTER VIEW dbo.vw_WarehouseKPI
AS
WITH InboundKPI AS
(
    SELECT WarehouseID,
           AVG(CASE WHEN Status = 'COMPLETED' THEN DATEDIFF(MINUTE, CreatedAt, ConfirmedAt) * 1.0 END) / 60.0 AS AverageHours,
           100.0 * SUM(CASE WHEN Status = 'COMPLETED' AND (DueDate IS NULL OR CONVERT(DATE, ConfirmedAt) <= DueDate) THEN 1 ELSE 0 END)
                 / NULLIF(SUM(CASE WHEN Status = 'COMPLETED' THEN 1 ELSE 0 END), 0) AS OnTimeRate
    FROM dbo.InboundOrders
    GROUP BY WarehouseID
), OutboundKPI AS
(
    SELECT WarehouseID,
           AVG(CASE WHEN Status = 'COMPLETED' THEN DATEDIFF(MINUTE, CreatedAt, ConfirmedAt) * 1.0 END) / 60.0 AS AverageHours,
           100.0 * SUM(CASE WHEN Status = 'COMPLETED' AND (DueDate IS NULL OR CONVERT(DATE, ConfirmedAt) <= DueDate) THEN 1 ELSE 0 END)
                 / NULLIF(SUM(CASE WHEN Status = 'COMPLETED' THEN 1 ELSE 0 END), 0) AS OnTimeRate
    FROM dbo.OutboundOrders
    GROUP BY WarehouseID
)
SELECT
    w.WarehouseID, w.WarehouseCode,
    CAST(ik.AverageHours AS DECIMAL(18,2)) AS AverageInboundProcessingHours,
    CAST(ok.AverageHours AS DECIMAL(18,2)) AS AverageOutboundProcessingHours,
    CAST(ik.OnTimeRate AS DECIMAL(6,2)) AS InboundOnTimeRate,
    CAST(ok.OnTimeRate AS DECIMAL(6,2)) AS OutboundOnTimeRate
FROM dbo.Warehouses w
LEFT JOIN InboundKPI ik ON ik.WarehouseID = w.WarehouseID
LEFT JOIN OutboundKPI ok ON ok.WarehouseID = w.WarehouseID;
GO

/*=============================================================================
 16. DU LIEU SEED: 5 VAI TRO CO DINH, DON VI TINH VA 63 QUYEN USE CASE
=============================================================================*/

MERGE dbo.Roles AS Target
USING
(
    VALUES
        ('SYSTEM_ADMIN',       N'Quản trị viên hệ thống', N'Quản lý người dùng, bảo mật và cấu hình hệ thống.'),
        ('WAREHOUSE_MANAGER',  N'Quản lý kho',            N'Lập phiếu, phê duyệt kiểm kho và theo dõi vận hành.'),
        ('WAREHOUSE_STAFF',    N'Nhân viên kho',           N'Thực hiện nhập, xuất, cất hàng và kiểm đếm.'),
        ('PURCHASING_STAFF',   N'Nhân viên mua hàng',      N'Quản lý nhà cung cấp và đơn mua hàng.'),
        ('SALES_STAFF',        N'Nhân viên bán hàng',      N'Kiểm tra tồn, quản lý đơn bán và yêu cầu xuất hàng.')
) AS Source(RoleCode, RoleName, Description)
ON Target.RoleCode = Source.RoleCode
WHEN MATCHED THEN
    UPDATE SET RoleName = Source.RoleName, Description = Source.Description, IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (RoleCode, RoleName, Description) VALUES (Source.RoleCode, Source.RoleName, Source.Description);
GO

MERGE dbo.UnitsOfMeasure AS Target
USING
(
    VALUES
        ('PCS', N'Cái'), ('SHEET', N'Tấm'), ('BAG', N'Bao'), ('BOX', N'Hộp'),
        ('KG', N'Kilôgam'), ('TON', N'Tấn'), ('M', N'Mét'), ('M2', N'Mét vuông'),
        ('M3', N'Mét khối'), ('L', N'Lít')
) AS Source(UnitCode, UnitName)
ON Target.UnitCode = Source.UnitCode
WHEN MATCHED THEN UPDATE SET UnitName = Source.UnitName, Status = 'ACTIVE'
WHEN NOT MATCHED THEN INSERT (UnitCode, UnitName) VALUES (Source.UnitCode, Source.UnitName);
GO

MERGE dbo.Permissions AS Target
USING
(
    VALUES
        ('UC01','M01',N'Xem thông tin kho'),
        ('UC02','M01',N'Xem sơ đồ vị trí lưu trữ'),
        ('UC03','M01',N'Tra cứu vị trí hàng hóa'),
        ('UC04','M02',N'Xem danh sách sản phẩm'),
        ('UC05','M02',N'Thêm sản phẩm'),
        ('UC06','M02',N'Chỉnh sửa sản phẩm'),
        ('UC07','M02',N'Xem chi tiết sản phẩm'),
        ('UC08','M02',N'Ngừng kinh doanh sản phẩm'),
        ('UC09','M02',N'Truy xuất lịch sử nhập xuất sản phẩm'),
        ('UC10','M03',N'Xem danh sách nhà cung cấp'),
        ('UC11','M03',N'Thêm nhà cung cấp'),
        ('UC12','M03',N'Chỉnh sửa nhà cung cấp'),
        ('UC13','M03',N'Xem chi tiết nhà cung cấp'),
        ('UC14','M03',N'Ngừng hợp tác nhà cung cấp'),
        ('UC15','M03',N'Xem sản phẩm do nhà cung cấp cung ứng'),
        ('UC16','M03',N'Xem lịch sử nhập hàng theo nhà cung cấp'),
        ('UC17','M04',N'Xem danh sách phiếu nhập'),
        ('UC18','M04',N'Tạo phiếu nhập'),
        ('UC19','M04',N'Chỉnh sửa phiếu nhập'),
        ('UC20','M04',N'Xem chi tiết phiếu nhập'),
        ('UC21','M04',N'Hủy phiếu nhập'),
        ('UC22','M04',N'Xác nhận nhập kho'),
        ('UC23','M04',N'Xác nhận cất hàng vào vị trí cố định'),
        ('UC24','M04',N'Xem lịch sử nhập kho'),
        ('UC25','M05',N'Xem danh sách phiếu xuất'),
        ('UC26','M05',N'Tạo phiếu xuất'),
        ('UC27','M05',N'Chỉnh sửa phiếu xuất'),
        ('UC28','M05',N'Xem chi tiết phiếu xuất'),
        ('UC29','M05',N'Hủy phiếu xuất'),
        ('UC30','M05',N'Kiểm tra và giữ tồn kho'),
        ('UC31','M05',N'Xác nhận xuất kho'),
        ('UC32','M05',N'Xem lịch sử xuất kho'),
        ('UC33','M06',N'Xem danh sách đợt kiểm kho'),
        ('UC34','M06',N'Thực hiện kiểm đếm'),
        ('UC35','M06',N'Ghi nhận chênh lệch tồn kho'),
        ('UC36','M06',N'Phê duyệt và điều chỉnh tồn kho'),
        ('UC37','M06',N'Xem lịch sử kiểm kho'),
        ('UC38','M07',N'Xem báo cáo tồn kho'),
        ('UC39','M07',N'Xem báo cáo nhập kho'),
        ('UC40','M07',N'Xem báo cáo xuất kho'),
        ('UC41','M07',N'Xem báo cáo nhập xuất tồn'),
        ('UC42','M07',N'Thống kê theo sản phẩm'),
        ('UC43','M07',N'Thống kê theo nhà cung cấp'),
        ('UC44','M07',N'Thống kê kết quả kiểm kho'),
        ('UC45','M07',N'Xuất báo cáo Excel/PDF'),
        ('UC46','M07',N'Xem Dashboard tổng quan'),
        ('UC47','M07',N'Xem cảnh báo tồn kho thấp'),
        ('UC48','M07',N'Xem cảnh báo hàng sắp hết hạn'),
        ('UC49','M07',N'Xem cảnh báo phiếu nhập/xuất quá hạn'),
        ('UC50','M07',N'Xem thông báo hệ thống'),
        ('UC51','M07',N'Xem KPI vận hành kho'),
        ('UC52','M08',N'Xem danh sách người dùng'),
        ('UC53','M08',N'Thêm người dùng'),
        ('UC54','M08',N'Chỉnh sửa người dùng'),
        ('UC55','M08',N'Xem chi tiết người dùng'),
        ('UC56','M08',N'Khóa/Mở khóa tài khoản'),
        ('UC57','M08',N'Gán vai trò cố định cho người dùng'),
        ('UC58','M08',N'Xem hồ sơ cá nhân'),
        ('UC59','M08',N'Cập nhật hồ sơ cá nhân'),
        ('UC60','M08',N'Đổi mật khẩu'),
        ('UC61','M08',N'Đăng nhập'),
        ('UC62','M08',N'Đăng xuất'),
        ('UC63','M08',N'Quên mật khẩu')
) AS Source(PermissionCode, ModuleCode, PermissionName)
ON Target.PermissionCode = Source.PermissionCode
WHEN MATCHED THEN
    UPDATE SET ModuleCode = Source.ModuleCode, PermissionName = Source.PermissionName, IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (PermissionCode, ModuleCode, PermissionName)
    VALUES (Source.PermissionCode, Source.ModuleCode, Source.PermissionName);
GO

/* Ma trận quyền mặc định; ứng dụng không cung cấp màn hình sửa vai trò cố định. */
INSERT dbo.RolePermissions (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.Roles r
CROSS JOIN dbo.Permissions p
WHERE r.RoleCode = 'SYSTEM_ADMIN'
  AND NOT EXISTS
      (SELECT 1 FROM dbo.RolePermissions x WHERE x.RoleID = r.RoleID AND x.PermissionID = p.PermissionID);

INSERT dbo.RolePermissions (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.Roles r
JOIN dbo.Permissions p ON p.ModuleCode IN ('M01','M02','M03','M04','M05','M06','M07')
WHERE r.RoleCode = 'WAREHOUSE_MANAGER'
  AND NOT EXISTS
      (SELECT 1 FROM dbo.RolePermissions x WHERE x.RoleID = r.RoleID AND x.PermissionID = p.PermissionID);

INSERT dbo.RolePermissions (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.Roles r
JOIN dbo.Permissions p ON p.PermissionCode IN
(
    'UC01','UC02','UC03','UC04','UC07','UC09',
    'UC17','UC20','UC22','UC23','UC24',
    'UC25','UC28','UC31','UC32',
    'UC33','UC34','UC35','UC37','UC50',
    'UC58','UC59','UC60','UC61','UC62','UC63'
)
WHERE r.RoleCode = 'WAREHOUSE_STAFF'
  AND NOT EXISTS
      (SELECT 1 FROM dbo.RolePermissions x WHERE x.RoleID = r.RoleID AND x.PermissionID = p.PermissionID);

INSERT dbo.RolePermissions (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.Roles r
JOIN dbo.Permissions p ON p.PermissionCode IN
(
    'UC04','UC07','UC09','UC10','UC11','UC12','UC13','UC14','UC15','UC16',
    'UC17','UC18','UC19','UC20','UC21','UC24','UC39','UC43','UC45','UC50',
    'UC58','UC59','UC60','UC61','UC62','UC63'
)
WHERE r.RoleCode = 'PURCHASING_STAFF'
  AND NOT EXISTS
      (SELECT 1 FROM dbo.RolePermissions x WHERE x.RoleID = r.RoleID AND x.PermissionID = p.PermissionID);

INSERT dbo.RolePermissions (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.Roles r
JOIN dbo.Permissions p ON p.PermissionCode IN
(
    'UC03','UC04','UC07','UC09','UC25','UC26','UC27','UC28','UC29','UC30','UC32',
    'UC38','UC40','UC41','UC42','UC45','UC46','UC47','UC48','UC49','UC50',
    'UC58','UC59','UC60','UC61','UC62','UC63'
)
WHERE r.RoleCode = 'SALES_STAFF'
  AND NOT EXISTS
      (SELECT 1 FROM dbo.RolePermissions x WHERE x.RoleID = r.RoleID AND x.PermissionID = p.PermissionID);
GO

PRINT N'BMWMS Database To-Be v3.0 đã được tạo/cập nhật thành công.';
GO
