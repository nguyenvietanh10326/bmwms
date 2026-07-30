USE BMWMS;
GO

-- Lấy User Admin đầu tiên làm CreatedByUserId
DECLARE @AdminId BIGINT;
SELECT TOP 1 @AdminId = UserID FROM Users WHERE RoleID = (SELECT RoleID FROM Roles WHERE RoleCode = 'SYSTEM_ADMIN');

IF @AdminId IS NULL SET @AdminId = 1;

-- Lấy 3 sản phẩm mẫu (nếu có)
DECLARE @Prod1 BIGINT, @Prod2 BIGINT, @Prod3 BIGINT;
SELECT TOP 1 @Prod1 = ProductID FROM Products ORDER BY ProductID;
SELECT TOP 1 @Prod2 = ProductID FROM Products WHERE ProductID > ISNULL(@Prod1, 0) ORDER BY ProductID;
SELECT TOP 1 @Prod3 = ProductID FROM Products WHERE ProductID > ISNULL(@Prod2, 0) ORDER BY ProductID;

-- UPDATE lại dữ liệu nếu đã tồn tại để fix lỗi font (lần trước chèn bị lỗi encode)
IF EXISTS(SELECT 1 FROM Suppliers WHERE SupplierCode = 'SUP-001')
BEGIN
    UPDATE Suppliers SET SupplierName = N'Xi măng Hà Tiên', Address = N'KCN Biên Hòa, Đồng Nai', RepresentativeName = N'Nguyễn Văn Minh' WHERE SupplierCode = 'SUP-001';
    UPDATE Suppliers SET SupplierName = N'VLXD Thăng Long', Address = N'123 Láng Hạ, Hà Nội', RepresentativeName = N'Bùi Văn Nam' WHERE SupplierCode = 'SUP-002';
    UPDATE Suppliers SET SupplierName = N'Xi măng Nghi Sơn', Address = N'Nghi Sơn, Thanh Hóa', RepresentativeName = N'Đỗ Thanh Tùng' WHERE SupplierCode = 'SUP-003';
    UPDATE Suppliers SET SupplierName = N'Thép Hòa Phát', Address = N'KCN Phố Nối, Hưng Yên', RepresentativeName = N'Trần Quốc Dũng' WHERE SupplierCode = 'SUP-004';
    UPDATE Suppliers SET SupplierName = N'Gạch Viglacera', Address = N'Tiên Sơn, Bắc Ninh', RepresentativeName = N'Phạm Quang' WHERE SupplierCode = 'SUP-006';
    UPDATE Suppliers SET SupplierName = N'Cát Đá Sông Hồng', Address = N'Cảng Khuyến Lương, Hà Nội', RepresentativeName = N'Lê Thu Hà' WHERE SupplierCode = 'SUP-008';
    
    PRINT N'Đã cập nhật lại thông tin Nhà cung cấp để fix lỗi font Tiếng Việt!';
END
ELSE
BEGIN
    INSERT INTO Suppliers (SupplierCode, SupplierName, PhoneNumber, Email, Address, RepresentativeName, TaxCode, Status, CreatedByUserId, CreatedAt)
    VALUES 
    ('SUP-001', N'Xi măng Hà Tiên', '0912345678', 'contact@hatien.vn', N'KCN Biên Hòa, Đồng Nai', N'Nguyễn Văn Minh', '0101234567', 'ACTIVE', @AdminId, GETDATE()),
    ('SUP-002', N'VLXD Thăng Long', '0987654321', 'info@thanglong.com.vn', N'123 Láng Hạ, Hà Nội', N'Bùi Văn Nam', '0109876543', 'INACTIVE', @AdminId, GETDATE()),
    ('SUP-003', N'Xi măng Nghi Sơn', '0901112233', 'sales@nghison.vn', N'Nghi Sơn, Thanh Hóa', N'Đỗ Thanh Tùng', '0104445556', 'ACTIVE', @AdminId, GETDATE()),
    ('SUP-004', N'Thép Hòa Phát', '0977888999', 'hoaphat@hoaphat.com.vn', N'KCN Phố Nối, Hưng Yên', N'Trần Quốc Dũng', '0109998887', 'ACTIVE', @AdminId, GETDATE()),
    ('SUP-006', N'Gạch Viglacera', '0933444555', 'viglacera@viglacera.vn', N'Tiên Sơn, Bắc Ninh', N'Phạm Quang', '0105556667', 'ACTIVE', @AdminId, GETDATE()),
    ('SUP-008', N'Cát Đá Sông Hồng', '0966777888', 'catdasonghong@gmail.com', N'Cảng Khuyến Lương, Hà Nội', N'Lê Thu Hà', '0102223334', 'ACTIVE', @AdminId, GETDATE());
    
    PRINT N'Đã chèn 6 nhà cung cấp mẫu thành công!';
    
    -- Chèn thử vào SupplierProducts để đếm số sản phẩm
    DECLARE @Sup1 BIGINT, @Sup2 BIGINT;
    SELECT @Sup1 = SupplierId FROM Suppliers WHERE SupplierCode = 'SUP-001';
    SELECT @Sup2 = SupplierId FROM Suppliers WHERE SupplierCode = 'SUP-004';
    
    IF @Prod1 IS NOT NULL AND @Sup1 IS NOT NULL INSERT INTO SupplierProducts (SupplierId, ProductId) VALUES (@Sup1, @Prod1);
    IF @Prod2 IS NOT NULL AND @Sup1 IS NOT NULL INSERT INTO SupplierProducts (SupplierId, ProductId) VALUES (@Sup1, @Prod2);
    IF @Prod3 IS NOT NULL AND @Sup2 IS NOT NULL INSERT INTO SupplierProducts (SupplierId, ProductId) VALUES (@Sup2, @Prod3);
        
    PRINT N'Đã chèn dữ liệu liên kết Nhà cung cấp - Sản phẩm!';
END
