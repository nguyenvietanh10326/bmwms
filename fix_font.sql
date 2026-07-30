USE BMWMS;
GO

UPDATE Suppliers SET SupplierName = N'Xi măng Hà Tiên', Address = N'KCN Biên Hòa, Đồng Nai', RepresentativeName = N'Nguyễn Văn Minh' WHERE SupplierCode = 'SUP-001';
UPDATE Suppliers SET SupplierName = N'VLXD Thăng Long', Address = N'123 Láng Hạ, Hà Nội', RepresentativeName = N'Bùi Văn Nam' WHERE SupplierCode = 'SUP-002';
UPDATE Suppliers SET SupplierName = N'Xi măng Nghi Sơn', Address = N'Nghi Sơn, Thanh Hóa', RepresentativeName = N'Đỗ Thanh Tùng' WHERE SupplierCode = 'SUP-003';
UPDATE Suppliers SET SupplierName = N'Thép Hòa Phát', Address = N'KCN Phố Nối, Hưng Yên', RepresentativeName = N'Trần Quốc Dũng' WHERE SupplierCode = 'SUP-004';
UPDATE Suppliers SET SupplierName = N'Gạch Viglacera', Address = N'Tiên Sơn, Bắc Ninh', RepresentativeName = N'Phạm Quang' WHERE SupplierCode = 'SUP-006';
UPDATE Suppliers SET SupplierName = N'Cát Đá Sông Hồng', Address = N'Cảng Khuyến Lương, Hà Nội', RepresentativeName = N'Lê Thu Hà' WHERE SupplierCode = 'SUP-008';

PRINT N'Đã sửa lỗi font thành công!';
