using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Services;
using BMWMS.Business.Services.StockOperations;
using BMWMS.Repository.Models;
using BMWMS.Repository.Repositories;
using BMWMS.Repository.Repositories.StockOperations;
using Microsoft.EntityFrameworkCore;

// Fixture-only integration smoke test. Never point this executable at a shared database.
var databaseName = args.SingleOrDefault();
if (databaseName is null ||
    !System.Text.RegularExpressions.Regex.IsMatch(databaseName, @"^BMWMS_CODEX_TEST_20260916_[A-Z0-9_]+$"))
{
    Console.Error.WriteLine("Pass exactly one isolated BMWMS_CODEX_TEST_20260916_* database name.");
    return 2;
}

var options = new DbContextOptionsBuilder<BmwmsContext>()
    .UseSqlServer($"Server=localhost;Database={databaseName};Integrated Security=True;TrustServerCertificate=True")
    .Options;
await using var context = new BmwmsContext(options);
var repository = new TransferRepository(context);
var capacity = new ProductGroupCapacityEvaluationService(context);
var audit = new AuditLogService(new AuditLogRepository(context));
var orders = new TransferOrderService(repository, capacity, audit);
var approvals = new TransferApprovalService(repository, audit);
var confirmations = new TransferConfirmService(repository, capacity, audit, context);

// Recover a previous interrupted run without changing non-fixture orders.
var interruptedIds = await context.TransferOrders.AsNoTracking()
    .Where(o => o.Status == "APPROVED" && o.Notes != null &&
                o.Notes.Contains("Fixture transfer smoke test"))
    .Select(o => o.TransferOrderId).ToListAsync();
foreach (var interruptedId in interruptedIds)
    await approvals.CancelTransferAsync(2, interruptedId, "Recover interrupted fixture test", true);

const long productId = 3, lotId = 18, sourceId = 12, destinationId = 11;
const long staffId = 10, otherStaffId = 11, managerId = 2;
var beforeSource = await Stock(sourceId);
var beforeDestination = await Stock(destinationId);
Assert(beforeSource.OnHand >= 1 && beforeSource.Available >= 1, "Fixture source lacks available stock.");

var created = await orders.CreateOrderAsync(staffId, NewOrder(1));
Assert(created.Success && created.TransferOrderId > 0, $"Create failed: {created.Message}");
var orderId = created.TransferOrderId!.Value;
var detailId = (await repository.GetOrderWithDetailsAsync(orderId))!.TransferOrderDetails.Single().TransferOrderDetailId;
await MustReject(() => confirmations.ConfirmAsync(otherStaffId, orderId, Confirmation(1, detailId)), "Unapproved confirmation");
await approvals.ApproveTransferAsync(managerId, new ApproveTransferDto { TransferOrderId = orderId });
var reserved = await Stock(sourceId);
Assert(reserved.Reserved == beforeSource.Reserved + 1 && reserved.OnHand == beforeSource.OnHand, "Approve did not reserve one unit.");
await MustReject(() => confirmations.ConfirmAsync(otherStaffId, orderId, Confirmation(1, detailId)), "Wrong staff confirmation");
await MustReject(() => confirmations.ConfirmAsync(staffId, orderId, Confirmation(1.1m, detailId)), "Over-planned actual");
await MustReject(() => confirmations.ConfirmAsync(staffId, orderId, Confirmation(0.5m, detailId)), "Shortfall without reason");
await confirmations.ConfirmAsync(staffId, orderId, new ConfirmTransferDto
{
    Items = [new ConfirmTransferItemDto { TransferOrderDetailId = detailId, ActualMovedQuantity = 0.5m }],
    ShortfallReason = "Only half physically moved",
    AcknowledgeCapacityWarning = true,
    CapacityWarningReason = "Fixture location capacity cannot be fully verified"
});
var afterSource = await Stock(sourceId);
var afterDestination = await Stock(destinationId);
Assert(afterSource.OnHand == beforeSource.OnHand - 0.5m && afterSource.Reserved == beforeSource.Reserved &&
       afterDestination.OnHand == beforeDestination.OnHand + 0.5m, "Confirm did not post actual quantity/release reserve.");
var completed = (await repository.GetOrderWithDetailsAsync(orderId))!;
Assert(completed.Status == "COMPLETED" && completed.TransferOrderDetails.Single().MovedQuantity == 0.5m,
    "Completed order does not record actual quantity.");

var second = await orders.CreateOrderAsync(staffId, NewOrder(0.25m));
Assert(second.Success && second.TransferOrderId > 0, $"Second create failed: {second.Message}");
var secondId = second.TransferOrderId!.Value;
await MustReject(() => approvals.CancelTransferAsync(otherStaffId, secondId, null, false), "Other staff cancelling draft");
await approvals.ApproveTransferAsync(managerId, new ApproveTransferDto { TransferOrderId = secondId });
await MustReject(() => approvals.CancelTransferAsync(staffId, secondId, null, false), "Staff cancelling approved");
await approvals.CancelTransferAsync(managerId, secondId, "Fixture cancellation", true);
var afterCancel = await Stock(sourceId);
Assert(afterCancel.Reserved == beforeSource.Reserved && afterCancel.OnHand == afterSource.OnHand,
    "Manager cancellation did not release reserve without moving stock.");
Console.WriteLine($"PASS transfer create/approve/confirm/cancel, actual quantity and actor guards; order IDs {orderId}, {secondId}");
return 0;

CreateTransferOrderDto NewOrder(decimal quantity) => new()
{
    WarehouseId = 1, Notes = "Fixture transfer smoke test",
    Items = [new CreateTransferItemDto { ProductId = productId, ProductLotId = lotId,
        SourceLocationId = sourceId, DestLocationId = destinationId, Quantity = quantity }]
};
ConfirmTransferDto Confirmation(decimal quantity, long id) => new()
{
    Items = [new ConfirmTransferItemDto { TransferOrderDetailId = id, ActualMovedQuantity = quantity }]
};
async Task<(decimal OnHand, decimal Reserved, decimal Available)> Stock(long locationId)
{
    var row = await context.Inventories.AsNoTracking().SingleOrDefaultAsync(i => i.ProductId == productId &&
        i.ProductLotId == lotId && i.StorageLocationId == locationId);
    return row is null ? (0, 0, 0) : (row.OnHandQuantity, row.ReservedQuantity,
        row.AvailableQuantity ?? row.OnHandQuantity - row.ReservedQuantity);
}
static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
static async Task MustReject(Func<Task> action, string scenario)
{
    try { await action(); }
    catch (InvalidOperationException) { return; }
    catch (UnauthorizedAccessException) { return; }
    catch (ArgumentException) { return; }
    throw new Exception($"Expected rejection: {scenario}");
}
