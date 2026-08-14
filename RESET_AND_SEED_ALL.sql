-- ==============================================================================
-- BMWMS - MASTER RESET & SEED ALL DATA SCRIPT (Chuẩn Hóa Toàn Bộ Dữ Liệu Test)
-- Phiên bản: To-Be v3.0 | Chuẩn hóa 15 Nhóm VLXD, 1 Kho Tổng, Full Tài Khoản Actor
-- Hướng dẫn: Mở trong SQL Server Management Studio (SSMS) và nhấn F5 (Execute)
-- ==============================================================================

USE [BMWMS];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 1: VÔ HIỆU HÓA TOÀN BỘ TRIGGER & KHÓA NGOẠI (Foreign Keys)';
PRINT N'======================================================================';

-- Vô hiệu hóa toàn bộ Trigger trên tất cả các bảng để xóa và nạp không bị chặn
EXEC sp_MSforeachtable 'ALTER TABLE ? DISABLE TRIGGER ALL';
-- Vô hiệu hóa toàn bộ ràng buộc khóa ngoại (Foreign Keys)
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 2: XÓA SẠCH DỮ LIỆU CŨ & RESET BỘ ĐẾM IDENTITY VỀ 0';
PRINT N'======================================================================';

DELETE FROM dbo.AuditLogs;
DELETE FROM dbo.Notifications;
DELETE FROM dbo.InventoryTransactions;
DELETE FROM dbo.Inventory;
DELETE FROM dbo.StocktakeLocations;
DELETE FROM dbo.StocktakeItems;
DELETE FROM dbo.StocktakeSessions;
DELETE FROM dbo.StocktakeSchedules;
DELETE FROM dbo.OutboundOrderDetails;
DELETE FROM dbo.OutboundOrderItems;
DELETE FROM dbo.OutboundOrders;
DELETE FROM dbo.SalesOrderDetails;
DELETE FROM dbo.SalesOrders;
DELETE FROM dbo.InboundOrderDetails;
DELETE FROM dbo.InboundOrderItems;
DELETE FROM dbo.InboundOrders;
DELETE FROM dbo.PurchaseOrderDetails;
DELETE FROM dbo.PurchaseOrders;
DELETE FROM dbo.TransferOrderDetails;
DELETE FROM dbo.TransferOrders;
DELETE FROM dbo.InventoryReservations;
DELETE FROM dbo.SupplierProducts;
DELETE FROM dbo.Suppliers;
DELETE FROM dbo.Customers;
DELETE FROM dbo.ProductAttributeValues;
DELETE FROM dbo.ProductFixedLocations;
DELETE FROM dbo.ProductWarehousePolicies;
DELETE FROM dbo.ProductLots;
DELETE FROM dbo.Products;
DELETE FROM dbo.ProductGroupAttributes;
DELETE FROM dbo.ProductAttributeOptions;
DELETE FROM dbo.ProductAttributes;
DELETE FROM dbo.ProductGroups;
DELETE FROM dbo.StorageLocations;
DELETE FROM dbo.StorageRacks;
DELETE FROM dbo.WarehouseZones;
DELETE FROM dbo.Warehouses;
DELETE FROM dbo.UnitsOfMeasure;
DELETE FROM dbo.PasswordResetTokens;
DELETE FROM dbo.UserSessions;
DELETE FROM dbo.RolePermissions;
DELETE FROM dbo.Users;
DELETE FROM dbo.Permissions;
DELETE FROM dbo.Roles;
GO

-- Reset Identity về 0 cho tất cả các bảng có cột tự tăng
DBCC CHECKIDENT ('dbo.Roles', RESEED, 0);
DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
DBCC CHECKIDENT ('dbo.Warehouses', RESEED, 0);
DBCC CHECKIDENT ('dbo.WarehouseZones', RESEED, 0);
DBCC CHECKIDENT ('dbo.StorageRacks', RESEED, 0);
DBCC CHECKIDENT ('dbo.StorageLocations', RESEED, 0);
DBCC CHECKIDENT ('dbo.UnitsOfMeasure', RESEED, 0);
DBCC CHECKIDENT ('dbo.ProductGroups', RESEED, 0);
DBCC CHECKIDENT ('dbo.ProductAttributes', RESEED, 0);
DBCC CHECKIDENT ('dbo.ProductAttributeOptions', RESEED, 0);
DBCC CHECKIDENT ('dbo.Products', RESEED, 0);
DBCC CHECKIDENT ('dbo.ProductLots', RESEED, 0);
DBCC CHECKIDENT ('dbo.Suppliers', RESEED, 0);
DBCC CHECKIDENT ('dbo.Customers', RESEED, 0);
DBCC CHECKIDENT ('dbo.PurchaseOrders', RESEED, 0);
DBCC CHECKIDENT ('dbo.PurchaseOrderDetails', RESEED, 0);
DBCC CHECKIDENT ('dbo.InboundOrders', RESEED, 0);
DBCC CHECKIDENT ('dbo.InboundOrderItems', RESEED, 0);
DBCC CHECKIDENT ('dbo.InboundOrderDetails', RESEED, 0);
DBCC CHECKIDENT ('dbo.Inventory', RESEED, 0);
DBCC CHECKIDENT ('dbo.InventoryTransactions', RESEED, 0);
GO

PRINT N'>>> Đã xóa sạch dữ liệu và reset identity thành công!';
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 3: SEED ROLES & FULL TÀI KHOẢN ACTORS (Password: Admin@123456)';
PRINT N'======================================================================';

-- 3.1. Roles
SET IDENTITY_INSERT dbo.Roles ON;
INSERT INTO dbo.Roles (RoleID, RoleCode, RoleName, Description, IsSystemRole, IsActive, CreatedAt)
VALUES 
(1, 'SYSTEM_ADMIN', N'Quản trị viên hệ thống', N'Toàn quyền quản trị tài khoản, phân quyền, cấu hình hệ thống', 1, 1, SYSUTCDATETIME()),
(2, 'WAREHOUSE_MANAGER', N'Quản lý kho (Trưởng kho)', N'Quản lý nhập/xuất/tồn, phê duyệt phiếu, quản lý vị trí và điều phối kho', 1, 1, SYSUTCDATETIME()),
(3, 'WAREHOUSE_STAFF', N'Nhân viên kho', N'Thực hiện kiểm đếm, soạn hàng, xếp dỡ, gán vị trí và thực thi phiếu', 1, 1, SYSUTCDATETIME()),
(4, 'ACCOUNTANT', N'Kế toán kho', N'Quản lý công nợ, giá trị tồn kho, hóa đơn và đối soát dữ liệu', 1, 1, SYSUTCDATETIME()),
(5, 'DIRECTOR', N'Ban Giám Đốc', N'Xem báo cáo KPI, biểu đồ tổng quan, giám sát vận hành toàn diện', 1, 1, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Roles OFF;
GO

-- 3.2. Users (Tất cả tài khoản đều dùng Password: Admin@123456)
-- Hash BCrypt chuẩn workFactor=12: $2a$12$i8Ub0QV2UyniLOX0yRql6uaaIY36sK6v17dh0BCLy8HiOBx0d8AR.
SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users (UserID, RoleID, Username, Email, PasswordHash, FullName, PhoneNumber, AvatarUrl, Status, FailedLoginCount, CreatedAt)
VALUES 
-- 5 Tài khoản Actor chính (Đăng nhập ngắn gọn bằng username hoặc email)
(1, 1, 'admin', 'admin@bmwms.vn', '$2a$12$i8Ub0QV2UyniLOX0yRql6uaaIY36sK6v17dh0BCLy8HiOBx0d8AR.', N'Quản trị viên Hệ thống', '0900000001', NULL, 'ACTIVE', 0, SYSUTCDATETIME()),
(2, 2, 'manager', 'manager@bmwms.vn', '$2a$12$i8Ub0QV2UyniLOX0yRql6uaaIY36sK6v17dh0BCLy8HiOBx0d8AR.', N'Hoàng Minh (Trưởng Kho)', '0900000002', NULL, 'ACTIVE', 0, SYSUTCDATETIME()),
(3, 3, 'staff', 'staff@bmwms.vn', '$2a$12$i8Ub0QV2UyniLOX0yRql6uaaIY36sK6v17dh0BCLy8HiOBx0d8AR.', N'Nguyễn Văn An (Nhân viên Kho)', '0900000003', NULL, 'ACTIVE', 0, SYSUTCDATETIME()),
(4, 4, 'accountant', 'accountant@bmwms.vn', '$2a$12$i8Ub0QV2UyniLOX0yRql6uaaIY36sK6v17dh0BCLy8HiOBx0d8AR.', N'Trần Thị Bình (Kế toán Kho)', '0900000004', NULL, 'ACTIVE', 0, SYSUTCDATETIME()),
(5, 5, 'director', 'director@bmwms.vn', '$2a$12$i8Ub0QV2UyniLOX0yRql6uaaIY36sK6v17dh0BCLy8HiOBx0d8AR.', N'Phạm Thị Cúc (Giám đốc Điều hành)', '0900000005', NULL, 'ACTIVE', 0, SYSUTCDATETIME()),

-- Các tài khoản bổ sung phục vụ test nghiệp vụ UC52 (Xem danh sách, tìm kiếm, lọc trạng thái Locked/Inactive)
(6, 3, 'le.van.dat', 'le.van.dat@bmwms.vn', '$2a$12$i8Ub0QV2UyniLOX0yRql6uaaIY36sK6v17dh0BCLy8HiOBx0d8AR.', N'Lê Văn Đạt (NV Kho - Bị khóa)', '0900000006', NULL, 'LOCKED', 5, SYSUTCDATETIME()),
(7, 2, 'vo.thi.em', 'vo.thi.em@bmwms.vn', '$2a$12$i8Ub0QV2UyniLOX0yRql6uaaIY36sK6v17dh0BCLy8HiOBx0d8AR.', N'Võ Thị Em (Phó Kho - Tạm ngưng)', '0900000007', NULL, 'INACTIVE', 0, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Users OFF;
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 4: SEED 1 KHO DUY NHẤT, KHU VỰC & VỊ TRÍ LƯU TRỮ';
PRINT N'======================================================================';

-- 4.1. Kho Tổng Duy Nhất
SET IDENTITY_INSERT dbo.Warehouses ON;
INSERT INTO dbo.Warehouses (WarehouseID, WarehouseCode, WarehouseName, Address, PhoneNumber, IsPrimary, Status, CreatedAt)
VALUES (1, 'WH01', N'Kho Tổng Vật Liệu Xây Dựng Miền Nam', N'KCN Tân Bình, P. Tây Thạnh, Q. Tân Phú, TP. Hồ Chí Minh', '02838123456', 1, 'ACTIVE', SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Warehouses OFF;
GO

-- 4.2. Khu vực trong kho (Warehouse Zones)
SET IDENTITY_INSERT dbo.WarehouseZones ON;
INSERT INTO dbo.WarehouseZones (ZoneID, WarehouseID, ZoneCode, ZoneName, Description, Status)
VALUES
(1, 1, 'ZONE-A', N'Khu A - Kho khô (Xi măng, Thạch cao)', N'Kho có mái che, chống ẩm, lưu trữ pallet xi măng', 'ACTIVE'),
(2, 1, 'ZONE-B', N'Khu B - Giá thép & Bãi thép dài', N'Hệ thống kệ đỡ thép cây, bãi chứa thép cuộn', 'ACTIVE'),
(3, 1, 'ZONE-C', N'Khu C - Giá tôn & Tấm lợp', N'Giá đỡ tôn đứng và xếp phẳng theo kích thước', 'ACTIVE'),
(4, 1, 'ZONE-D', N'Khu D - Pallet Gạch đá & Ốp lát', N'Khu vực tập kết pallet gạch ceramic, gạch xây', 'ACTIVE'),
(5, 1, 'ZONE-E', N'Khu E - Bãi vật liệu rời (Cát, Đá)', N'Bãi ngoài trời có vách ngăn chứa cát vàng, cát đen, đá 1x2', 'ACTIVE'),
(6, 1, 'ZONE-F', N'Khu F - Kho hóa chất, Sơn & Chống thấm (FEFO)', N'Kho thông gió, kiểm soát hạn sử dụng, chống cháy nổ', 'ACTIVE'),
(7, 1, 'ZONE-G', N'Khu G - Giá để ống nước & Phụ kiện', N'Giá treo và kệ chia ngăn ống PVC, PPR, HDPE', 'ACTIVE'),
(8, 1, 'ZONE-H', N'Khu H - Kệ thiết bị điện & Ngũ kim', N'Kệ Selective chia tầng chứa dây điện, bu lông, phụ kiện', 'ACTIVE'),
(9, 1, 'ZONE-RECV', N'Khu vực Tiếp nhận hàng (Inbound Receiving)', N'Khu vực bốc dỡ và kiểm tra quy cách trước khi nhập kho', 'ACTIVE'),
(10, 1, 'ZONE-DISP', N'Khu vực Soạn hàng & Xuất kho (Dispatch/Staging)', N'Khu vực tập kết hàng chờ xuất xe tải', 'ACTIVE');
SET IDENTITY_INSERT dbo.WarehouseZones OFF;
GO

-- 4.3. Dãy kệ (Storage Racks)
SET IDENTITY_INSERT dbo.StorageRacks ON;
INSERT INTO dbo.StorageRacks (RackID, WarehouseID, ZoneID, RackCode, RackName, Status)
VALUES
(1, 1, 1, 'RACK-A1', N'Dãy Pallet Xi măng A1', 'ACTIVE'),
(2, 1, 1, 'RACK-A2', N'Dãy Pallet Xi măng A2', 'ACTIVE'),
(3, 1, 2, 'RACK-B1', N'Kệ thép cây D10-D20 B1', 'ACTIVE'),
(4, 1, 4, 'RACK-D1', N'Dãy Pallet Gạch 600x600 D1', 'ACTIVE'),
(5, 1, 6, 'RACK-F1', N'Kệ Sơn nội/ngoại thất F1', 'ACTIVE'),
(6, 1, 6, 'RACK-F2', N'Kệ Keo & Phụ gia hóa chất F2', 'ACTIVE'),
(7, 1, 7, 'RACK-G1', N'Giá đỡ Ống uPVC Tiền Phong G1', 'ACTIVE'),
(8, 1, 8, 'RACK-H1', N'Kệ Thiết bị điện & Dây cáp H1', 'ACTIVE');
SET IDENTITY_INSERT dbo.StorageRacks OFF;
GO

-- 4.4. Vị trí lưu trữ (Storage Locations)
SET IDENTITY_INSERT dbo.StorageLocations ON;
INSERT INTO dbo.StorageLocations (StorageLocationID, WarehouseID, RackID, LocationCode, LocationName, LocationType, AreaSquareMeter, MaxWeightKg, MaxVolumeM3, IsPutawayAllowed, IsPickable, Status, CreatedAt)
VALUES
-- Vị trí tiếp nhận & xuất hàng
(1, 1, NULL, 'LOC-RECEIVING-01', N'Cửa nhập số 1 (Dock 1)', 'RECEIVING', 50.0, 50000.0, 150.0, 1, 0, 'AVAILABLE', SYSUTCDATETIME()),
(2, 1, NULL, 'LOC-STAGING-01', N'Sân đệm kiểm hàng số 1', 'STAGING', 40.0, 30000.0, 100.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),
(3, 1, NULL, 'LOC-DISPATCH-01', N'Cửa xuất hàng số 1', 'DISPATCH', 50.0, 50000.0, 150.0, 0, 1, 'AVAILABLE', SYSUTCDATETIME()),

-- Vị trí lưu trữ Khu A (Xi măng)
(4, 1, 1, 'LOC-A1-01', N'Kệ A1 - Tầng 1 (Pallet XM)', 'BIN', 10.0, 10000.0, 20.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),
(5, 1, 1, 'LOC-A1-02', N'Kệ A1 - Tầng 2 (Pallet XM)', 'BIN', 10.0, 10000.0, 20.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),
(6, 1, 2, 'LOC-A2-01', N'Kệ A2 - Tầng 1 (Pallet XM)', 'BIN', 10.0, 10000.0, 20.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),

-- Vị trí lưu trữ Khu B (Thép)
(7, 1, 3, 'LOC-B1-01', N'Giá thép B1 - Ngăn 1 (D10-D16)', 'BIN', 20.0, 20000.0, 30.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),
(8, 1, 3, 'LOC-B1-02', N'Giá thép B1 - Ngăn 2 (D18-D25)', 'BIN', 20.0, 20000.0, 30.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),

-- Vị trí lưu trữ Khu D (Gạch)
(9, 1, 4, 'LOC-D1-01', N'Kệ Gạch D1 - Ô 1', 'BIN', 8.0, 8000.0, 15.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),
(10, 1, 4, 'LOC-D1-02', N'Kệ Gạch D1 - Ô 2', 'BIN', 8.0, 8000.0, 15.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),

-- Vị trí Bãi Cát Đá Khu E
(11, 1, NULL, 'LOC-E-SAND-01', N'Hộc chứa Cát vàng xây tô', 'BIN', 50.0, 100000.0, 80.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),
(12, 1, NULL, 'LOC-E-STONE-01', N'Hộc chứa Đá 1x2 bê tông', 'BIN', 50.0, 100000.0, 80.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),

-- Vị trí Kho Hóa chất / Sơn Khu F (FEFO)
(13, 1, 5, 'LOC-F1-01', N'Kệ Sơn F1 - Tầng 1 (Sơn thùng)', 'BIN', 6.0, 4000.0, 12.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),
(14, 1, 5, 'LOC-F1-02', N'Kệ Sơn F1 - Tầng 2 (Sơn lon)', 'BIN', 6.0, 3000.0, 10.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),
(15, 1, 6, 'LOC-F2-01', N'Kệ Keo F2 - Tầng 1 (Sika Ceram)', 'BIN', 6.0, 5000.0, 12.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),

-- Vị trí Ống & Thiết bị điện Khu G, H
(16, 1, 7, 'LOC-G1-01', N'Giá ống Tiền Phong G1', 'BIN', 15.0, 5000.0, 25.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME()),
(17, 1, 8, 'LOC-H1-01', N'Kệ Dây điện Cadivi H1', 'BIN', 5.0, 2000.0, 8.0, 1, 1, 'AVAILABLE', SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.StorageLocations OFF;
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 5: SEED ĐƠN VỊ TÍNH (UnitsOfMeasure) & 15 NHÓM VLXD';
PRINT N'======================================================================';

-- 5.1. UnitsOfMeasure
SET IDENTITY_INSERT dbo.UnitsOfMeasure ON;
INSERT INTO dbo.UnitsOfMeasure (UnitOfMeasureID, UnitCode, UnitName, Status)
VALUES
(1, 'BAO', N'Bao (50kg)', 'ACTIVE'),
(2, 'KG', N'Kilogram', 'ACTIVE'),
(3, 'TAN', N'Tấn (1000kg)', 'ACTIVE'),
(4, 'CAY', N'Cây (Thanh 11.7m)', 'ACTIVE'),
(5, 'CUON', N'Cuộn', 'ACTIVE'),
(6, 'M2', N'Mét vuông (m²)', 'ACTIVE'),
(7, 'M3', N'Mét khối (m³)', 'ACTIVE'),
(8, 'THUNG', N'Thùng (18L)', 'ACTIVE'),
(9, 'LON', N'Lon / Hộp (5L/1L)', 'ACTIVE'),
(10, 'CAN', N'Can (25L/5L)', 'ACTIVE'),
(11, 'TUYP', N'Tuýp (Chai keo)', 'ACTIVE'),
(12, 'ONG', N'Ống (Cây 4m)', 'ACTIVE'),
(13, 'CAI', N'Cái / Chiếc', 'ACTIVE'),
(14, 'TAM', N'Tấm / Khổ', 'ACTIVE');
SET IDENTITY_INSERT dbo.UnitsOfMeasure OFF;
GO

-- 5.2. 15 Nhóm Ngành Hàng VLXD Chuẩn (PG01 -> PG15)
SET IDENTITY_INSERT dbo.ProductGroups ON;
INSERT INTO dbo.ProductGroups (ProductGroupID, GroupCode, GroupName, Description, Status, CreatedAt)
VALUES
(1, 'PG01', N'Xi măng', N'Xi măng PCB30, PCB40, xi măng trắng các loại (Khu A – Kho khô, pallet; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(2, 'PG02', N'Thép xây dựng', N'Thép cuộn, thép thanh D10, D16, D20 (Khu B – Giá thép dài; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(3, 'PG03', N'Tôn và vật liệu lợp', N'Tôn lạnh, tôn màu, tấm lợp các loại (Khu C – Giá tôn; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(4, 'PG04', N'Gạch xây và gạch ốp lát', N'Gạch đỏ, gạch block, gạch ceramic, gạch granite (Khu D – Pallet gạch; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(5, 'PG05', N'Cát, đá và vật liệu rời', N'Cát vàng, cát đen, đá 1x2, sỏi xây dựng (Khu E – Bãi vật liệu rời; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(6, 'PG06', N'Sơn và chất phủ', N'Sơn nội thất, sơn ngoại thất, sơn lót, sơn chống thấm (Khu F – Kho hóa chất; FEFO, Bắt buộc Lô & HSD)', 'ACTIVE', SYSUTCDATETIME()),
(7, 'PG07', N'Keo và vật liệu chống thấm', N'Keo dán gạch, silicone, màng chống thấm (Khu G – Kho khô/hóa chất; FEFO, Bắt buộc Lô & HSD)', 'ACTIVE', SYSUTCDATETIME()),
(8, 'PG08', N'Phụ gia bê tông và hóa chất', N'Phụ gia đông kết nhanh, chống thấm, bảo dưỡng (Khu H – Kho hóa chất; FEFO, Bắt buộc Lô & HSD)', 'ACTIVE', SYSUTCDATETIME()),
(9, 'PG09', N'Ống và phụ kiện cấp thoát nước', N'Ống PVC, PPR, HDPE, cút, tê, van khóa (Khu I – Giá ống & phụ kiện; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(10, 'PG10', N'Thiết bị điện', N'Dây điện, cáp điện, aptomat, ổ cắm, công tắc (Khu J – Kệ thiết bị điện; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(11, 'PG11', N'Thiết bị vệ sinh', N'Bồn cầu, lavabo, vòi nước, sen tắm (Khu K – Kệ hàng dễ vỡ; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(12, 'PG12', N'Gỗ, ván và vật liệu nội thất', N'Ván MDF, ván ép, gỗ thanh, ván dăm (Khu L – Kho khô; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(13, 'PG13', N'Kính và cửa', N'Kính cường lực, cửa nhôm kính, cửa nhựa (Khu M – Giá dựng đứng; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(14, 'PG14', N'Ngũ kim và phụ kiện xây dựng', N'Đinh, vít, bu lông, bản lề, khóa (Khu N – Kệ linh kiện nhỏ; FIFO)', 'ACTIVE', SYSUTCDATETIME()),
(15, 'PG15', N'Dụng cụ và thiết bị thi công', N'Máy khoan, máy cắt, bay, xẻng, thước đo (Khu O – Kệ dụng cụ; FIFO)', 'ACTIVE', SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.ProductGroups OFF;
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 6: SEED 41 THUỘC TÍNH KỸ THUẬT (EAV) & OPTIONS & MAPPING';
PRINT N'======================================================================';

-- 6.1. ProductAttributes
SET IDENTITY_INSERT dbo.ProductAttributes ON;
INSERT INTO dbo.ProductAttributes (ProductAttributeID, AttributeCode, AttributeName, DataType, UnitLabel, Description, Status)
VALUES
(1, 'BRAND', N'Thương hiệu / Nhà sản xuất', 'TEXT', NULL, N'Tên thương hiệu hoặc nhà sản xuất', 'ACTIVE'),
(2, 'CEMENT_TYPE', N'Loại xi măng', 'OPTION', NULL, N'Chủng loại xi măng', 'ACTIVE'),
(3, 'BAG_WEIGHT', N'Khối lượng bao', 'NUMBER', N'kg', N'Khối lượng tịnh của bao', 'ACTIVE'),
(4, 'CEMENT_GRADE', N'Mác xi măng', 'OPTION', NULL, N'Cường độ nén của xi măng', 'ACTIVE'),
(5, 'DIAMETER', N'Đường kính', 'NUMBER', N'mm', N'Đường kính tiết diện', 'ACTIVE'),
(6, 'LENGTH', N'Chiều dài', 'NUMBER', N'm', N'Chiều dài tiêu chuẩn', 'ACTIVE'),
(7, 'WIDTH', N'Chiều rộng', 'NUMBER', N'm', N'Chiều rộng khổ tấm/cuộn', 'ACTIVE'),
(8, 'THICKNESS', N'Độ dày', 'NUMBER', N'mm', N'Độ dày của vật tư', 'ACTIVE'),
(9, 'STEEL_STANDARD', N'Tiêu chuẩn thép', 'OPTION', NULL, N'Tiêu chuẩn kỹ thuật thép', 'ACTIVE'),
(10, 'STEEL_GRADE', N'Mác thép', 'OPTION', NULL, N'Mác thép cường độ', 'ACTIVE'),
(11, 'COLOR', N'Màu sắc', 'TEXT', NULL, N'Màu sắc hoàn thiện', 'ACTIVE'),
(12, 'COATING', N'Lớp mạ / Lớp phủ', 'TEXT', NULL, N'Quy cách lớp mạ kẽm, mạ màu', 'ACTIVE'),
(13, 'DIMENSION', N'Kích thước quy cách', 'TEXT', N'mm', N'Kích thước quy chuẩn (DxRxC)', 'ACTIVE'),
(14, 'SURFACE_TYPE', N'Loại bề mặt', 'OPTION', NULL, N'Đặc tính bề mặt', 'ACTIVE'),
(15, 'QUALITY_GRADE', N'Cấp chất lượng', 'OPTION', NULL, N'Phân loại chất lượng sản phẩm', 'ACTIVE'),
(16, 'MATERIAL_TYPE', N'Loại vật liệu', 'TEXT', NULL, N'Phân loại vật liệu rời', 'ACTIVE'),
(17, 'GRAIN_SIZE', N'Kích thước hạt', 'TEXT', N'mm', N'Cỡ hạt sàng', 'ACTIVE'),
(18, 'SOURCE_LOCATION', N'Nguồn khai thác / Xuất xứ', 'TEXT', NULL, N'Mỏ khai thác hoặc nguồn gốc', 'ACTIVE'),
(19, 'VOLUME', N'Dung tích', 'NUMBER', N'L', N'Thể tích đóng gói', 'ACTIVE'),
(20, 'PAINT_TYPE', N'Loại sơn', 'OPTION', NULL, N'Phân loại sơn', 'ACTIVE'),
(21, 'APPLICATION_SURFACE', N'Bề mặt áp dụng', 'TEXT', NULL, N'Loại bề mặt tương thích', 'ACTIVE'),
(22, 'GLUE_TYPE', N'Loại keo / Màng', 'OPTION', NULL, N'Chủng loại keo dán, màng', 'ACTIVE'),
(23, 'WEIGHT_NET', N'Khối lượng tịnh', 'NUMBER', N'kg', N'Khối lượng đóng gói', 'ACTIVE'),
(24, 'USAGE_SCOPE', N'Phạm vi sử dụng', 'TEXT', NULL, N'Khu vực trong nhà/ngoài trời/chống thấm', 'ACTIVE'),
(25, 'ADDITIVE_TYPE', N'Loại phụ gia', 'OPTION', NULL, N'Chủng loại phụ gia', 'ACTIVE'),
(26, 'PHYSICAL_FORM', N'Dạng vật chất', 'OPTION', NULL, N'Dạng lỏng, bột, nhũ tương', 'ACTIVE'),
(27, 'USAGE_RATIO', N'Tỷ lệ sử dụng', 'TEXT', NULL, N'Tỷ lệ trộn khuyến nghị', 'ACTIVE'),
(28, 'PRESSURE_RATING', N'Áp suất định mức', 'OPTION', NULL, N'Cấp áp lực làm việc', 'ACTIVE'),
(29, 'MATERIAL', N'Chất liệu', 'TEXT', NULL, N'Chất liệu cấu thành', 'ACTIVE'),
(30, 'POWER', N'Công suất', 'NUMBER', N'W', N'Công suất định mức', 'ACTIVE'),
(31, 'VOLTAGE', N'Điện áp định mức', 'OPTION', NULL, N'Điện áp làm việc', 'ACTIVE'),
(32, 'CURRENT_RATING', N'Dòng điện định mức', 'NUMBER', N'A', N'Cường độ dòng điện', 'ACTIVE'),
(33, 'CORE_COUNT', N'Số lõi', 'NUMBER', N'Lõi', N'Số ruột dẫn bên trong', 'ACTIVE'),
(34, 'CROSS_SECTION', N'Tiết diện', 'NUMBER', N'mm²', N'Tiết diện ruột dẫn', 'ACTIVE'),
(35, 'WOOD_TYPE', N'Loại gỗ / ván', 'OPTION', NULL, N'Chủng loại gỗ công nghiệp/tự nhiên', 'ACTIVE'),
(36, 'MOISTURE_RESISTANCE', N'Cấp chống ẩm', 'OPTION', NULL, N'Khả năng kháng ẩm', 'ACTIVE'),
(37, 'GLASS_TYPE', N'Loại kính / cửa', 'OPTION', NULL, N'Quy cách kính hoặc hệ cửa', 'ACTIVE'),
(38, 'THREAD_TYPE', N'Kiểu ren', 'TEXT', NULL, N'Quy cách ren (ren mịn, thô, tự khoan)', 'ACTIVE'),
(39, 'PLATING', N'Lớp mạ bề mặt', 'OPTION', NULL, N'Xử lý bề mặt chống rỉ', 'ACTIVE'),
(40, 'MODEL', N'Model / Mã dòng máy', 'TEXT', NULL, N'Ký hiệu mã máy của nhà SX', 'ACTIVE'),
(41, 'WARRANTY_MONTHS', N'Thời hạn bảo hành', 'NUMBER', N'Tháng', N'Số tháng bảo hành chính hãng', 'ACTIVE');
SET IDENTITY_INSERT dbo.ProductAttributes OFF;
GO

-- 6.2. ProductAttributeOptions
INSERT INTO dbo.ProductAttributeOptions (ProductAttributeID, OptionCode, OptionValue, DisplayOrder, IsActive)
VALUES
-- CEMENT_TYPE (ID: 2)
(2, 'PCB30', N'Xi măng Pooc lăng hỗn hợp PCB30', 1, 1),
(2, 'PCB40', N'Xi măng Pooc lăng hỗn hợp PCB40', 2, 1),
(2, 'WHITE', N'Xi măng trắng', 3, 1),
-- CEMENT_GRADE (ID: 4)
(4, '30MPA', N'Mác 30 (>= 30 MPa)', 1, 1),
(4, '40MPA', N'Mác 40 (>= 40 MPa)', 2, 1),
-- STEEL_STANDARD (ID: 9)
(9, 'TCVN1651', N'TCVN 1651-2:2018', 1, 1),
(9, 'JIS_G3112', N'JIS G3112 (Nhật Bản)', 2, 1),
-- STEEL_GRADE (ID: 10)
(10, 'CB240T', N'CB240-T (Thép cuộn trơn)', 1, 1),
(10, 'CB300V', N'CB300-V (Thép thanh vằn)', 2, 1),
(10, 'CB400V', N'CB400-V (Thép thanh vằn)', 3, 1),
-- SURFACE_TYPE (ID: 14)
(14, 'GLOSS', N'Bóng kính (Polished/Glossy)', 1, 1),
(14, 'MATTE', N'Men mờ (Matte)', 2, 1),
-- QUALITY_GRADE (ID: 15)
(15, 'GRADE_1', N'Loại 1 (A1 - Chuẩn cao cấp)', 1, 1),
(15, 'GRADE_2', N'Loại 2 (A2 - Tiêu chuẩn)', 2, 1),
-- PAINT_TYPE (ID: 20)
(20, 'INTERIOR', N'Sơn nội thất', 1, 1),
(20, 'EXTERIOR', N'Sơn ngoại thất', 2, 1),
(20, 'PRIMER', N'Sơn lót kháng kiềm', 3, 1),
-- GLUE_TYPE (ID: 22)
(22, 'TILE_GLUE', N'Keo dán gạch đá', 1, 1),
(22, 'SILICONE', N'Keo Silicone xây dựng', 2, 1),
(22, 'WATERPROOF_MEMBRANE', N'Màng chống thấm bitum/PU', 3, 1),
-- ADDITIVE_TYPE (ID: 25)
(25, 'ACCELERATOR', N'Phụ gia đông kết nhanh', 1, 1),
(25, 'WATER_REDUCER', N'Phụ gia siêu dẻo / giảm nước', 2, 1),
(25, 'WATERPROOFING', N'Phụ gia chống thấm bê tông', 3, 1),
-- PHYSICAL_FORM (ID: 26)
(26, 'LIQUID', N'Dung dịch lỏng', 1, 1),
(26, 'POWDER', N'Dạng bột khô', 2, 1),
-- PRESSURE_RATING (ID: 28)
(28, 'PN6', N'PN6 (Áp lực 6 bar)', 1, 1),
(28, 'PN10', N'PN10 (Áp lực 10 bar)', 2, 1),
(28, 'PN16', N'PN16 (Áp lực 16 bar)', 3, 1),
-- VOLTAGE (ID: 31)
(31, '220V', N'220V (1 Pha)', 1, 1),
(31, '380V', N'380V (3 Pha)', 2, 1),
-- WOOD_TYPE (ID: 35)
(35, 'MDF', N'Ván sợi MDF', 1, 1),
(35, 'HDF', N'Ván sợi mật độ cao HDF', 2, 1),
(35, 'PLYWOOD', N'Ván ép Plywood / Gỗ dán', 3, 1),
-- MOISTURE_RESISTANCE (ID: 36)
(36, 'STANDARD', N'Tiêu chuẩn thường', 1, 1),
(36, 'MR_GREEN', N'Chống ẩm lõi xanh (HMR)', 2, 1),
-- PLATING (ID: 39)
(39, 'ELECTRO_ZINC', N'Mạ kẽm điện phân (Trắng/Bảy màu)', 1, 1),
(39, 'HOT_DIP_ZINC', N'Mạ kẽm nhúng nóng', 2, 1),
(39, 'INOX_304', N'Thép không gỉ Inox 304', 3, 1);
GO

-- 6.3. Cấu hình ProductGroupAttributes
INSERT INTO dbo.ProductGroupAttributes (ProductGroupID, ProductAttributeID, IsRequired, DisplayOrder, DefaultValue)
VALUES
-- PG01: Xi măng
(1, 1, 1, 1, NULL),
(1, 2, 1, 2, N'PCB40'),
(1, 3, 1, 3, N'50'),
(1, 4, 1, 4, N'40MPA'),

-- PG02: Thép xây dựng
(2, 1, 1, 1, NULL),
(2, 5, 1, 2, NULL),
(2, 6, 1, 3, N'11.7'),
(2, 9, 1, 4, N'TCVN1651'),
(2, 10, 1, 5, N'CB300V'),

-- PG03: Tôn
(3, 1, 1, 1, NULL),
(3, 8, 1, 2, N'0.45'),
(3, 7, 1, 3, N'1.08'),
(3, 6, 1, 4, N'6.0'),
(3, 11, 1, 5, N'Xanh ngọc'),

-- PG04: Gạch
(4, 1, 1, 1, NULL),
(4, 13, 1, 2, N'600x600'),
(4, 14, 1, 3, N'GLOSS'),
(4, 15, 1, 4, N'GRADE_1'),

-- PG05: Cát Đá
(5, 16, 1, 1, N'Cát vàng xây tô'),
(5, 17, 1, 2, N'1.5 - 2.0'),
(5, 18, 0, 3, N'Mỏ cát Sông Lô'),

-- PG06: Sơn (FEFO)
(6, 1, 1, 1, N'Dulux'),
(6, 11, 1, 2, N'Trắng sứ'),
(6, 19, 1, 3, N'18'),
(6, 20, 1, 4, N'INTERIOR'),

-- PG07: Keo (FEFO)
(7, 1, 1, 1, N'Sika'),
(7, 22, 1, 2, N'TILE_GLUE'),
(7, 23, 1, 3, N'25'),

-- PG08: Phụ gia (FEFO)
(8, 1, 1, 1, N'Sika'),
(8, 25, 1, 2, N'WATERPROOFING'),
(8, 26, 1, 3, N'LIQUID'),

-- PG09: Ống nước
(9, 1, 1, 1, N'Tiền Phong'),
(9, 5, 1, 2, N'110'),
(9, 6, 1, 3, N'4'),
(9, 28, 1, 4, N'PN10'),

-- PG10: Thiết bị điện
(10, 1, 1, 1, N'Cadivi'),
(10, 31, 1, 2, N'220V'),
(10, 33, 1, 3, N'2'),
(10, 34, 1, 4, N'2.5');
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 7: SEED 18 VẬT TƯ XÂY DỰNG MẪU (Products) & GIÁ TRỊ EAV';
PRINT N'======================================================================';

SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (ProductID, ProductGroupID, UnitOfMeasureID, ProductCode, ProductName, Barcode, Description, RotationMethod, TrackLot, TrackExpiry, DefaultShelfLifeDays, Status, CreatedByUserID, CreatedAt)
VALUES
-- 1. Xi măng (FIFO)
(1, 1, 1, 'XM001', N'Xi măng Vicem Hà Tiên PCB40 (Bao 50kg)', '8935001100012', N'Xi măng Pooc lăng hỗn hợp chất lượng cao, chuyên dùng đổ bê tông móng, dầm, cột.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),
(2, 1, 1, 'XM002', N'Xi măng Nghi Sơn PCB30 (Bao 50kg)', '8935001100029', N'Xi măng xây tô chuyên dụng, dẻo vữa, bám dính tốt, chống rạn nứt tường.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 2. Thép xây dựng (FIFO)
(3, 2, 4, 'TH001', N'Thép cây Hòa Phát D16 CB300V (Cây 11.7m)', '8935002200015', N'Thép thanh vằn đường kính 16mm, mác thép CB300V theo tiêu chuẩn TCVN 1651-2:2018.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),
(4, 2, 4, 'TH002', N'Thép cây Hòa Phát D20 CB400V (Cây 11.7m)', '8935002200022', N'Thép thanh vằn đường kính 20mm, mác thép cường độ cao CB400V.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),
(5, 2, 3, 'TH003', N'Thép cuộn Hòa Phát Phi 6 CB240T', '8935002200039', N'Thép cuộn trơn tròn Phi 6 dùng làm cốt đai và gia công kết cấu.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 3. Tôn và tấm lợp (FIFO)
(6, 3, 14, 'TL001', N'Tôn lạnh mạ màu Hoa Sen 0.45mm (Khổ 1.08m)', '8935003300018', N'Tôn lạnh mạ màu xanh ngọc, chống ăn mòn, phản xạ nhiệt tốt.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 4. Gạch xây & ốp lát (FIFO)
(7, 4, 6, 'GV001', N'Gạch ốp lát Ceramic Viglacera 600x600', '8935004400011', N'Gạch men bóng kính mài cạnh, xương ceramic cao cấp, chịu lực tốt.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),
(8, 4, 13, 'GV002', N'Gạch xây Tuynel 4 lỗ Đồng Tâm', '8935004400028', N'Gạch đất nung 4 lỗ kích thước 80x80x180mm cường độ nén cao.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 5. Cát Đá (FIFO)
(9, 5, 7, 'CD001', N'Cát vàng xây tô sàng tuyển Sông Lô', '8935005500014', N'Cát vàng hạt trung 1.5 - 2.0mm, đã qua sàng rửa sạch tạp chất.', 'FIFO', 0, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),
(10, 5, 7, 'CD002', N'Đá xây dựng 1x2 bê tông nghiền xanh', '8935005500021', N'Đá dăm 1x2 cỡ hạt đồng đều, mác đá cứng đổ bê tông tươi mác cao.', 'FIFO', 0, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 6. Sơn (FEFO - Bắt buộc Lô & HSD)
(11, 6, 8, 'SN001', N'Sơn nội thất Dulux EasyClean 18L', '8935006600017', N'Sơn nước nội thất cao cấp lau chùi hiệu quả, chống bám bẩn.', 'FEFO', 1, 1, 730, 'ACTIVE', 1, SYSUTCDATETIME()),
(12, 6, 8, 'SN002', N'Sơn lót ngoại thất Maxilite chống kiềm 18L', '8935006600024', N'Sơn lót kháng kiềm ngoại thất bảo vệ tường trước rêu mốc.', 'FEFO', 1, 1, 730, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 7. Keo & Chống thấm (FEFO - Bắt buộc Lô & HSD)
(13, 7, 1, 'KG001', N'Keo dán gạch Sika Ceram 200HP (Bao 25kg)', '8935007700010', N'Keo dán gạch gốc xi măng cao cấp cho gạch khổ lớn và ngoài trời.', 'FEFO', 1, 1, 365, 'ACTIVE', 1, SYSUTCDATETIME()),
(14, 7, 11, 'KG002', N'Keo Silicone Apollo A500 trung tính', '8935007700027', N'Keo trám khe kết dính nhôm kính, bê tông chống thấm nước.', 'FEFO', 1, 1, 365, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 8. Phụ gia bê tông (FEFO - Bắt buộc Lô & HSD)
(15, 8, 10, 'PG001', N'Phụ gia siêu dẻo Sikament R4 (Can 25L)', '8935008800013', N'Phụ gia siêu hóa dẻo kéo dài thời gian ninh kết cho bê tông.', 'FEFO', 1, 1, 365, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 9. Ống nước (FIFO)
(16, 9, 12, 'ON001', N'Ống uPVC Tiền Phong D110x3.2mm (Cây 4m)', '8935009900016', N'Ống thoát nước uPVC Class 2 chịu áp lực PN6.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 10. Thiết bị điện (FIFO)
(17, 10, 5, 'DX001', N'Dây cáp điện Cadivi CV 2x2.5mm² (Cuộn 100m)', '8935010100019', N'Dây đôi ruột đồng mềm bọc cách điện PVC 450/750V.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME()),

-- 11. Thiết bị vệ sinh (FIFO)
(18, 11, 13, 'TB001', N'Bồn cầu một khối Inax AC-909VRN', '8935011100012', N'Bồn cầu sứ vệ sinh phủ men Aqua Ceramic chống bám bẩn xả xoáy.', 'FIFO', 1, 0, NULL, 'ACTIVE', 1, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Products OFF;
GO

-- 7.1. Giá trị EAV (ProductAttributeValues)
INSERT INTO dbo.ProductAttributeValues (ProductID, ProductAttributeID, AttributeValue, UpdatedAt)
VALUES
-- XM001 (Vicem Hà Tiên)
(1, 1, N'Vicem Hà Tiên', SYSUTCDATETIME()),
(1, 2, N'PCB40', SYSUTCDATETIME()),
(1, 3, N'50', SYSUTCDATETIME()),
(1, 4, N'40MPA', SYSUTCDATETIME()),

-- TH001 (Thép Hòa Phát D16)
(3, 1, N'Hòa Phát', SYSUTCDATETIME()),
(3, 5, N'16', SYSUTCDATETIME()),
(3, 6, N'11.7', SYSUTCDATETIME()),
(3, 9, N'TCVN1651', SYSUTCDATETIME()),
(3, 10, N'CB300V', SYSUTCDATETIME()),

-- GV001 (Gạch Viglacera 600x600)
(7, 1, N'Viglacera', SYSUTCDATETIME()),
(7, 13, N'600x600', SYSUTCDATETIME()),
(7, 14, N'GLOSS', SYSUTCDATETIME()),
(7, 15, N'GRADE_1', SYSUTCDATETIME()),

-- SN001 (Sơn Dulux)
(11, 1, N'Dulux', SYSUTCDATETIME()),
(11, 11, N'Trắng sứ (White 2140)', SYSUTCDATETIME()),
(11, 19, N'18', SYSUTCDATETIME()),
(11, 20, N'INTERIOR', SYSUTCDATETIME()),

-- KG001 (Keo Sika Ceram)
(13, 1, N'Sika', SYSUTCDATETIME()),
(13, 22, N'TILE_GLUE', SYSUTCDATETIME()),
(13, 23, N'25', SYSUTCDATETIME()),

-- DX001 (Dây điện Cadivi)
(17, 1, N'Cadivi', SYSUTCDATETIME()),
(17, 31, N'220V', SYSUTCDATETIME()),
(17, 33, N'2', SYSUTCDATETIME()),
(17, 34, N'2.5', SYSUTCDATETIME());
GO

-- 7.2. Lô sản xuất mẫu (ProductLots)
SET IDENTITY_INSERT dbo.ProductLots ON;
INSERT INTO dbo.ProductLots (ProductLotID, ProductID, LotNumber, ManufactureDate, ExpiryDate, FirstReceivedDate, Status, CreatedAt)
VALUES
(1, 1, 'LOT-XM-202607', '2026-07-01', NULL, '2026-07-10', 'AVAILABLE', SYSUTCDATETIME()),
(2, 3, 'LOT-TH-HP2608', '2026-07-15', NULL, '2026-07-20', 'AVAILABLE', SYSUTCDATETIME()),
(3, 7, 'LOT-GV-VG2605', '2026-05-10', NULL, '2026-05-20', 'AVAILABLE', SYSUTCDATETIME()),
(4, 11, 'LOT-SN-DL2601', '2026-06-01', '2028-06-01', '2026-06-10', 'AVAILABLE', SYSUTCDATETIME()),
(5, 13, 'LOT-KG-SK2604', '2026-04-01', '2027-04-01', '2026-04-15', 'AVAILABLE', SYSUTCDATETIME()),
(6, 15, 'LOT-PG-SK2606', '2026-06-15', '2027-06-15', '2026-06-25', 'AVAILABLE', SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.ProductLots OFF;
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 8: SEED NHÀ CUNG CẤP (Suppliers) & LIÊN KẾT SẢN PHẨM';
PRINT N'======================================================================';

SET IDENTITY_INSERT dbo.Suppliers ON;
INSERT INTO dbo.Suppliers (SupplierID, SupplierCode, SupplierName, PhoneNumber, Email, Address, RepresentativeName, TaxCode, Status, CreatedByUserID, CreatedAt)
VALUES
(1, 'SUP-001', N'Công ty Cổ phần Xi măng Vicem Hà Tiên', '02838291750', 'contact@vicemhatien.com.vn', N'360 Bến Vân Đồn, P.1, Q.4, TP.HCM', N'Nguyễn Văn Hùng', '0300123456', 'ACTIVE', 1, SYSUTCDATETIME()),
(2, 'SUP-002', N'Tập đoàn Hòa Phát - Chi nhánh Thép Xây Dựng', '02439747755', 'thephoaphat@hoaphat.com.vn', N'66 Nguyễn Du, P. Nguyễn Du, Q. Hai Bà Trưng, Hà Nội', N'Trần Đình Long', '0100234567', 'ACTIVE', 1, SYSUTCDATETIME()),
(3, 'SUP-003', N'Tổng Công ty Viglacera - CTCP', '02435536660', 'info@viglacera.com.vn', N'Tòa nhà Viglacera, Số 1 Đại lộ Thăng Long, Hà Nội', N'Nguyễn Anh Tuấn', '0100104568', 'ACTIVE', 1, SYSUTCDATETIME()),
(4, 'SUP-004', N'Tập đoàn Hoa Sen (Tôn Hoa Sen)', '18001515', 'hoasen@hoasengroup.vn', N'Số 183 Nguyễn Văn Trỗi, P.10, Q. Phú Nhuận, TP.HCM', N'Lê Phước Vũ', '3700381324', 'ACTIVE', 1, SYSUTCDATETIME()),
(5, 'SUP-005', N'Công ty TNHH Sika Hóa Chất Xây Dựng Việt Nam', '02513560700', 'sikavietnam@vn.sika.com', N'KCN Nhơn Trạch 1, H. Nhơn Trạch, Đồng Nai', N'Markus Schopfer', '3600238910', 'ACTIVE', 1, SYSUTCDATETIME()),
(6, 'SUP-006', N'Công ty CP Nhựa Thiếu Niên Tiền Phong', '02253813979', 'contact@nhuatienphong.vn', N'Số 222 An Đà, P. Đằng Giang, Q. Ngô Quyền, Hải Phòng', N'Đặng Quốc Dũng', '0200234569', 'ACTIVE', 1, SYSUTCDATETIME()),
(7, 'SUP-007', N'Công ty CP Dây Cáp Điện Việt Nam (Cadivi)', '02838299443', 'cadivi@cadivi.vn', N'70-72 Nam Kỳ Khởi Nghĩa, Q.1, TP.HCM', N'Lê Bá Thọ', '0300381567', 'ACTIVE', 1, SYSUTCDATETIME()),
(8, 'SUP-008', N'Công ty CP Khai Thác Cát Đá Sông Lô', '02103845678', 'catdasonglo@gmail.com', N'Cảng Việt Trì, TP. Việt Trì, Phú Thọ', N'Phạm Văn Cường', '2600123987', 'ACTIVE', 1, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Suppliers OFF;
GO

-- Liên kết Nhà cung cấp - Sản phẩm (SupplierProducts)
INSERT INTO dbo.SupplierProducts (SupplierID, ProductID, SupplierProductCode, LastPurchasePrice, LeadTimeDays, IsPreferred, Status)
VALUES
(1, 1, 'HT-PCB40', 88000.0, 2, 1, 'ACTIVE'),
(1, 2, 'HT-PCB30', 82000.0, 2, 1, 'ACTIVE'),
(2, 3, 'HP-D16', 295000.0, 3, 1, 'ACTIVE'),
(2, 4, 'HP-D20', 460000.0, 3, 1, 'ACTIVE'),
(2, 5, 'HP-PHI6', 15800.0, 3, 1, 'ACTIVE'),
(3, 7, 'VG-600X600', 165000.0, 5, 1, 'ACTIVE'),
(3, 8, 'VG-TUYNEL', 1250.0, 2, 1, 'ACTIVE'),
(4, 6, 'HS-TON045', 115000.0, 4, 1, 'ACTIVE'),
(5, 13, 'SK-CERAM200', 230000.0, 3, 1, 'ACTIVE'),
(5, 15, 'SK-MENT-R4', 850000.0, 3, 1, 'ACTIVE'),
(6, 16, 'TP-UPVC110', 178000.0, 2, 1, 'ACTIVE'),
(7, 17, 'CAD-CV25', 1450000.0, 2, 1, 'ACTIVE'),
(8, 9, 'SL-CATVANG', 320000.0, 1, 1, 'ACTIVE'),
(8, 10, 'SL-DA1X2', 380000.0, 1, 1, 'ACTIVE');
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 9: SEED ĐƠN MUA HÀNG (PO) & PHIẾU NHẬP KHO (Inbound Orders)';
PRINT N'======================================================================';

-- 9.1. Purchase Orders
SET IDENTITY_INSERT dbo.PurchaseOrders ON;
INSERT INTO dbo.PurchaseOrders (PurchaseOrderID, PurchaseOrderNumber, SupplierID, OrderDate, ExpectedDeliveryDate, Status, Notes, CreatedByUserID, CreatedAt)
VALUES
(1, 'PO-2026-001', 1, '2026-07-05', '2026-07-10', 'COMPLETED', N'Đơn nhập định kỳ Xi măng Hà Tiên', 1, SYSUTCDATETIME()),
(2, 'PO-2026-002', 2, '2026-07-15', '2026-07-20', 'CONFIRMED', N'Đơn nhập thép thanh D16 chuẩn bị cho dự án công trình Q.2', 1, SYSUTCDATETIME()),
(3, 'PO-2026-003', 5, '2026-08-01', '2026-08-08', 'DRAFT', N'Đơn đặt hàng phụ gia chống thấm và keo dán gạch', 1, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.PurchaseOrders OFF;
GO

SET IDENTITY_INSERT dbo.PurchaseOrderDetails ON;
INSERT INTO dbo.PurchaseOrderDetails (PurchaseOrderDetailID, PurchaseOrderID, ProductID, OrderedQuantity, UnitPrice, Notes)
VALUES
(1, 1, 1, 500.0, 88000.0, N'500 bao xi măng Hà Tiên PCB40'),
(2, 2, 3, 200.0, 295000.0, N'200 cây thép Hòa Phát D16'),
(3, 3, 13, 100.0, 230000.0, N'100 bao keo Sika Ceram');
SET IDENTITY_INSERT dbo.PurchaseOrderDetails OFF;
GO

-- 9.2. Inbound Orders
SET IDENTITY_INSERT dbo.InboundOrders ON;
INSERT INTO dbo.InboundOrders (InboundOrderID, InboundOrderNumber, WarehouseID, SourceType, PurchaseOrderID, ExpectedReceiptDate, Status, Notes, CreatedByUserID, AssignedToUserID, CreatedAt)
VALUES
(1, 'INB-2026-001', 1, 'PURCHASE_ORDER', 1, '2026-07-10', 'COMPLETED', N'Đã nhập hoàn tất 500 bao xi măng vào Khu A', 1, 3, SYSUTCDATETIME()),
(2, 'INB-2026-002', 1, 'PURCHASE_ORDER', 2, '2026-07-20', 'IN_PROGRESS', N'Đang kiểm đếm quy cách thép thanh tại Dock 1', 1, 3, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.InboundOrders OFF;
GO

SET IDENTITY_INSERT dbo.InboundOrderItems ON;
INSERT INTO dbo.InboundOrderItems (InboundOrderItemID, InboundOrderID, ProductID, ExpectedQuantity, ReceivedQuantity, DamagedQuantity, ShortageQuantity, Notes)
VALUES
(1, 1, 1, 500.0, 500.0, 0.0, 0.0, N'Nhập đủ 500 bao'),
(2, 2, 3, 200.0, 100.0, 0.0, 0.0, N'Đã dỡ 100 cây đầu tiên');
SET IDENTITY_INSERT dbo.InboundOrderItems OFF;
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 10: SEED TỒN KHO THỰC TẾ THEO VỊ TRÍ & LÔ (Inventory)';
PRINT N'======================================================================';

SET IDENTITY_INSERT dbo.Inventory ON;
INSERT INTO dbo.Inventory (InventoryID, ProductID, StorageLocationID, ProductLotID, OnHandQuantity, ReservedQuantity, LastUpdatedAt)
VALUES
-- Xi măng Hà Tiên tại Kệ A1-01 (Lô LOT-XM-202607)
(1, 1, 4, 1, 350.0, 50.0, SYSUTCDATETIME()),
-- Xi măng Hà Tiên tại Kệ A1-02 (Lô LOT-XM-202607)
(2, 1, 5, 1, 150.0, 0.0, SYSUTCDATETIME()),
-- Thép Hòa Phát D16 tại Giá B1-01 (Lô LOT-TH-HP2608)
(3, 3, 7, 2, 200.0, 20.0, SYSUTCDATETIME()),
-- Gạch Viglacera tại Kệ D1-01 (Lô LOT-GV-VG2605)
(4, 7, 9, 3, 400.0, 0.0, SYSUTCDATETIME()),
-- Sơn Dulux 18L tại Kệ F1-01 (Lô LOT-SN-DL2601 - FEFO)
(5, 11, 13, 4, 80.0, 10.0, SYSUTCDATETIME()),
-- Keo Sika Ceram tại Kệ F2-01 (Lô LOT-KG-SK2604 - FEFO)
(6, 13, 15, 5, 120.0, 0.0, SYSUTCDATETIME()),
-- Phụ gia Sikament R4 tại Kệ F2-01 (Lô LOT-PG-SK2606 - FEFO)
(7, 15, 15, 6, 50.0, 0.0, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Inventory OFF;
GO

PRINT N'======================================================================';
PRINT N'>>> BƯỚC 11: KÍCH HOẠT LẠI TOÀN BỘ TRIGGER & KHÓA NGOẠI (Foreign Keys)';
PRINT N'======================================================================';

-- Bật lại toàn bộ Trigger trên tất cả các bảng
EXEC sp_MSforeachtable 'ALTER TABLE ? ENABLE TRIGGER ALL';
-- Kiểm tra và bật lại toàn bộ ràng buộc Foreign Key
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO

PRINT N'======================================================================';
PRINT N'======================================================================';
PRINT N'>>> BƯỚC CUỐI: BẬT LẠI TOÀN BỘ TRIGGER VÀ KIỂM TRA RÀNG BUỘC KHÓA NGOẠI';
PRINT N'======================================================================';

EXEC sp_MSforeachtable 'ALTER TABLE ? ENABLE TRIGGER ALL';
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO

PRINT N'======================================================================';
PRINT N'>>> TỔNG HỢP KIỂM TRA DỮ LIỆU ĐÃ SEED THÀNH CÔNG:';
PRINT N'======================================================================';

SELECT N'Roles' AS TableName, COUNT(*) AS TotalRecords FROM dbo.Roles
UNION ALL SELECT N'Users (Full 5 Actors + Test)', COUNT(*) FROM dbo.Users
UNION ALL SELECT N'Warehouses (1 Kho duy nhất)', COUNT(*) FROM dbo.Warehouses
UNION ALL SELECT N'WarehouseZones', COUNT(*) FROM dbo.WarehouseZones
UNION ALL SELECT N'StorageLocations', COUNT(*) FROM dbo.StorageLocations
UNION ALL SELECT N'UnitsOfMeasure (ĐVT VLXD)', COUNT(*) FROM dbo.UnitsOfMeasure
UNION ALL SELECT N'ProductGroups (15 Nhóm Chuẩn)', COUNT(*) FROM dbo.ProductGroups
UNION ALL SELECT N'ProductAttributes (41 Thuộc tính EAV)', COUNT(*) FROM dbo.ProductAttributes
UNION ALL SELECT N'Products (Vật tư xây dựng mẫu)', COUNT(*) FROM dbo.Products
UNION ALL SELECT N'ProductLots', COUNT(*) FROM dbo.ProductLots
UNION ALL SELECT N'Suppliers', COUNT(*) FROM dbo.Suppliers
UNION ALL SELECT N'PurchaseOrders', COUNT(*) FROM dbo.PurchaseOrders
UNION ALL SELECT N'InboundOrders', COUNT(*) FROM dbo.InboundOrders
UNION ALL SELECT N'Inventory (Tồn kho thực tế)', COUNT(*) FROM dbo.Inventory;
GO

PRINT N'======================================================================';
PRINT N'>>> HOÀN TẤT RESET VÀ NẠP TOÀN BỘ DỮ LIỆU MẪU CHUẨN MỰC!';
PRINT N'>>> TẤT CẢ TÀI KHOẢN ĐĂNG NHẬP VỚI PASSWORD: Admin@123456';
PRINT N'======================================================================';
GO
