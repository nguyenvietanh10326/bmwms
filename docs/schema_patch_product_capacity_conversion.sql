/*
  BMWMS - require product volume conversion for warehouse capacity.

  STORAGE_VOLUME_M3_PER_BASE_UOM stores the number of cubic meters occupied
  by one product base unit. Example: one 50 kg bag = 0.035 m3.
  This script is idempotent and can be rerun on an existing database.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @StorageVolumeAttributeId BIGINT;

SELECT @StorageVolumeAttributeId = ProductAttributeID
FROM dbo.ProductAttributes
WHERE AttributeCode = 'STORAGE_VOLUME_M3_PER_BASE_UOM';

IF @StorageVolumeAttributeId IS NULL
BEGIN
    INSERT INTO dbo.ProductAttributes
    (
        AttributeCode,
        AttributeName,
        DataType,
        UnitLabel,
        Description,
        Status
    )
    VALUES
    (
        'STORAGE_VOLUME_M3_PER_BASE_UOM',
        N'Quy đổi thể tích lưu kho theo ĐVT cơ sở',
        'NUMBER',
        N'm³/ĐVT',
        N'Thể tích chiếm chỗ của một đơn vị tính cơ sở, dùng để tính sức chứa theo m³.',
        'ACTIVE'
    );

    SET @StorageVolumeAttributeId = CONVERT(BIGINT, SCOPE_IDENTITY());
END;

INSERT INTO dbo.ProductGroupAttributes
(
    ProductGroupID,
    ProductAttributeID,
    IsRequired,
    DisplayOrder,
    DefaultValue
)
SELECT
    productGroup.ProductGroupID,
    @StorageVolumeAttributeId,
    1,
    ISNULL((
        SELECT MAX(existing.DisplayOrder) + 1
        FROM dbo.ProductGroupAttributes existing
        WHERE existing.ProductGroupID = productGroup.ProductGroupID
    ), 1),
    NULL
FROM dbo.ProductGroups productGroup
WHERE productGroup.Status = 'ACTIVE'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.ProductGroupAttributes existing
      WHERE existing.ProductGroupID = productGroup.ProductGroupID
        AND existing.ProductAttributeID = @StorageVolumeAttributeId
  );

UPDATE groupAttribute
SET IsRequired = 1,
    DefaultValue = NULL
FROM dbo.ProductGroupAttributes groupAttribute
JOIN dbo.ProductGroups productGroup
  ON productGroup.ProductGroupID = groupAttribute.ProductGroupID
WHERE productGroup.Status = 'ACTIVE'
  AND groupAttribute.ProductAttributeID = @StorageVolumeAttributeId;

COMMIT TRANSACTION;
