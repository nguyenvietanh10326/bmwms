using System.Data;
using BMWMS.Repository.Interfaces.StockOperations;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories.StockOperations
{
    public class TransferRepository : ITransferRepository
    {
        public const string StatusDraft = "DRAFT";
        public const string StatusApproved = "APPROVED";
        public const string StatusCompleted = "COMPLETED";
        public const string StatusCancelled = "CANCELLED";

        private readonly BmwmsContext _context;

        public TransferRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<List<WarehouseZone>> GetZonesByWarehouseAsync(long warehouseId = 1) =>
            await _context.WarehouseZones.Where(z => z.WarehouseId == warehouseId).ToListAsync();

        public async Task<List<StorageRack>> GetRacksByZoneAsync(long warehouseId, long? zoneId = null)
        {
            var q = _context.StorageRacks.Where(r => r.WarehouseId == warehouseId);
            if (zoneId.HasValue) q = q.Where(r => r.ZoneId == zoneId.Value);
            return await q.ToListAsync();
        }

        public async Task<List<BMWMS.Repository.Models.Inventory>> GetInventoriesByLocationAsync(long locationId) =>
            await _context.Inventories.Include(i => i.Product).Include(i => i.ProductLot)
                .Where(i => i.StorageLocationId == locationId && (i.OnHandQuantity > 0 || i.ReservedQuantity > 0)).ToListAsync();

        public async Task<StorageLocation?> GetLocationWithInventoryAsync(long locationId) =>
            await _context.StorageLocations.Include(l => l.Inventories).ThenInclude(i => i.Product)
                .Include(l => l.Inventories).ThenInclude(i => i.ProductLot)
                .FirstOrDefaultAsync(l => l.StorageLocationId == locationId);

        public async Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null)
        {
            var q = _context.StorageLocations.Where(l => l.WarehouseId == warehouseId && l.Status != "BLOCKED");
            if (zoneId.HasValue) q = q.Where(l => l.StorageRack.ZoneId == zoneId.Value);
            if (rackId.HasValue) q = q.Where(l => l.RackId == rackId.Value);
            return await q.ToListAsync();
        }

        public async Task<List<Warehouse>> GetAllWarehousesAsync() => await _context.Warehouses.ToListAsync();

        public async Task<List<User>> GetStaffUsersAsync() =>
            await _context.Users.Include(u => u.Role).Where(u => u.Role.RoleCode == "WAREHOUSE_STAFF" && u.Status == "ACTIVE").ToListAsync();

        public async Task<(List<TransferOrder> Items, int TotalCount, int DraftCount, int ApprovedCount, int CompletedCount, int CancelledCount)>
            GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize)
        {
            var query = _context.TransferOrders
                .Include(o => o.CreatedByUser)
                .Include(o => o.AssignedToUser)
                .Include(o => o.ApprovedByUser)
                .Include(o => o.ConfirmedByUser)
                .Include(o => o.SourceWarehouse)
                .Include(o => o.DestinationWarehouse)
                .Include(o => o.TransferOrderDetails)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(o => o.TransferOrderNumber.Contains(keyword));

            if (warehouseId.HasValue)
                query = query.Where(o => o.SourceWarehouseId == warehouseId.Value || o.DestinationWarehouseId == warehouseId.Value);

            var counts = await query.GroupBy(o => o.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
            var totalCount = counts.Sum(x => x.Count);
            var draftCount = counts.FirstOrDefault(x => x.Status == StatusDraft)?.Count ?? 0;
            var approvedCount = counts.FirstOrDefault(x => x.Status == StatusApproved)?.Count ?? 0;
            var completedCount = counts.FirstOrDefault(x => x.Status == StatusCompleted)?.Count ?? 0;
            var cancelledCount = counts.FirstOrDefault(x => x.Status == StatusCancelled)?.Count ?? 0;

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            var items = await query.OrderByDescending(o => o.CreatedAt).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

            return (items, totalCount, draftCount, approvedCount, completedCount, cancelledCount);
        }

        public async Task<TransferOrder?> GetOrderWithDetailsAsync(long transferOrderId)
        {
            return await _context.TransferOrders
                .Include(o => o.CreatedByUser)
                .Include(o => o.AssignedToUser)
                .Include(o => o.ApprovedByUser)
                .Include(o => o.ConfirmedByUser)
                .Include(o => o.SourceWarehouse)
                .Include(o => o.DestinationWarehouse)
                .Include(o => o.TransferOrderDetails).ThenInclude(d => d.SourceLocation).ThenInclude(l => l.StorageRack.WarehouseZone)
                .Include(o => o.TransferOrderDetails).ThenInclude(d => d.SourceLocation).ThenInclude(l => l.StorageRack)
                .Include(o => o.TransferOrderDetails).ThenInclude(d => d.DestinationLocation).ThenInclude(l => l.StorageRack.WarehouseZone)
                .Include(o => o.TransferOrderDetails).ThenInclude(d => d.DestinationLocation).ThenInclude(l => l.StorageRack)
                .Include(o => o.TransferOrderDetails).ThenInclude(d => d.Product).ThenInclude(p => p.UnitOfMeasure)
                .Include(o => o.TransferOrderDetails).ThenInclude(d => d.ProductLot)
                .Include(o => o.TransferOrderDetails).ThenInclude(d => d.InventoryTransactions)
                .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId);
        }

        public async Task<TransferOrder> CreatePendingOrderAsync(
            long warehouseId,
            DateOnly? dueDate,
            List<TransferItemParam> items,
            long createdByUserId,
            string? notes)
        {
            var now = DateTime.UtcNow;
            var order = new TransferOrder
            {
                TransferOrderNumber = $"BT-{now:yyyyMMdd}-{now:HHmmssfff}",
                TransferType = "INTERNAL_LOCATION",
                SourceWarehouseId = warehouseId,
                DestinationWarehouseId = warehouseId,
                RequestedDate = DateOnly.FromDateTime(now),
                DueDate = dueDate,
                Status = StatusDraft,
                CreatedByUserId = createdByUserId,
                AssignedToUserId = createdByUserId, // Auto-assign
                CreatedAt = now,
                Notes = notes,
                TransferOrderDetails = items.Select(i => new TransferOrderDetail
                {
                    SourceLocationId = i.SourceLocationId,
                    DestinationLocationId = i.DestLocationId,
                    ProductId = i.ProductId,
                    ProductLotId = i.ProductLotId,
                    RequestedQuantity = i.Quantity,
                    MovedQuantity = 0
                }).ToList()
            };

            _context.TransferOrders.Add(order);
            await _context.SaveChangesAsync();
            return await GetOrderWithDetailsAsync(order.TransferOrderId) ?? order;
        }

        public async Task<TransferOrder> UpdateDraftOrderAsync(
            long transferOrderId,
            long warehouseId,
            DateOnly? dueDate,
            List<TransferItemParam> items,
            long updatedByUserId,
            string? notes)
        {
            var order = await _context.TransferOrders.Include(o => o.TransferOrderDetails).FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId);
            if (order == null) throw new ArgumentException("Phiếu không tồn tại.");
            if (order.Status != StatusDraft) throw new InvalidOperationException("Chỉ được sửa phiếu DRAFT.");
            if (order.CreatedByUserId != updatedByUserId) throw new UnauthorizedAccessException("Chỉ người tạo mới được sửa phiếu.");

            order.DueDate = dueDate;
            order.Notes = AppendNote(order.Notes, "UPDATED", notes);
            _context.TransferOrderDetails.RemoveRange(order.TransferOrderDetails);
            
            order.TransferOrderDetails = items.Select(i => new TransferOrderDetail
            {
                SourceLocationId = i.SourceLocationId,
                DestinationLocationId = i.DestLocationId,
                ProductId = i.ProductId,
                ProductLotId = i.ProductLotId,
                RequestedQuantity = i.Quantity,
                MovedQuantity = 0
            }).ToList();

            await _context.SaveChangesAsync();
            return await GetOrderWithDetailsAsync(order.TransferOrderId) ?? order;
        }

        public async Task<TransferOrder> ApproveOrderAsync(long transferOrderId, long approvedByUserId, string? notes)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                var order = await _context.TransferOrders.Include(o => o.TransferOrderDetails)
                    .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId);
                if (order == null) throw new ArgumentException("Không tìm thấy phiếu");
                if (order.Status != StatusDraft) throw new InvalidOperationException("Chỉ duyệt phiếu DRAFT");

                order.Status = StatusApproved;
                order.ApprovedByUserId = approvedByUserId;
                order.ApprovedAt = DateTime.UtcNow;
                order.Notes = AppendNote(order.Notes, "APPROVED", notes);

                // Thêm InventoryTransaction RESERVE
                foreach (var detail in order.TransferOrderDetails)
                {
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "RESERVE",
                        ProductId = detail.ProductId,
                        StorageLocationId = detail.SourceLocationId.Value,
                        ProductLotId = detail.ProductLotId.Value,
                        OnHandDelta = 0,
                        ReservedDelta = detail.RequestedQuantity,
                        TransferOrderDetailId = detail.TransferOrderDetailId,
                        PerformedByUserId = approvedByUserId,
                        TransactionAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return await GetOrderWithDetailsAsync(order.TransferOrderId) ?? order;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<TransferOrder> CancelOrderAsync(long transferOrderId, long cancelledByUserId, string? notes)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                var order = await _context.TransferOrders.Include(o => o.TransferOrderDetails)
                    .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId);
                if (order == null) throw new ArgumentException("Không tìm thấy phiếu");
                if (order.Status == StatusCompleted || order.Status == StatusCancelled) 
                    throw new InvalidOperationException("Không thể hủy phiếu đã hoàn thành hoặc đã hủy.");

                var oldStatus = order.Status;
                order.Status = StatusCancelled;
                order.Notes = AppendNote(order.Notes, "CANCELLED", notes);

                // Nếu đang APPROVED, trả lại hàng (RELEASE_RESERVATION)
                if (oldStatus == StatusApproved)
                {
                    foreach (var detail in order.TransferOrderDetails)
                    {
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionType = "RELEASE_RESERVATION",
                            ProductId = detail.ProductId,
                            StorageLocationId = detail.SourceLocationId.Value,
                            ProductLotId = detail.ProductLotId.Value,
                            OnHandDelta = 0,
                            ReservedDelta = -detail.RequestedQuantity,
                            TransferOrderDetailId = detail.TransferOrderDetailId,
                            PerformedByUserId = cancelledByUserId,
                            TransactionAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return await GetOrderWithDetailsAsync(order.TransferOrderId) ?? order;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<TransferOrder> ConfirmTransferAsync(
            long transferOrderId, 
            long staffUserId, 
            IReadOnlyCollection<TransferConfirmItemParam> items, 
            string? destinationChangeReason, 
            string? shortfallReason, 
            string? notes)
        {
            // Transaction Serializable for Layer 1 & 2 locks are handled inside Service, 
            // but since we do DB inserts here, we expect Service to have created Transaction, 
            // or we create it here if not passed.
            // Actually, best to do it here.
            
            // Note: The UPDLOCK and HOLDLOCK logic will be in Service, so this repository method 
            // is just doing the final insert. Service passes in the items.
            var order = await _context.TransferOrders.Include(o => o.TransferOrderDetails)
                .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId);
                
            if (order == null) throw new ArgumentException("Phiếu không tồn tại.");
            if (order.Status != StatusApproved) throw new InvalidOperationException("Chỉ xác nhận phiếu APPROVED.");

            order.Status = StatusCompleted;
            order.ConfirmedByUserId = staffUserId;
            order.ConfirmedAt = DateTime.UtcNow;
            
            if (!string.IsNullOrWhiteSpace(destinationChangeReason))
                order.Notes = AppendNote(order.Notes, "DEST_CHANGED", destinationChangeReason);
            if (!string.IsNullOrWhiteSpace(shortfallReason))
                order.Notes = AppendNote(order.Notes, "SHORTFALL", shortfallReason);
            if (!string.IsNullOrWhiteSpace(notes))
                order.Notes = AppendNote(order.Notes, "CONFIRMED", notes);

            foreach (var detail in order.TransferOrderDetails)
            {
                var input = items.FirstOrDefault(i => i.TransferOrderDetailId == detail.TransferOrderDetailId);
                if (input == null) throw new ArgumentException($"Thiếu thông tin xác nhận cho chi tiết {detail.TransferOrderDetailId}");

                detail.MovedQuantity = input.ActualMovedQuantity;
                if (input.DestinationLocationId.HasValue && input.DestinationLocationId.Value != detail.DestinationLocationId)
                {
                    detail.StaffNote = AppendNote(detail.StaffNote, "DEST_CHANGED", $"Đổi từ {detail.DestinationLocationId} sang {input.DestinationLocationId.Value}");
                    detail.DestinationLocationId = input.DestinationLocationId.Value;
                }

                // 1. Release Reservation (trừ lại lượng đã reserve lúc Approve)
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    TransactionType = "RELEASE_RESERVATION",
                    ProductId = detail.ProductId,
                    StorageLocationId = detail.SourceLocationId.Value,
                    ProductLotId = detail.ProductLotId.Value,
                    OnHandDelta = 0,
                    ReservedDelta = -detail.RequestedQuantity,
                    TransferOrderDetailId = detail.TransferOrderDetailId,
                    PerformedByUserId = staffUserId,
                    TransactionAt = DateTime.UtcNow
                });

                if (detail.MovedQuantity > 0)
                {
                    // 2. Transfer Out
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "TRANSFER_OUT",
                        ProductId = detail.ProductId,
                        StorageLocationId = detail.SourceLocationId.Value,
                        ProductLotId = detail.ProductLotId.Value,
                        OnHandDelta = -detail.MovedQuantity,
                        TransferOrderDetailId = detail.TransferOrderDetailId,
                        PerformedByUserId = staffUserId,
                        TransactionAt = DateTime.UtcNow
                    });

                    // 3. Transfer In
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "TRANSFER_IN",
                        ProductId = detail.ProductId,
                        StorageLocationId = detail.DestinationLocationId.Value,
                        ProductLotId = detail.ProductLotId.Value,
                        OnHandDelta = detail.MovedQuantity,
                        TransferOrderDetailId = detail.TransferOrderDetailId,
                        PerformedByUserId = staffUserId,
                        TransactionAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            return await GetOrderWithDetailsAsync(order.TransferOrderId) ?? order;
        }

        private static string? AppendNote(string? existing, string tag, string? note)
        {
            if (string.IsNullOrWhiteSpace(note)) return existing;
            var line = $"[{tag}] {note.Trim()}";
            return string.IsNullOrWhiteSpace(existing) ? line : $"{existing.Trim()}\n{line}";
        }
    }
}

