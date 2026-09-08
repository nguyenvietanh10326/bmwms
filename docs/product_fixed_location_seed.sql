/*
  Goi y vi tri co dinh cho bo du lieu mau mot kho.
  Ra soat lai neu ma vi tri/san pham cua moi truong trien khai khac bo seed nay.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

WITH FixedMap AS
(
    SELECT * FROM (VALUES
        ('XM001','LOC-A1-01'), ('XM002','LOC-A1-02'),
        ('TH001','LOC-B1-01'), ('TH002','LOC-B1-02'), ('TH003','LOC-B1-01'),
        ('TL001','LOC-B1-02'),
        ('GV001','LOC-D1-01'), ('GV002','LOC-D1-02'),
        ('CD001','LOC-E-SAND-01'), ('CD002','LOC-E-STONE-01'),
        ('SN001','LOC-F1-01'), ('SN002','LOC-F1-02'),
        ('KG001','LOC-F2-01'), ('KG002','LOC-F2-01'), ('PG001','LOC-F2-01'),
        ('ON001','LOC-G1-01'), ('DX001','LOC-H1-01'), ('TB001','LOC-D1-02')
    ) v(ProductCode, LocationCode)
), SourceData AS
(
    SELECT p.ProductID, l.StorageLocationID
    FROM FixedMap m
    JOIN dbo.Products p ON p.ProductCode = m.ProductCode
    JOIN dbo.StorageLocations l ON l.LocationCode = m.LocationCode
    WHERE p.Status = 'ACTIVE' AND l.LocationType = 'BIN'
)
MERGE dbo.ProductFixedLocations AS target
USING SourceData AS source
ON target.ProductID = source.ProductID
WHEN MATCHED THEN UPDATE SET
    StorageLocationID = source.StorageLocationID,
    Priority = 1,
    IsDefault = 1,
    IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (ProductID, StorageLocationID, Priority, IsDefault, IsActive)
    VALUES (source.ProductID, source.StorageLocationID, 1, 1, 1);

COMMIT TRANSACTION;
