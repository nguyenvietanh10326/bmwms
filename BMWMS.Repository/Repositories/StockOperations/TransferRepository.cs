using BMWMS.Repository.Interfaces.StockOperations;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories.StockOperations
{
    public class TransferRepository : ITransferRepository
    {
        private readonly BmwmsContext _context;

        public TransferRepository(BmwmsContext context)
        {
            _context = context;
        }

        // ── FORM DATA ──────────────────────────────────────────────────────────

        public async Task<List<BMWMS.Repository.Models.Inventory>> GetInventoriesByLocationAsync(long locationId)
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(i => i.Product).ThenInclude(p => p.UnitOfMeasure)
                .Include(i => i.ProductLot)
                .Where(i => i.StorageLocationId == locationId && i.OnHandQuantity > 0)
                .OrderBy(i => i.Product.ProductName)
                .ThenBy(i => i.ProductLot.LotNumber)
                .ToListAsync();
        }

        public async Task<StorageLocation?> GetLocationWithInventoryAsync(long locationId)
        {
            return await _context.StorageLocations
                .AsNoTracking()
                .Include(l => l.Warehouse)
                .Include(l => l.StorageRack).ThenInclude(r => r!.WarehouseZone)
                .Include(l => l.Inventories)
                .FirstOrDefaultAsync(l => l.StorageLocationId == locationId);
        }

        public async Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId)
        {
            var query = _context.StorageLocations
                .AsNoTracking()
                .Include(l => l.StorageRack).ThenInclude(r => r!.WarehouseZone)
                .AsQueryable();

            if (warehouseId > 0)
            {
                query = query.Where(l => l.WarehouseId == warehouseId);
            }

            return await query
                .Where(l => l.Status.ToUpper() == "ACTIVE" || l.Status == "Active" || string.IsNullOrEmpty(l.Status))
                .OrderBy(l => l.LocationCode)
                .ToListAsync();
        }

        public async Task<List<Warehouse>> GetAllWarehousesAsync()
        {
            return await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Status == "ACTIVE")
                .OrderBy(w => w.WarehouseName)
                .ToListAsync();
        }

        // ── LIST ───────────────────────────────────────────────────────────────

        public async Task<(List<TransferOrder> Items, int TotalCount, int PendingCount, int ApprovedCount, int RejectedCount)>
            GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize)
        {
            var query = _context.TransferOrders
                .AsNoTracking()
                .Include(o => o.CreatedByUser)
                .Include(o => o.ConfirmedByUser)
                .Include(o => o.SourceWarehouse)
                .Include(o => o.TransferOrderDetails)
                .Where(o => o.TransferType == "BIN_TRANSFER")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(o => o.TransferOrderNumber.ToLower().Contains(kw)
                                      || (o.Notes != null && o.Notes.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status.ToUpper() != "ALL")
                query = query.Where(o => o.Status == status.ToUpper());

            if (warehouseId.HasValue && warehouseId > 0)
                query = query.Where(o => o.SourceWarehouseId == warehouseId.Value);

            var allForStats = query;
            int pendingCount  = await allForStats.CountAsync(o => o.Status == "PENDING");
            int approvedCount = await allForStats.CountAsync(o => o.Status == "COMPLETED");
            int rejectedCount = await allForStats.CountAsync(o => o.Status == "REJECTED");
            int totalCount    = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount, pendingCount, approvedCount, rejectedCount);
        }

        // ── DETAIL ─────────────────────────────────────────────────────────────

        public async Task<TransferOrder?> GetOrderWithDetailsAsync(long transferOrderId)
        {
            return await _context.TransferOrders
                .AsNoTracking()
                .Include(o => o.CreatedByUser)
                .Include(o => o.ConfirmedByUser)
                .Include(o => o.SourceWarehouse)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.Product).ThenInclude(p => p.UnitOfMeasure)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.ProductLot)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.SourceLocation)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.DestinationLocation)
                .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId);
        }

        // ── CREATE (Staff) ─────────────────────────────────────────────────────

        public async Task<TransferOrder> CreatePendingOrderAsync(
            long warehouseId,
            long sourceLocationId,
            long destLocationId,
            long productId,
            long productLotId,
            decimal quantity,
            long createdByUserId,
            string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orderNumber = $"BT-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

                var order = new TransferOrder
                {
                    TransferOrderNumber    = orderNumber,
                    TransferType           = "BIN_TRANSFER",
                    SourceWarehouseId      = warehouseId,
                    DestinationWarehouseId = warehouseId, // cùng kho
                    RequestedDate          = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status                 = "PENDING",   // Chờ duyệt
                    Notes                  = notes,
                    CreatedByUserId        = createdByUserId,
                    CreatedAt              = DateTime.UtcNow
                };
                _context.TransferOrders.Add(order);
                await _context.SaveChangesAsync();

                var detail = new TransferOrderDetail
                {
                    TransferOrderId       = order.TransferOrderId,
                    ProductId             = productId,
                    ProductLotId          = productLotId,
                    SourceLocationId      = sourceLocationId,
                    DestinationLocationId = destLocationId,
                    RequestedQuantity     = quantity,
                    MovedQuantity         = 0 // chưa chuyển, chờ duyệt
                };
                _context.TransferOrderDetails.Add(detail);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ── APPROVE (Manager) ──────────────────────────────────────────────────

        public async Task<TransferOrder> ApproveOrderAsync(
            long transferOrderId, long approvedByUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.TransferOrders
                    .Include(o => o.TransferOrderDetails)
                    .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu điều chuyển.");

                if (order.Status != "PENDING")
                    throw new InvalidOperationException($"Phiếu đang ở trạng thái '{order.Status}', không thể duyệt.");

                // Validate tồn kho đủ không
                foreach (var detail in order.TransferOrderDetails)
                {
                    if (!detail.SourceLocationId.HasValue || !detail.DestinationLocationId.HasValue)
                        throw new InvalidOperationException("Phiếu thiếu thông tin ô nguồn/đích.");

                    var srcInv = await _context.Inventories
                        .FirstOrDefaultAsync(i =>
                            i.StorageLocationId == detail.SourceLocationId.Value
                            && i.ProductId == detail.ProductId
                            && i.ProductLotId == detail.ProductLotId);

                    var available = srcInv != null
                        ? (srcInv.AvailableQuantity ?? srcInv.OnHandQuantity - srcInv.ReservedQuantity)
                        : 0;

                    if (detail.RequestedQuantity > available)
                        throw new InvalidOperationException(
                            $"Tồn kho tại ô nguồn không đủ. Yêu cầu: {detail.RequestedQuantity}, Khả dụng: {available}.");

                    // INSERT 2 InventoryTransactions — DB Trigger tự MERGE vào Inventory
                    var now = DateTime.UtcNow;
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType       = "TRANSFER_BIN",
                        ProductId             = detail.ProductId,
                        StorageLocationId     = detail.SourceLocationId.Value,
                        ProductLotId          = detail.ProductLotId!.Value,
                        OnHandDelta           = -detail.RequestedQuantity,
                        ReservedDelta         = 0,
                        TransferOrderDetailId = detail.TransferOrderDetailId,
                        PerformedByUserId     = approvedByUserId,
                        TransactionAt         = now,
                        Notes = $"Duyệt phiếu {order.TransferOrderNumber}: xuất khỏi ô {detail.SourceLocationId}"
                    });
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType       = "TRANSFER_BIN",
                        ProductId             = detail.ProductId,
                        StorageLocationId     = detail.DestinationLocationId.Value,
                        ProductLotId          = detail.ProductLotId!.Value,
                        OnHandDelta           = +detail.RequestedQuantity,
                        ReservedDelta         = 0,
                        TransferOrderDetailId = detail.TransferOrderDetailId,
                        PerformedByUserId     = approvedByUserId,
                        TransactionAt         = now,
                        Notes = $"Duyệt phiếu {order.TransferOrderNumber}: nhập vào ô {detail.DestinationLocationId}"
                    });

                    detail.MovedQuantity      = detail.RequestedQuantity;
                    detail.ConfirmedByUserId  = approvedByUserId;
                    detail.ConfirmedAt        = now;
                }

                order.Status            = "COMPLETED";
                order.ConfirmedByUserId = approvedByUserId;
                order.ConfirmedAt       = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(notes))
                    order.Notes = (order.Notes + "\n[Duyệt] " + notes).Trim();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ── REJECT (Manager) ───────────────────────────────────────────────────

        public async Task<TransferOrder> RejectOrderAsync(
            long transferOrderId, long rejectedByUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.TransferOrders
                    .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu điều chuyển.");

                if (order.Status != "PENDING")
                    throw new InvalidOperationException("Chỉ có thể từ chối phiếu đang ở trạng thái PENDING.");

                order.Status            = "REJECTED";
                order.ConfirmedByUserId = rejectedByUserId;
                order.ConfirmedAt       = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(notes))
                    order.Notes = (order.Notes + "\n[Từ chối] " + notes).Trim();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
