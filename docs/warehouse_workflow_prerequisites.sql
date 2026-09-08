/* Vi tri he thong bat buoc cho luong nhan/xuat mot kho. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @WarehouseID BIGINT =
(
    SELECT TOP (1) WarehouseID
    FROM dbo.Warehouses
    WHERE Status = 'ACTIVE'
    ORDER BY IsPrimary DESC, WarehouseID
);

IF @WarehouseID IS NULL
    THROW 51020, N'Khong co kho dang hoat dong de cau hinh vi tri he thong.', 1;

IF NOT EXISTS
(
    SELECT 1 FROM dbo.StorageLocations
    WHERE WarehouseID = @WarehouseID AND LocationType = 'QUARANTINE'
      AND Status NOT IN ('BLOCKED','INACTIVE')
)
BEGIN
    INSERT dbo.StorageLocations
    (
        WarehouseID, RackID, LocationCode, LocationName, LocationType,
        AreaSquareMeter, MaxWeightKg, MaxVolumeM3,
        IsPutawayAllowed, IsPickable, Status, CreatedAt
    )
    VALUES
    (
        @WarehouseID, NULL, 'LOC-QUARANTINE-01', N'Khu cách ly hàng hỏng/chờ xử lý', 'QUARANTINE',
        NULL, NULL, NULL, 1, 0, 'AVAILABLE', SYSUTCDATETIME()
    );
END;

COMMIT TRANSACTION;
