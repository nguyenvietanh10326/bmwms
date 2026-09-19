SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_StocktakeItems_Resolution')
    ALTER TABLE dbo.StocktakeItems DROP CONSTRAINT CK_StocktakeItems_Resolution;

UPDATE dbo.StocktakeItems
SET Resolution = 'NO_ADJUSTMENT',
    Notes = CASE
        WHEN Notes IS NULL OR LTRIM(RTRIM(Notes)) = ''
            THEN '[RESERVED_SHORTAGE] Không khớp tồn do số đếm nhỏ hơn số lượng đã giữ.'
        WHEN Notes LIKE '%[[]RESERVED_SHORTAGE]%'
            THEN Notes
        ELSE LEFT(Notes + CHAR(13) + CHAR(10) + '[RESERVED_SHORTAGE] Không khớp tồn do số đếm nhỏ hơn số lượng đã giữ.', 1000)
    END
WHERE Resolution = 'RECOUNT';

ALTER TABLE dbo.StocktakeItems WITH CHECK ADD CONSTRAINT CK_StocktakeItems_Resolution CHECK
(
    Resolution IS NULL OR Resolution IN ('NO_ADJUSTMENT', 'ACCEPT_DIFFERENCE')
);

ALTER TABLE dbo.StocktakeItems CHECK CONSTRAINT CK_StocktakeItems_Resolution;

COMMIT TRANSACTION;
GO
