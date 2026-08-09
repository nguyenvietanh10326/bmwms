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

        public async Task<List<WarehouseZone>> GetZonesByWarehouseAsync(long warehouseId = 1)
        {
            return await _context.WarehouseZones
                .AsNoTracking()
                .Where(z => (warehouseId <= 0 || z.WarehouseId == warehouseId) && z.Status == "ACTIVE")
                .OrderBy(z => z.ZoneCode)
                .ToListAsync();
        }

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

        public async Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId = 1, long? zoneId = null)
        {
            var query = _context.StorageLocations
                .AsNoTracking()
                .Include(l => l.StorageRack).ThenInclude(r => r!.WarehouseZone)
                .AsQueryable();

            if (warehouseId > 0)
            {
                query = query.Where(l => l.WarehouseId == warehouseId || (l.StorageRack != null && l.StorageRack.WarehouseZone != null && l.StorageRack.WarehouseZone.WarehouseId == warehouseId));
            }

            if (zoneId.HasValue && zoneId.Value > 0)
            {
                query = query.Where(l => l.StorageRack != null && l.StorageRack.ZoneId == zoneId.Value);
            }

            var list = await query
                .Where(l => l.Status == null || (l.Status != "INACTIVE" && l.Status != "DELETED"))
                .OrderBy(l => l.LocationCode)
                .ToListAsync();

            // Fallback: Nếu không có kết quả theo Zone/Warehouse, lấy danh sách tất cả vị trí khả dụng
            if (!list.Any())
            {
                list = await _context.StorageLocations
                    .AsNoTracking()
                    .Include(l => l.StorageRack).ThenInclude(r => r!.WarehouseZone)
                    .Where(l => l.Status == null || l.Status != "INACTIVE")
                    .OrderBy(l => l.LocationCode)
                    .ToListAsync();
            }

            return list;
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
                .Where(o => o.TransferType == "INTERNAL_LOCATION" || o.TransferType == "BIN_TRANSFER")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(o => o.TransferOrderNumber.ToLower().Contains(kw)
                                      || (o.Notes != null && o.Notes.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status.ToUpper() != "ALL")
            {
                var st = status.ToUpper();
                if (st == "PENDING") st = "DRAFT";
                if (st == "REJECTED") st = "CANCELLED";
                query = query.Where(o => o.Status == st);
            }

            if (warehouseId.HasValue && warehouseId > 0)
                query = query.Where(o => o.SourceWarehouseId == warehouseId.Value);

            var allForStats = query;
            int pendingCount  = await allForStats.CountAsync(o => o.Status == "DRAFT" || o.Status == "PENDING" || o.Status == "ASSIGNED");
            int approvedCount = await allForStats.CountAsync(o => o.Status == "COMPLETED");
            int rejectedCount = await allForStats.CountAsync(o => o.Status == "CANCELLED" || o.Status == "REJECTED");
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

        // ── CREATE MULTI-ITEM (Staff) ──────────────────────────────────────────

        public async Task<TransferOrder> CreatePendingOrderAsync(
            long warehouseId,
            List<TransferItemParam> items,
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
                    TransferType           = "INTERNAL_LOCATION",
                    SourceWarehouseId      = warehouseId > 0 ? warehouseId : 1,
                    DestinationWarehouseId = warehouseId > 0 ? warehouseId : 1,
                    RequestedDate          = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status                 = "DRAFT",
                    Notes                  = notes,
                    CreatedByUserId        = createdByUserId > 0 ? createdByUserId : 1,
                    CreatedAt              = DateTime.UtcNow
                };
                _context.TransferOrders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in items)
                {
                    var detail = new TransferOrderDetail
                    {
                        TransferOrderId       = order.TransferOrderId,
                        ProductId             = item.ProductId,
                        ProductLotId          = item.ProductLotId > 0 ? item.ProductLotId : null,
                        SourceLocationId      = item.SourceLocationId > 0 ? item.SourceLocationId : null,
                        DestinationLocationId = item.DestLocationId > 0 ? item.DestLocationId : null,
                        RequestedQuantity     = item.Quantity,
                        MovedQuantity         = 0
                    };
                    _context.TransferOrderDetails.Add(detail);
                }

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

                if (order.Status != "DRAFT" && order.Status != "PENDING" && order.Status != "ASSIGNED")
                    throw new InvalidOperationException($"Phiếu đang ở trạng thái '{order.Status}', không thể duyệt.");

                // Process each detail line in the order
                foreach (var detail in order.TransferOrderDetails)
                {
                    if (!detail.SourceLocationId.HasValue || !detail.DestinationLocationId.HasValue)
                        throw new InvalidOperationException("Phiếu thiếu thông tin ô nguồn hoặc ô đích.");

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

                    var now = DateTime.UtcNow;
                    // Xuất khỏi ô nguồn (TRANSFER_OUT: OnHandDelta < 0, ReservedDelta = 0)
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType       = "TRANSFER_OUT",
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

                    // Nhập vào ô đích (TRANSFER_IN: OnHandDelta > 0, ReservedDelta = 0)
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType       = "TRANSFER_IN",
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

                    detail.MovedQuantity     = detail.RequestedQuantity;
                    detail.ConfirmedByUserId = approvedByUserId;
                    detail.ConfirmedAt       = now;
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

                if (order.Status != "DRAFT" && order.Status != "PENDING" && order.Status != "ASSIGNED")
                    throw new InvalidOperationException("Chỉ có thể từ chối phiếu đang ở trạng thái chờ duyệt.");

                order.Status            = "CANCELLED";
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
