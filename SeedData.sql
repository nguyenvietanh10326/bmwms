USE [BMWMS];
GO

-- 1. Insert Roles
SET IDENTITY_INSERT dbo.Roles ON;
INSERT INTO dbo.Roles (RoleID, RoleCode, RoleName, Description, IsSystemRole, IsActive)
VALUES 
(1, 'ADMIN', N'Quản trị hệ thống', N'Toàn quyền', 1, 1),
(2, 'MANAGER', N'Quản lý kho', N'Quản lý chung', 1, 1),
(3, 'STAFF', N'Nhân viên kho', N'Thực hiện các thao tác kho', 1, 1);
SET IDENTITY_INSERT dbo.Roles OFF;

-- 2. Insert Users (Password: Admin@123)
SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users (UserID, RoleID, Username, Email, PasswordHash, FullName, PhoneNumber, Status)
VALUES 
(1, 1, 'admin', 'admin@bmwms.com', '\\\', N'Quản trị viên', '0987654321', 'ACTIVE'),
(2, 2, 'manager', 'manager@bmwms.com', '\\\', N'Trưởng Kho', '0987654322', 'ACTIVE'),
(3, 3, 'staff', 'staff@bmwms.com', '\\\', N'Nhân Viên Kho', '0987654323', 'ACTIVE');
SET IDENTITY_INSERT dbo.Users OFF;

-- 3. Insert Suppliers
SET IDENTITY_INSERT dbo.Suppliers ON;
INSERT INTO dbo.Suppliers (SupplierID, SupplierCode, SupplierName, PhoneNumber, Email, Address, RepresentativeName, TaxCode, CreatedByUserID)
VALUES 
(1, 'NCC001', N'Công ty Cổ phần Xi Măng Hà Tiên', '19001234', 'contact@hatien.com', N'Q.1, TP.HCM', N'Nguyễn Văn A', '0300123456', 1),
(2, 'NCC002', N'Tập đoàn Hòa Phát', '19002345', 'info@hoaphat.com', N'Cầu Giấy, Hà Nội', N'Trần Văn B', '0100234567', 1),
(3, 'NCC003', N'Đại lý Gạch Đồng Tâm', '19003456', 'sales@dongtam.com', N'Q.7, TP.HCM', N'Lê Thị C', '0300345678', 1);
SET IDENTITY_INSERT dbo.Suppliers OFF;

-- 4. Insert Warehouses
SET IDENTITY_INSERT dbo.Warehouses ON;
INSERT INTO dbo.Warehouses (WarehouseID, WarehouseCode, WarehouseName, Address, PhoneNumber, IsPrimary, Status)
VALUES 
(1, 'WH-MAIN', N'Kho Tổng Miền Nam', N'KCN Tân Bình, TP.HCM', '0281234567', 1, 'ACTIVE'),
(2, 'WH-SUB', N'Kho Phụ Bình Dương', N'KCN VSIP, Bình Dương', '0274234567', 0, 'ACTIVE');
SET IDENTITY_INSERT dbo.Warehouses OFF;

GO
