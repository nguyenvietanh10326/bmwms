SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.UnitsOfMeasure', N'QuantityScale') IS NULL
BEGIN
    ALTER TABLE dbo.UnitsOfMeasure
        ADD QuantityScale TINYINT NOT NULL
            CONSTRAINT DF_UnitsOfMeasure_QuantityScale DEFAULT (0) WITH VALUES;
END;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_UnitsOfMeasure_QuantityScale')
    EXEC(N'ALTER TABLE dbo.UnitsOfMeasure ADD CONSTRAINT CK_UnitsOfMeasure_QuantityScale
        CHECK (QuantityScale BETWEEN 0 AND 4);');

EXEC(N'UPDATE dbo.UnitsOfMeasure
SET QuantityScale = CASE WHEN UnitCode IN (''KG'',''TAN'',''M2'',''M3'') THEN 3 ELSE 0 END;');

COMMIT TRANSACTION;
