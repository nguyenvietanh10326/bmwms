SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;

;WITH ActiveItems AS
(
    SELECT
        item.StocktakeItemID,
        COALESCE(inventory.ReservedQuantity, 0) AS ReservedQuantity
    FROM dbo.StocktakeItems AS item
    JOIN dbo.StocktakeSessions AS session
      ON session.StocktakeSessionID = item.StocktakeSessionID
    LEFT JOIN dbo.Inventory AS inventory
      ON inventory.ProductID = item.ProductID
     AND inventory.ProductLotID = item.ProductLotID
     AND inventory.StorageLocationID = item.StorageLocationID
    WHERE session.Status IN ('IN_PROGRESS', 'COUNTED', 'PENDING_APPROVAL')
      AND (session.Notes IS NULL OR session.Notes NOT LIKE '%[[]AVAILABLE_SNAPSHOT]%')
)
UPDATE item
SET BookQuantity = CASE
        WHEN item.BookQuantity > active.ReservedQuantity
            THEN item.BookQuantity - active.ReservedQuantity
        ELSE 0
    END,
    CountedQuantity = CASE
        WHEN item.CountedQuantity IS NULL THEN NULL
        WHEN item.CountedQuantity > active.ReservedQuantity
            THEN item.CountedQuantity - active.ReservedQuantity
        ELSE 0
    END,
    AdjustmentQuantity = NULL,
    Resolution = NULL,
    ApprovedByUserID = NULL,
    ApprovedAt = NULL
FROM dbo.StocktakeItems AS item
JOIN ActiveItems AS active
  ON active.StocktakeItemID = item.StocktakeItemID;

UPDATE session
SET Notes = LEFT(
    CASE
        WHEN session.Notes IS NULL OR LTRIM(RTRIM(session.Notes)) = '' THEN
            N'[AVAILABLE_SNAPSHOT] BookQuantity va CountedQuantity dung so luong kha dung san sang xuat ban.'
        ELSE
            session.Notes + CHAR(13) + CHAR(10) +
            N'[AVAILABLE_SNAPSHOT] BookQuantity va CountedQuantity dung so luong kha dung san sang xuat ban.'
    END,
    1000)
FROM dbo.StocktakeSessions AS session
WHERE session.Status IN ('IN_PROGRESS', 'COUNTED', 'PENDING_APPROVAL')
  AND (session.Notes IS NULL OR session.Notes NOT LIKE '%[[]AVAILABLE_SNAPSHOT]%');

COMMIT TRANSACTION;
GO
