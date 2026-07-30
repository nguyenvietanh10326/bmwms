USE [BMWMS];
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Create ProductGroups if missing
IF NOT EXISTS (SELECT 1 FROM [ProductGroups] WHERE GroupCode = 'PG-01')
BEGIN
    INSERT INTO [ProductGroups] (GroupCode, GroupName, Status, CreatedAt, UpdatedAt)
    VALUES ('PG-01', N'Vật liệu xây dựng', 'ACTIVE', GETUTCDATE(), GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM [ProductGroups] WHERE GroupCode = 'PG-02')
BEGIN
    INSERT INTO [ProductGroups] (GroupCode, GroupName, Status, CreatedAt, UpdatedAt)
    VALUES ('PG-02', N'Thiết bị điện nước', 'ACTIVE', GETUTCDATE(), GETUTCDATE());
END
GO

-- 2. Create Products if missing
DECLARE @GroupId1 BIGINT = (SELECT TOP 1 ProductGroupID FROM [ProductGroups] WHERE GroupCode = 'PG-01');
DECLARE @GroupId2 BIGINT = (SELECT TOP 1 ProductGroupID FROM [ProductGroups] WHERE GroupCode = 'PG-02');
DECLARE @UnitId INT = (SELECT TOP 1 UnitOfMeasureID FROM [UnitsOfMeasure] WHERE UnitCode = 'KG' OR UnitCode = 'TON' OR UnitOfMeasureID > 0);
DECLARE @UserId BIGINT = (SELECT TOP 1 UserID FROM [Users]);

IF NOT EXISTS (SELECT 1 FROM [Products] WHERE ProductCode = 'XM001')
    INSERT INTO [Products] (ProductGroupID, UnitOfMeasureID, ProductCode, ProductName, RotationMethod, TrackLot, TrackExpiry, Status, CreatedByUserId, CreatedAt)
    VALUES (@GroupId1, @UnitId, 'XM001', N'Xi măng PCB40', 'FIFO', 1, 1, 'ACTIVE', @UserId, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [Products] WHERE ProductCode = 'TH001')
    INSERT INTO [Products] (ProductGroupID, UnitOfMeasureID, ProductCode, ProductName, RotationMethod, TrackLot, TrackExpiry, Status, CreatedByUserId, CreatedAt)
    VALUES (@GroupId1, @UnitId, 'TH001', N'Thép cây D16', 'FIFO', 1, 0, 'ACTIVE', @UserId, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [Products] WHERE ProductCode = 'CD002')
    INSERT INTO [Products] (ProductGroupID, UnitOfMeasureID, ProductCode, ProductName, RotationMethod, TrackLot, TrackExpiry, Status, CreatedByUserId, CreatedAt)
    VALUES (@GroupId1, @UnitId, 'CD002', N'Đá 1x2 nghiền', 'FIFO', 0, 0, 'ACTIVE', @UserId, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [Products] WHERE ProductCode = 'GV001')
    INSERT INTO [Products] (ProductGroupID, UnitOfMeasureID, ProductCode, ProductName, RotationMethod, TrackLot, TrackExpiry, Status, CreatedByUserId, CreatedAt)
    VALUES (@GroupId1, @UnitId, 'GV001', N'Gạch Viglacera', 'FIFO', 1, 0, 'ACTIVE', @UserId, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM [Products] WHERE ProductCode = 'DX001')
    INSERT INTO [Products] (ProductGroupID, UnitOfMeasureID, ProductCode, ProductName, RotationMethod, TrackLot, TrackExpiry, Status, CreatedByUserId, CreatedAt)
    VALUES (@GroupId2, @UnitId, 'DX001', N'Dây cáp điện 2.5', 'FIFO', 0, 0, 'ACTIVE', @UserId, GETUTCDATE());
GO

-- 3. Clear existing SupplierProducts to avoid duplicates during massive insert (Optional, but safe for seeding)
-- DELETE FROM [SupplierProducts];

-- 4. Assign Products to ALL Suppliers dynamically
DECLARE @SupplierId BIGINT;
DECLARE @SupplierCode VARCHAR(50);
DECLARE @Product1Id BIGINT = (SELECT TOP 1 ProductID FROM [Products] WHERE ProductCode = 'XM001');
DECLARE @Product2Id BIGINT = (SELECT TOP 1 ProductID FROM [Products] WHERE ProductCode = 'TH001');
DECLARE @Product3Id BIGINT = (SELECT TOP 1 ProductID FROM [Products] WHERE ProductCode = 'CD002');
DECLARE @Product4Id BIGINT = (SELECT TOP 1 ProductID FROM [Products] WHERE ProductCode = 'GV001');
DECLARE @Product5Id BIGINT = (SELECT TOP 1 ProductID FROM [Products] WHERE ProductCode = 'DX001');

DECLARE supplier_cursor CURSOR FOR 
SELECT SupplierID, SupplierCode FROM [Suppliers];

OPEN supplier_cursor;
FETCH NEXT FROM supplier_cursor INTO @SupplierId, @SupplierCode;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Assign Product 1 (Xi măng)
    IF NOT EXISTS (SELECT 1 FROM [SupplierProducts] WHERE SupplierID = @SupplierId AND ProductID = @Product1Id)
        INSERT INTO [SupplierProducts] (SupplierID, ProductID, SupplierProductCode, LastPurchasePrice, LeadTimeDays, IsPreferred, Status)
        VALUES (@SupplierId, @Product1Id, @SupplierCode + '-XM001', 250, 2, 1, 'ACTIVE');

    -- Assign Product 2 (Thép)
    IF NOT EXISTS (SELECT 1 FROM [SupplierProducts] WHERE SupplierID = @SupplierId AND ProductID = @Product2Id)
        INSERT INTO [SupplierProducts] (SupplierID, ProductID, SupplierProductCode, LastPurchasePrice, LeadTimeDays, IsPreferred, Status)
        VALUES (@SupplierId, @Product2Id, @SupplierCode + '-TH001', 380, 5, 0, 'ACTIVE');

    -- Randomize some products based on SupplierID (Even/Odd) to make data look diverse
    IF @SupplierId % 2 = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [SupplierProducts] WHERE SupplierID = @SupplierId AND ProductID = @Product3Id)
            INSERT INTO [SupplierProducts] (SupplierID, ProductID, SupplierProductCode, LastPurchasePrice, LeadTimeDays, IsPreferred, Status)
            VALUES (@SupplierId, @Product3Id, @SupplierCode + '-CD002', 150, 3, 1, 'ACTIVE');
        
        IF NOT EXISTS (SELECT 1 FROM [SupplierProducts] WHERE SupplierID = @SupplierId AND ProductID = @Product4Id)
            INSERT INTO [SupplierProducts] (SupplierID, ProductID, SupplierProductCode, LastPurchasePrice, LeadTimeDays, IsPreferred, Status)
            VALUES (@SupplierId, @Product4Id, @SupplierCode + '-GV001', 120, 4, 0, 'INACTIVE');
    END
    ELSE
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [SupplierProducts] WHERE SupplierID = @SupplierId AND ProductID = @Product5Id)
            INSERT INTO [SupplierProducts] (SupplierID, ProductID, SupplierProductCode, LastPurchasePrice, LeadTimeDays, IsPreferred, Status)
            VALUES (@SupplierId, @Product5Id, @SupplierCode + '-DX001', 450, 7, 1, 'ACTIVE');
    END

    FETCH NEXT FROM supplier_cursor INTO @SupplierId, @SupplierCode;
END

CLOSE supplier_cursor;
DEALLOCATE supplier_cursor;
GO
