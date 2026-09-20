using System.Data;
using BMWMS.Repository.Interfaces.StockOperations;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories.StockOperations
{
    public class TransferRepository : ITransferRepository
    {
        public const string StatusDraft = "DRAFT";
        public const string StatusCompleted = "COMPLETED";
        public const string StatusCancelled = "CANCELLED";

        private readonly BmwmsContext _context;

        public TransferRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<List<WarehouseZone>> GetZonesByWarehouseAsync(long warehouseId = 1, long? productId = null)
        {
            var q = _context.WarehouseZones.Where(z => z.WarehouseId == warehouseId);
            if (productId.HasValue && productId.Value > 0)
            {
                var groupId = await _context.Products
                    .Where(p => p.ProductId == productId.Value)
                    .Select(p => (long?)p.ProductGroupId)
                    .FirstOrDefaultAsync();
                q = groupId.HasValue ? q.Where(z => z.ProductGroupId == groupId.Value) : q.Where(z => false);
            }
            return await q.OrderBy(z => z.ZoneCode).ToListAsync();
        }

        public async Task<List<StorageRack>> GetRacksByZoneAsync(long warehouseId, long? zoneId = null, long? productId = null)
        {
            var q = _context.StorageRacks
                .Include(r => r.WarehouseZone)
                .Where(r => r.WarehouseId == warehouseId);
            if (zoneId.HasValue) q = q.Where(r => r.ZoneId == zoneId.Value);
            if (productId.HasValue && productId.Value > 0)
            {
                var groupId = await _context.Products
                    .Where(p => p.ProductId == productId.Value)
                    .Select(p => (long?)p.ProductGroupId)
                    .FirstOrDefaultAsync();
                q = groupId.HasValue ? q.Where(r => r.WarehouseZone.ProductGroupId == groupId.Value) : q.Where(r => false);
            }
            return await q.OrderBy(r => r.RackCode).ToListAsync();
        }

        public async Task<List<BMWMS.Repository.Models.Inventory>> GetInventoriesByLocationAsync(long locationId) =>
            await _context.Inventories.Include(i => i.Product).Include(i => i.ProductLot)
                .Where(i => i.StorageLocationId == locationId && (i.OnHandQuantity > 0 || i.ReservedQuantity > 0)).ToListAsync();

        public async Task<StorageLocation?> GetLocationWithInventoryAsync(long locationId) =>
            await _context.StorageLocations.Include(l => l.Inventories).ThenInclude(i => i.Product)
                .Include(l => l.Inventories).ThenInclude(i => i.ProductLot)
                .FirstOrDefaultAsync(l => l.StorageLocationId == locationId);

        public async Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null, long? productId = null)
        {
            var q = _context.StorageLocations
                .Include(l => l.StorageRack)
                .ThenInclude(r => r.WarehouseZone)
                .Where(l => l.WarehouseId == warehouseId && l.Status != "BLOCKED");
            if (zoneId.HasValue) q = q.Where(l => l.StorageRack.ZoneId == zoneId.Value);
            if (rackId.HasValue) q = q.Where(l => l.RackId == rackId.Value);
            if (productId.HasValue && productId.Value > 0)
            {
                var groupId = await _context.Products
                    .Where(p => p.ProductId == productId.Value)
                    .Select(p => (long?)p.ProductGroupId)
                    .FirstOrDefaultAsync();
                q = groupId.HasValue ? q.Where(l => l.StorageRack!.WarehouseZone.ProductGroupId == groupId.Value) : q.Where(l => false);
            }
            return await q.OrderBy(l => l.LocationCode).ToListAsync();
        }

        public async Task<List<Warehouse>> GetAllWarehousesAsync() => await _context.Warehouses.ToListAsync();

        public async Task<List<User>> GetStaffUsersAsync() =>
            await _context.Users.Include(u => u.Role).Where(u => u.Role.RoleCode == "WAREHOUSE_STAFF" && u.Status == "ACTIVE").ToListAsync();

        public async Task<(List<TransferOrder> Items, int TotalCount, int DraftCount, int CompletedCount, int CancelledCount)>
            GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize, long? currentStaffId)
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

            if (currentStaffId.HasValue)
                query = query.Where(o => o.CreatedByUserId == currentStaffId.Value || o.AssignedToUserId == currentStaffId.Value);

            var counts = await query.GroupBy(o => o.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
            var totalCount = counts.Sum(x => x.Count);
            var draftCount = counts.FirstOrDefault(x => x.Status == StatusDraft)?.Count ?? 0;
            var completedCount = counts.FirstOrDefault(x => x.Status == StatusCompleted)?.Count ?? 0;
            var cancelledCount = counts.FirstOrDefault(x => x.Status == StatusCancelled)?.Count ?? 0;

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            var items = await query.OrderByDescending(o => o.CreatedAt).Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

            return (items, totalCount, draftCount, completedCount, cancelledCount);
        }

        public async Task<IReadOnlyList<string>> ValidateTransferItemsAsync(
            long warehouseId,
            IReadOnlyCollection<TransferItemParam> items)
        {
            var errors = new List<string>();
            if (items == null || items.Count == 0)
            {
                errors.Add("Phiếu chuyển kho phải có ít nhất 1 dòng hàng.");
                return errors;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var item = items.ElementAt(i);
                var rowLabel = $"Dòng {i + 1}";

                if (item.ProductId <= 0)
                    errors.Add($"{rowLabel}: Chưa chọn sản phẩm.");
                if (item.ProductLotId <= 0)
                    errors.Add($"{rowLabel}: Chưa chọn lô hàng.");
                if (item.SourceLocationId <= 0)
                    errors.Add($"{rowLabel}: Chưa chọn vị trí nguồn.");
                if (item.DestLocationId <= 0)
                    errors.Add($"{rowLabel}: Chưa chọn vị trí đích.");
                if (item.Quantity <= 0)
                    errors.Add($"{rowLabel}: Số lượng chuyển phải lớn hơn 0.");
                if (item.SourceLocationId > 0 && item.SourceLocationId == item.DestLocationId)
                    errors.Add($"{rowLabel}: Vị trí nguồn và đích không được trùng nhau.");
            }

            if (errors.Any()) return errors;

            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var lotIds = items.Select(i => i.ProductLotId).Distinct().ToList();
            var locationIds = items
                .SelectMany(i => new[] { i.SourceLocationId, i.DestLocationId })
                .Distinct()
                .ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .Select(p => new { p.ProductId, p.ProductCode, p.ProductName, p.Status, p.UnitOfMeasure.QuantityScale, p.UnitOfMeasure.UnitName })
                .ToListAsync();
            var lots = await _context.ProductLots
                .Where(l => lotIds.Contains(l.ProductLotId))
                .Select(l => new { l.ProductLotId, l.ProductId, l.LotNumber, l.Status })
                .ToListAsync();
            var locations = await _context.StorageLocations
                .Where(l => locationIds.Contains(l.StorageLocationId))
                .Select(l => new
                {
                    l.StorageLocationId,
                    l.WarehouseId,
                    l.LocationCode,
                    l.Status,
                    l.IsPickable,
                    l.IsPutawayAllowed
                })
                .ToListAsync();

            var sourceGroups = items
                .GroupBy(i => new { i.ProductId, i.ProductLotId, i.SourceLocationId })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductLotId,
                    g.Key.SourceLocationId,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            var sourceProductIds = sourceGroups.Select(g => g.ProductId).Distinct().ToList();
            var sourceLotIds = sourceGroups.Select(g => g.ProductLotId).Distinct().ToList();
            var sourceLocationIds = sourceGroups.Select(g => g.SourceLocationId).Distinct().ToList();
            var inventories = await _context.Inventories
                .Where(i =>
                    sourceProductIds.Contains(i.ProductId) &&
                    sourceLotIds.Contains(i.ProductLotId) &&
                    sourceLocationIds.Contains(i.StorageLocationId))
                .Select(i => new
                {
                    i.ProductId,
                    i.ProductLotId,
                    i.StorageLocationId,
                    AvailableQuantity = i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)
                })
                .ToListAsync();

            foreach (var item in items.Select((value, index) => new { value, index }))
            {
                var rowLabel = $"Dòng {item.index + 1}";
                var product = products.FirstOrDefault(p => p.ProductId == item.value.ProductId);
                var lot = lots.FirstOrDefault(l => l.ProductLotId == item.value.ProductLotId);
                var source = locations.FirstOrDefault(l => l.StorageLocationId == item.value.SourceLocationId);
                var dest = locations.FirstOrDefault(l => l.StorageLocationId == item.value.DestLocationId);

                if (product == null)
                {
                    errors.Add($"{rowLabel}: Sản phẩm không tồn tại.");
                    continue;
                }
                if (!string.Equals(product.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{rowLabel}: Sản phẩm {product.ProductCode} không ở trạng thái ACTIVE.");
                if (item.value.Quantity > 0 && decimal.Round(item.value.Quantity, product.QuantityScale) != item.value.Quantity)
                {
                    var rule = product.QuantityScale == 0
                        ? "số nguyên"
                        : $"tối đa {product.QuantityScale} chữ số thập phân";
                    errors.Add($"{rowLabel}: Số lượng {product.ProductCode} phải là {rule} theo đơn vị {product.UnitName}.");
                }

                if (lot == null)
                    errors.Add($"{rowLabel}: Lô hàng không tồn tại.");
                else
                {
                    if (lot.ProductId != item.value.ProductId)
                        errors.Add($"{rowLabel}: Lô {lot.LotNumber} không thuộc sản phẩm {product.ProductCode}.");
                    if (!string.Equals(lot.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
                        errors.Add($"{rowLabel}: Lô {lot.LotNumber} không khả dụng.");
                }

                if (source == null)
                    errors.Add($"{rowLabel}: Vị trí nguồn không tồn tại.");
                else
                {
                    if (source.WarehouseId != warehouseId)
                        errors.Add($"{rowLabel}: Vị trí nguồn {source.LocationCode} không thuộc kho đã chọn.");
                    if (!source.IsPickable)
                        errors.Add($"{rowLabel}: Vị trí nguồn {source.LocationCode} không cho phép lấy hàng.");
                    if (source.Status is "BLOCKED" or "INACTIVE")
                        errors.Add($"{rowLabel}: Vị trí nguồn {source.LocationCode} đang {source.Status}.");
                }

                if (dest == null)
                    errors.Add($"{rowLabel}: Vị trí đích không tồn tại.");
                else
                {
                    if (dest.WarehouseId != warehouseId)
                        errors.Add($"{rowLabel}: Vị trí đích {dest.LocationCode} không thuộc kho đã chọn.");
                    if (!dest.IsPutawayAllowed)
                        errors.Add($"{rowLabel}: Vị trí đích {dest.LocationCode} không cho phép cất hàng.");
                    if (dest.Status is "BLOCKED" or "INACTIVE")
                        errors.Add($"{rowLabel}: Vị trí đích {dest.LocationCode} đang {dest.Status}.");
                }
            }

            foreach (var group in sourceGroups)
            {
                var inventory = inventories.FirstOrDefault(i =>
                    i.ProductId == group.ProductId &&
                    i.ProductLotId == group.ProductLotId &&
                    i.StorageLocationId == group.SourceLocationId);
                var available = inventory?.AvailableQuantity ?? 0;
                if (available < group.Quantity)
                {
                    var productCode = products.FirstOrDefault(p => p.ProductId == group.ProductId)?.ProductCode ?? group.ProductId.ToString();
                    var locationCode = locations.FirstOrDefault(l => l.StorageLocationId == group.SourceLocationId)?.LocationCode ?? group.SourceLocationId.ToString();
                    errors.Add($"Tồn khả dụng tại {locationCode} cho {productCode} không đủ: yêu cầu {group.Quantity}, khả dụng {available}.");
                }
            }

            return errors;
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

        public async Task<TransferOrder> CancelOrderAsync(long transferOrderId, long cancelledByUserId, string? notes, bool isManager)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var order = await _context.TransferOrders.Include(o => o.TransferOrderDetails)
                    .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId);
                if (order == null) throw new ArgumentException("Không tìm thấy phiếu");
                if (order.Status == StatusCompleted || order.Status == StatusCancelled) 
                    throw new InvalidOperationException("Không thể hủy phiếu đã hoàn thành hoặc đã hủy.");
                if (isManager ? order.Status != StatusDraft :
                    order.Status != StatusDraft || order.CreatedByUserId != cancelledByUserId)
                    throw new UnauthorizedAccessException("Chỉ người tạo hoặc Quản lý kho được hủy phiếu chưa thực hiện.");

                order.Status = StatusCancelled;
                order.Notes = AppendNote(order.Notes, "CANCELLED", notes);

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
            // The service owns the serializable transaction and source inventory locks.
            var order = await _context.TransferOrders.Include(o => o.TransferOrderDetails)
                .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId);
                
            if (order == null) throw new ArgumentException("Phiếu không tồn tại.");
            if (order.Status != StatusDraft) throw new InvalidOperationException("Chỉ thực hiện phiếu chưa hoàn tất.");
            if (order.AssignedToUserId != staffUserId)
                throw new UnauthorizedAccessException("Chỉ nhân viên kho được giao mới được xác nhận chuyển kho.");
            if (items.Count != order.TransferOrderDetails.Count ||
                items.GroupBy(item => item.TransferOrderDetailId).Any(group => group.Count() != 1) ||
                items.Any(item => order.TransferOrderDetails.All(detail => detail.TransferOrderDetailId != item.TransferOrderDetailId)))
                throw new ArgumentException("Dữ liệu xác nhận không khớp các dòng phiếu chuyển kho.");

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

                if (detail.MovedQuantity > 0)
                {
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

