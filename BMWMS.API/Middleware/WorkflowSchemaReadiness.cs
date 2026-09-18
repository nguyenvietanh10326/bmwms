using Microsoft.Data.SqlClient;

namespace BMWMS.API.Middleware;

// A read-only deployment guard, not an automatic migration. Never alter the
// team's database at startup or disguise a missing schema as an empty list.
public sealed class WorkflowSchemaReadiness(IConfiguration configuration, ILogger<WorkflowSchemaReadiness> logger)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTime checkedAt;
    private bool ready;
    public const string PatchFile = "SCHEMA_PATCH_DEMO_REVIEW_2026-09-18.sql";
    public async Task<bool> CheckAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (DateTime.UtcNow - checkedAt < TimeSpan.FromSeconds(ready ? 60 : 5)) return ready;
            using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandTimeout = 5;
            command.CommandText = """
                SELECT CASE WHEN
                  COL_LENGTH('dbo.PurchaseOrders','RowVersion') IS NOT NULL AND
                  COL_LENGTH('dbo.PurchaseOrders','RevisionNo') IS NOT NULL AND
                  COL_LENGTH('dbo.PurchaseOrders','ApprovedAt') IS NOT NULL AND
                  COL_LENGTH('dbo.PurchaseOrders','ApprovedByUserId') IS NOT NULL AND
                  COL_LENGTH('dbo.SalesOrders','RowVersion') IS NOT NULL AND
                  COL_LENGTH('dbo.SalesOrders','RevisionNo') IS NOT NULL AND
                  COL_LENGTH('dbo.PurchaseOrderDetails','IsActive') IS NOT NULL AND
                  COL_LENGTH('dbo.SalesOrderDetails','IsActive') IS NOT NULL AND
                  COL_LENGTH('dbo.InboundOrders','ReturnRequestID') IS NOT NULL AND
                  COL_LENGTH('dbo.InboundOrderItems','ReturnRequestItemID') IS NOT NULL AND
                  OBJECT_ID('dbo.CustomerReturnRequests','U') IS NOT NULL AND
                  OBJECT_ID('dbo.CustomerReturnRequestItems','U') IS NOT NULL AND
                  OBJECT_ID('dbo.CustomerReturnSourceAllocations','U') IS NOT NULL AND
                  OBJECT_ID('dbo.usp_ReserveSalesOrder','P') IS NOT NULL AND
                  OBJECT_ID('dbo.usp_ConfirmOutboundOrder','P') IS NOT NULL AND
                  OBJECT_ID('dbo.usp_ReleaseSalesOrderReservations','P') IS NOT NULL AND
                  (SELECT COUNT(*) FROM sys.triggers WHERE is_disabled = 0 AND name IN (
                    'trg_InventoryTransactions_ApplyToInventory',
                    'trg_InventoryTransactions_Immutable',
                    'trg_InventoryTransactions_BlockStocktakeLockedLocation',
                    'trg_Inventory_BlockStocktakeLockedLocation',
                    'trg_InboundOrderItems_ValidateSource',
                    'trg_CustomerReturnSourceAllocations_Validate',
                    'trg_InboundOrderDetails_Validate',
                    'trg_WarehouseZones_HierarchicalCapacity',
                    'trg_StorageRacks_HierarchicalCapacity',
                    'trg_StorageLocations_HierarchicalCapacity',
                    'trg_WarehouseZones_RequiredQuantityCapacity',
                    'trg_StorageRacks_RequiredQuantityCapacity',
                    'trg_StorageLocations_RequiredQuantityCapacity')) = 13
                THEN 1 ELSE 0 END;
                """;
            ready = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
            if (!ready) logger.LogError("Workflow schema is missing. Apply {PatchFile} to the selected local database before testing orders", PatchFile);
            checkedAt = DateTime.UtcNow;
            return ready;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "Cannot verify workflow database schema");
            ready = false; checkedAt = DateTime.UtcNow;
            return false;
        }
        finally { gate.Release(); }
    }
}
