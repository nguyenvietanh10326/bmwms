-- Tạo Zone (Khu vực kho)
SET IDENTITY_INSERT WarehouseZones ON;
INSERT INTO WarehouseZones (ZoneID, WarehouseID, ZoneCode, ZoneName, Description, Status) VALUES
(1, 1, 'ZONE-A', N'Khu A - Vật liệu xây dựng', N'Khu vực lưu trữ xi măng, cát, đá, sắt thép', 'ACTIVE'),
(2, 1, 'ZONE-B', N'Khu B - Vật tư hoàn thiện', N'Khu vực lưu trữ gạch, sơn, keo, phụ kiện', 'ACTIVE'),
(3, 1, 'ZONE-C', N'Khu C - Hàng cồng kềnh', N'Khu vực lưu trữ ống nước, thép hình, tôn', 'ACTIVE'),
(4, 1, 'ZONE-RCV', N'Khu vực nhận hàng', N'Khu tiếp nhận hàng từ nhà cung cấp', 'ACTIVE'),
(5, 1, 'ZONE-DSP', N'Khu vực xuất hàng', N'Khu đóng gói và xuất hàng', 'ACTIVE');
SET IDENTITY_INSERT WarehouseZones OFF;

-- Tạo Rack (Kệ hàng)
SET IDENTITY_INSERT StorageRacks ON;
INSERT INTO StorageRacks (RackID, WarehouseID, ZoneID, RackCode, RackName, Status) VALUES
(1, 1, 1, 'A-R01', N'Kệ A-R01', 'ACTIVE'),
(2, 1, 1, 'A-R02', N'Kệ A-R02', 'ACTIVE'),
(3, 1, 1, 'A-R03', N'Kệ A-R03', 'ACTIVE'),
(4, 1, 2, 'B-R01', N'Kệ B-R01', 'ACTIVE'),
(5, 1, 2, 'B-R02', N'Kệ B-R02', 'ACTIVE'),
(6, 1, 3, 'C-R01', N'Kệ C-R01', 'ACTIVE'),
(7, 1, 3, 'C-R02', N'Kệ C-R02', 'ACTIVE');
SET IDENTITY_INSERT StorageRacks OFF;

-- Thêm các vị trí StorageLocations
INSERT INTO StorageLocations (WarehouseID, RackID, LocationCode, LocationName, LocationType, AreaSquareMeter, MaxWeightKg, IsPutawayAllowed, IsPickable, Status, CreatedAt) VALUES
-- Kệ A-R01 (thêm tầng 3, 4)
(1, 1, 'BIN-A01-03', N'Kệ A01 Tầng 03', 'BIN', 4.5, 2000, 1, 1, 'AVAILABLE', GETDATE()),
(1, 1, 'BIN-A01-04', N'Kệ A01 Tầng 04', 'BIN', 4.5, 1500, 1, 1, 'AVAILABLE', GETDATE()),
-- Kệ A-R02 (thêm tầng 2, 3)
(1, 2, 'BIN-A02-02', N'Kệ A02 Tầng 02', 'BIN', 4.5, 2500, 1, 1, 'AVAILABLE', GETDATE()),
(1, 2, 'BIN-A02-03', N'Kệ A02 Tầng 03', 'BIN', 4.5, 2000, 1, 1, 'AVAILABLE', GETDATE()),
-- Kệ A-R03
(1, 3, 'BIN-A03-01', N'Kệ A03 Tầng 01', 'BIN', 6.0, 5000, 1, 1, 'AVAILABLE', GETDATE()),
(1, 3, 'BIN-A03-02', N'Kệ A03 Tầng 02', 'BIN', 6.0, 4000, 1, 1, 'AVAILABLE', GETDATE()),
(1, 3, 'BIN-A03-03', N'Kệ A03 Tầng 03', 'BIN', 6.0, 3000, 1, 1, 'AVAILABLE', GETDATE()),
-- Kệ B-R01 (thêm tầng 2, 3)
(1, 4, 'BIN-B01-02', N'Kệ B01 Tầng 02', 'BIN', 3.5, 1500, 1, 1, 'AVAILABLE', GETDATE()),
(1, 4, 'BIN-B01-03', N'Kệ B01 Tầng 03', 'BIN', 3.5, 1000, 1, 1, 'AVAILABLE', GETDATE()),
-- Kệ B-R02
(1, 5, 'BIN-B02-01', N'Kệ B02 Tầng 01', 'BIN', 3.5, 2000, 1, 1, 'AVAILABLE', GETDATE()),
(1, 5, 'BIN-B02-02', N'Kệ B02 Tầng 02', 'BIN', 3.5, 1500, 1, 1, 'AVAILABLE', GETDATE()),
(1, 5, 'BIN-B02-03', N'Kệ B02 Tầng 03', 'BIN', 3.5, 1000, 1, 1, 'AVAILABLE', GETDATE()),
-- Kệ C-R01 (hàng cồng kềnh, ô lớn)
(1, 6, 'BIN-C01-01', N'Kệ C01 Ô 01', 'BIN', 12.0, 8000, 1, 1, 'AVAILABLE', GETDATE()),
(1, 6, 'BIN-C01-02', N'Kệ C01 Ô 02', 'BIN', 12.0, 8000, 1, 1, 'AVAILABLE', GETDATE()),
-- Kệ C-R02
(1, 7, 'BIN-C02-01', N'Kệ C02 Ô 01', 'BIN', 10.0, 6000, 1, 1, 'AVAILABLE', GETDATE()),
(1, 7, 'BIN-C02-02', N'Kệ C02 Ô 02', 'BIN', 10.0, 6000, 1, 1, 'AVAILABLE', GETDATE()),
-- Thêm khu nhận hàng và staging
(1, NULL, 'RCV-02', N'Khu vực nhận hàng 02', 'RECEIVING', 20.0, NULL, 0, 0, 'AVAILABLE', GETDATE()),
(1, NULL, 'STG-02', N'Khu vực tạm chứa 02', 'STAGING', 15.0, NULL, 1, 0, 'AVAILABLE', GETDATE()),
(1, NULL, 'DSP-02', N'Khu vực xuất hàng 02', 'DISPATCH', 18.0, NULL, 0, 1, 'AVAILABLE', GETDATE()),
(1, NULL, 'QUA-02', N'Khu vực cách ly 02', 'QUARANTINE', 8.0, NULL, 0, 0, 'AVAILABLE', GETDATE());
