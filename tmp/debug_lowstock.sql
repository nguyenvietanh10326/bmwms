SELECT TOP 20
    p.ProductID,
    p.ProductCode,
    p.ProductName,
    policy.WarehouseID,
    w.WarehouseCode,
    policy.MinimumStockQuantity,
    SUM(i.OnHandQuantity) AS TotalOnHand,
    SUM(i.ReservedQuantity) AS TotalReserved,
    SUM(i.AvailableQuantity) AS TotalAvailable,
    v.AvailableQuantity AS ViewAvailable,
    v.ShortageQuantity,
    v.WarehouseCode AS ViewWarehouseCode
FROM dbo.Products p
LEFT JOIN dbo.ProductWarehousePolicies policy ON policy.ProductID = p.ProductID
LEFT JOIN dbo.Warehouses w ON w.WarehouseID = policy.WarehouseID
LEFT JOIN dbo.Inventory i ON i.ProductID = p.ProductID
LEFT JOIN dbo.StorageLocations sl ON sl.StorageLocationID = i.StorageLocationID
LEFT JOIN dbo.vw_LowStockAlerts v ON v.ProductID = p.ProductID AND v.WarehouseID = policy.WarehouseID
GROUP BY p.ProductID, p.ProductCode, p.ProductName, policy.WarehouseID, w.WarehouseCode,
         policy.MinimumStockQuantity, v.AvailableQuantity, v.ShortageQuantity, v.WarehouseCode
ORDER BY p.ProductID;
