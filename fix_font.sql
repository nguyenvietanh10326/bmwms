USE [BMWMS];
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
UPDATE [Products] SET ProductName = N'Xi măng PCB40' WHERE ProductCode = 'XM001';
UPDATE [Products] SET ProductName = N'Thép cây D16' WHERE ProductCode = 'TH001';
UPDATE [Products] SET ProductName = N'Đá 1x2 nghiền' WHERE ProductCode = 'CD002';
UPDATE [Products] SET ProductName = N'Gạch Viglacera' WHERE ProductCode = 'GV001';
UPDATE [Products] SET ProductName = N'Dây cáp điện 2.5' WHERE ProductCode = 'DX001';

UPDATE [ProductGroups] SET GroupName = N'Vật liệu xây dựng' WHERE GroupCode = 'PG-01';
UPDATE [ProductGroups] SET GroupName = N'Thiết bị điện nước' WHERE GroupCode = 'PG-02';
GO
