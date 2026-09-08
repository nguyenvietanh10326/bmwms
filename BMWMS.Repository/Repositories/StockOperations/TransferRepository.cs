using BMWMS.Repository.Interfaces.StockOperations;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories.StockOperations
{
    public class TransferRepository : ITransferRepository
    {
        private const string StatusDraft = "DRAFT";
        private const string StatusAssigned = "ASSIGNED";
        private const string StatusApprovedLegacy = "APPROVED";
        private const string StatusInProgress = "IN_PROGRESS";
        private const string StatusCompleted = "COMPLETED";
        private const string StatusCancelled = "CANCELLED";

        private readonly BmwmsContext _context;

        public TransferRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<List<WarehouseZone>> GetZonesByWarehouseAsync(long warehouseId = 1)
        {
            return await _context.WarehouseZones
                .AsNoTracking()
                .Where(z => (warehouseId <= 0 || z.WarehouseId == warehouseId) && z.Status == "ACTIVE")
                .OrderBy(z => z.ZoneCode)
                .ToListAsync();
        }

        public async Task<List<StorageRack>> GetRacksByZoneAsync(long warehouseId, long? zoneId = null)
        {
            var query = _context.StorageRacks
                .AsNoTracking()
                .Include(r => r.WarehouseZone)
                .Where(r => r.WarehouseId == warehouseId && r.Status == "ACTIVE");

            if (zoneId.HasValue && zoneId.Value > 0)
                query = query.Where(r => r.ZoneId == zoneId.Value);

            return await query.OrderBy(r => r.RackCode).ToListAsync();
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

        public async Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null)
        {
            var query = _context.StorageLocations
                .AsNoTracking()
                .Include(l => l.StorageRack).ThenInclude(r => r!.WarehouseZone)
                .Where(l => l.Status == "ACTIVE" || l.Status == "AVAILABLE" || l.Status == "OCCUPIED")
                .AsQueryable();

            if (warehouseId > 0)
                query = query.Where(l => l.WarehouseId == warehouseId);

            if (zoneId.HasValue && zoneId.Value > 0)
                query = query.Where(l => l.StorageRack != null && l.StorageRack.ZoneId == zoneId.Value);

            if (rackId.HasValue && rackId.Value > 0)
                query = query.Where(l => l.RackId == rackId.Value);

            return await query.OrderBy(l => l.LocationCode).ToListAsync();
        }

        public async Task<List<Warehouse>> GetAllWarehousesAsync()
        {
            return await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Status == "ACTIVE")
                .OrderBy(w => w.WarehouseName)
                .ToListAsync();
        }

        public async Task<List<User>> GetStaffUsersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.Status == "ACTIVE" && u.Role != null &&
                           u.Role.RoleCode == "WAREHOUSE_STAFF")
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<(List<TransferOrder> Items, int TotalCount, int DraftCount, int ApprovedCount, int InProgressCount, int CompletedCount, int CancelledCount)>
            GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize)
        {
            var query = BuildListQuery();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(o => o.TransferOrderNumber.ToLower().Contains(kw)
                                      || (o.Notes != null && o.Notes.ToLower().Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status.ToUpper() != "ALL")
            {
                query = ApplyStatusFilter(query, status);
            }

            if (warehouseId.HasValue && warehouseId > 0)
                query = query.Where(o => o.SourceWarehouseId == warehouseId.Value);

            int draftCount = await query.CountAsync(o => o.Status == StatusDraft);
            int approvedCount = await query.CountAsync(o => o.Status == StatusAssigned || o.Status == StatusApprovedLegacy);
            int totalInProgressCount = await query.CountAsync(o => o.Status == StatusInProgress);
            int postedInProgressCount = await query.CountAsync(o => o.Status == StatusInProgress && o.TransferOrderDetails.Any() && o.TransferOrderDetails.All(d =>
                d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_OUT") &&
                d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_IN")));
            int completedCount = await query.CountAsync(o => o.Status == StatusCompleted) + postedInProgressCount;
            int cancelledCount = await query.CountAsync(o => o.Status == StatusCancelled);
            int totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((Math.Max(pageIndex, 1) - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount, draftCount, approvedCount, totalInProgressCount - postedInProgressCount, completedCount, cancelledCount);
        }

        public async Task<TransferOrder?> GetOrderWithDetailsAsync(long transferOrderId)
        {
            return await BuildDetailQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId);
        }

        public async Task<TransferOrder> CreatePendingOrderAsync(
            long warehouseId,
            long? assignedToUserId,
            DateOnly? dueDate,
            List<TransferItemParam> items,
            long createdByUserId,
            string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await ValidateTransferItemsAsync(warehouseId, items, quantitySelector: i => i.Quantity);
                if (assignedToUserId.HasValue && assignedToUserId.Value > 0)
                    await ValidateWarehouseStaffAsync(assignedToUserId.Value);

                var orderNumber = $"BT-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

                var order = new TransferOrder
                {
                    TransferOrderNumber = orderNumber,
                    TransferType = "INTERNAL_LOCATION",
                    SourceWarehouseId = warehouseId > 0 ? warehouseId : 1,
                    DestinationWarehouseId = warehouseId > 0 ? warehouseId : 1,
                    RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    DueDate = dueDate,
                    Status = StatusDraft,
                    Notes = notes,
                    CreatedByUserId = createdByUserId > 0 ? createdByUserId : 1,
                    AssignedToUserId = assignedToUserId > 0 ? assignedToUserId : null,
                    CreatedAt = DateTime.UtcNow
                };
                _context.TransferOrders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in items)
                {
                    _context.TransferOrderDetails.Add(new TransferOrderDetail
                    {
                        TransferOrderId = order.TransferOrderId,
                        ProductId = item.ProductId,
                        ProductLotId = item.ProductLotId,
                        SourceLocationId = item.SourceLocationId,
                        DestinationLocationId = item.DestLocationId,
                        RequestedQuantity = item.Quantity,
                        MovedQuantity = 0
                    });
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

        public async Task<TransferOrder> ApproveOrderAsync(long transferOrderId, long approvedByUserId, long? assignedToUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await GetTrackedOrderAsync(transferOrderId, includeTransactions: false);

                if (order.Status != StatusDraft)
                    throw new InvalidOperationException($"Phieu dang o trang thai '{order.Status}', khong the duyet.");

                await ValidateOrderDetailsAsync(order, d => d.RequestedQuantity);

                var effectiveAssigneeId = assignedToUserId.HasValue && assignedToUserId.Value > 0
                    ? assignedToUserId.Value
                    : order.AssignedToUserId;
                if (!effectiveAssigneeId.HasValue || effectiveAssigneeId.Value <= 0)
                    throw new InvalidOperationException("Phải phân công một nhân viên kho trước khi duyệt phiếu điều chuyển.");
                await ValidateWarehouseStaffAsync(effectiveAssigneeId.Value);
                await EnsureWarehouseStaffAvailableAsync(effectiveAssigneeId.Value, transferOrderId);

                order.Status = StatusAssigned;
                order.ConfirmedByUserId = approvedByUserId;
                order.ConfirmedAt = DateTime.UtcNow;

                order.AssignedToUserId = effectiveAssigneeId.Value;

                order.Notes = AppendNote(order.Notes, "Duyet", notes);

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

        public async Task<TransferOrder> UpdateDraftOrderAsync(
            long transferOrderId,
            long warehouseId,
            long? assignedToUserId,
            DateOnly? dueDate,
            List<TransferItemParam> items,
            long updatedByUserId,
            string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await GetTrackedOrderAsync(transferOrderId, includeTransactions: false);

                if (order.Status != StatusDraft)
                    throw new InvalidOperationException($"Phieu dang o trang thai '{order.Status}', chi co the sua truoc khi phe duyet.");

                await ValidateTransferItemsAsync(warehouseId, items, quantitySelector: i => i.Quantity);
                if (assignedToUserId.HasValue && assignedToUserId.Value > 0)
                    await ValidateWarehouseStaffAsync(assignedToUserId.Value);

                order.SourceWarehouseId = warehouseId > 0 ? warehouseId : order.SourceWarehouseId;
                order.DestinationWarehouseId = warehouseId > 0 ? warehouseId : order.DestinationWarehouseId;
                order.AssignedToUserId = assignedToUserId > 0 ? assignedToUserId : null;
                order.DueDate = dueDate;
                order.Notes = notes;

                _context.TransferOrderDetails.RemoveRange(order.TransferOrderDetails);
                await _context.SaveChangesAsync();

                foreach (var item in items)
                {
                    _context.TransferOrderDetails.Add(new TransferOrderDetail
                    {
                        TransferOrderId = order.TransferOrderId,
                        ProductId = item.ProductId,
                        ProductLotId = item.ProductLotId,
                        SourceLocationId = item.SourceLocationId,
                        DestinationLocationId = item.DestLocationId,
                        RequestedQuantity = item.Quantity,
                        MovedQuantity = 0
                    });
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

        public async Task<TransferOrder> RejectOrderAsync(long transferOrderId, long rejectedByUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.TransferOrders
                    .FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId)
                    ?? throw new InvalidOperationException("Khong tim thay phieu dieu chuyen.");

                if (await _context.InboundOrders.AnyAsync(o => o.TransferOrderId == transferOrderId) ||
                    await _context.OutboundOrders.AnyAsync(o => o.TransferOrderId == transferOrderId))
                    throw new InvalidOperationException("Phiếu điều chuyển tác nghiệp liên kết phải được xử lý từ phiếu nhập/xuất nguồn.");

                if (order.Status != StatusDraft && !IsApprovedStatus(order.Status))
                    throw new InvalidOperationException("Chi co the tu choi phieu dang o trang thai DRAFT hoac da duyet/chua xuat.");

                order.Status = StatusCancelled;
                order.ConfirmedByUserId = rejectedByUserId;
                order.ConfirmedAt = DateTime.UtcNow;
                order.Notes = AppendNote(order.Notes, "Tu choi", notes);

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

        public async Task<TransferOrder> ConfirmTransferIssueAsync(
            long transferOrderId,
            long staffUserId,
            IReadOnlyCollection<TransferIssueItemParam> items,
            string? notes)
        {
            var ownedTransaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
                : null;
            try
            {
                var order = await GetTrackedOrderAsync(transferOrderId, includeTransactions: true);

                if (!IsApprovedStatus(order.Status))
                    throw new InvalidOperationException($"Phieu dang o trang thai '{order.Status}'. Chi co the xac nhan xuat khi phieu da duoc duyet.");

                await ValidateAssignedOperatorAsync(order, staffUserId);

                await ApplyIssueAsync(order, staffUserId, items, notes);

                await _context.SaveChangesAsync();
                if (ownedTransaction != null)
                    await ownedTransaction.CommitAsync();
                return order;
            }
            catch
            {
                if (ownedTransaction != null)
                    await ownedTransaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (ownedTransaction != null)
                    await ownedTransaction.DisposeAsync();
            }
        }

        public async Task<TransferOrder> ConfirmTransferReceiptAsync(
            long transferOrderId,
            long staffUserId,
            IReadOnlyCollection<TransferReceiptItemParam> items,
            string? destinationChangeReason,
            string? notes)
        {
            var ownedTransaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
                : null;
            try
            {
                var order = await GetTrackedOrderAsync(transferOrderId, includeTransactions: true);

                if (order.Status != StatusInProgress)
                    throw new InvalidOperationException($"Phieu dang o trang thai '{order.Status}'. Chi co the xac nhan nhap khi phieu dang di chuyen.");

                await ValidateAssignedOperatorAsync(order, staffUserId);

                if (HasPostedReceipt(order))
                    CompleteOrder(order, staffUserId, notes);
                else
                    await ApplyReceiptAsync(order, staffUserId, items, destinationChangeReason, notes);

                await _context.SaveChangesAsync();
                if (ownedTransaction != null)
                    await ownedTransaction.CommitAsync();
                return order;
            }
            catch
            {
                if (ownedTransaction != null)
                    await ownedTransaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (ownedTransaction != null)
                    await ownedTransaction.DisposeAsync();
            }
        }

        public async Task<TransferOrder> ConfirmTransferAsync(long transferOrderId, long staffUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await GetTrackedOrderAsync(transferOrderId, includeTransactions: true);

                if (IsApprovedStatus(order.Status))
                {
                    var requestedItems = order.TransferOrderDetails.Select(detail => new TransferIssueItemParam
                    {
                        TransferOrderDetailId = detail.TransferOrderDetailId,
                        ActualMovedQuantity = detail.RequestedQuantity
                    }).ToList();
                    await ApplyIssueAsync(order, staffUserId, requestedItems, notes);
                    await ApplyReceiptAsync(
                        order,
                        staffUserId,
                        order.TransferOrderDetails.Select(detail => new TransferReceiptItemParam
                        {
                            TransferOrderDetailId = detail.TransferOrderDetailId,
                            DestinationLocationId = detail.DestinationLocationId ?? 0
                        }).ToList(),
                        destinationChangeReason: null,
                        notes: notes);
                }
                else if (order.Status == StatusInProgress)
                {
                    if (!HasPostedReceipt(order))
                        await ApplyReceiptAsync(
                            order,
                            staffUserId,
                            order.TransferOrderDetails.Select(detail => new TransferReceiptItemParam
                            {
                                TransferOrderDetailId = detail.TransferOrderDetailId,
                                DestinationLocationId = detail.DestinationLocationId ?? 0
                            }).ToList(),
                            destinationChangeReason: null,
                            notes: notes);
                    else
                        CompleteOrder(order, staffUserId, notes);
                }
                else
                {
                    throw new InvalidOperationException($"Phieu dang o trang thai '{order.Status}', khong the xac nhan.");
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

        private IQueryable<TransferOrder> BuildListQuery()
        {
            return _context.TransferOrders
                .AsNoTracking()
                .Include(o => o.CreatedByUser)
                .Include(o => o.AssignedToUser)
                .Include(o => o.ConfirmedByUser)
                .Include(o => o.SourceWarehouse)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.SourceLocation)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.DestinationLocation)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.InventoryTransactions)
                .Where(o => (o.TransferType == "INTERNAL_LOCATION" || o.TransferType == "BIN_TRANSFER") &&
                            !o.TransferOrderNumber.StartsWith("TR-WF-") &&
                            (o.Notes == null || (!o.Notes.StartsWith("[AUTO_INBOUND]") &&
                                                 !o.Notes.StartsWith("[AUTO_OUTBOUND]"))));
        }

        private IQueryable<TransferOrder> BuildDetailQuery()
        {
            return _context.TransferOrders
                .Include(o => o.CreatedByUser)
                .Include(o => o.AssignedToUser)
                .Include(o => o.ConfirmedByUser)
                .Include(o => o.SourceWarehouse)
                .Include(o => o.DestinationWarehouse)
                .Include(o => o.InboundOrders)
                .Include(o => o.OutboundOrders)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.Product).ThenInclude(p => p.UnitOfMeasure)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.ProductLot)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.ConfirmedByUser)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.InventoryTransactions)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.SourceLocation)
                        .ThenInclude(l => l!.StorageRack)
                            .ThenInclude(r => r!.WarehouseZone)
                .Include(o => o.TransferOrderDetails)
                    .ThenInclude(d => d.DestinationLocation)
                        .ThenInclude(l => l!.StorageRack)
                            .ThenInclude(r => r!.WarehouseZone)
                .Where(o => !o.TransferOrderNumber.StartsWith("TR-WF-") &&
                            (o.Notes == null || (!o.Notes.StartsWith("[AUTO_INBOUND]") &&
                                                 !o.Notes.StartsWith("[AUTO_OUTBOUND]"))));
        }

        private async Task<TransferOrder> GetTrackedOrderAsync(long transferOrderId, bool includeTransactions)
        {
            var query = BuildDetailQuery();

            var order = await query.FirstOrDefaultAsync(o => o.TransferOrderId == transferOrderId)
                ?? throw new InvalidOperationException("Khong tim thay phieu dieu chuyen.");

            if (order.InboundOrders.Count > 0 || order.OutboundOrders.Count > 0)
                throw new InvalidOperationException("Phiếu điều chuyển tác nghiệp liên kết phải được xử lý từ phiếu nhập/xuất nguồn để tránh ghi tồn hai lần.");

            if (!includeTransactions)
                return order;

            return order;
        }

        private async Task ApplyIssueAsync(
            TransferOrder order,
            long staffUserId,
            IReadOnlyCollection<TransferIssueItemParam> items,
            string? notes)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Vui lòng nhập số lượng thực chuyển cho từng dòng hàng.");

            var duplicateDetailId = items.GroupBy(item => item.TransferOrderDetailId).FirstOrDefault(group => group.Count() > 1);
            if (duplicateDetailId != null)
                throw new InvalidOperationException("Dữ liệu số lượng thực chuyển bị trùng dòng hàng.");

            var quantityByDetailId = items.ToDictionary(item => item.TransferOrderDetailId, item => item.ActualMovedQuantity);
            if (quantityByDetailId.Count != order.TransferOrderDetails.Count ||
                order.TransferOrderDetails.Any(detail => !quantityByDetailId.ContainsKey(detail.TransferOrderDetailId)))
                throw new InvalidOperationException("Phải khai báo số lượng thực chuyển cho tất cả dòng hàng trong phiếu.");

            foreach (var detail in order.TransferOrderDetails)
            {
                var actualQuantity = quantityByDetailId[detail.TransferOrderDetailId];
                if (actualQuantity <= 0 || actualQuantity > detail.RequestedQuantity)
                    throw new InvalidOperationException(
                        $"Số lượng thực chuyển của sản phẩm {detail.ProductId} phải lớn hơn 0 và không vượt {detail.RequestedQuantity}.");
            }

            await ValidateOrderDetailsAsync(order, detail => quantityByDetailId[detail.TransferOrderDetailId]);

            var now = DateTime.UtcNow;
            foreach (var detail in order.TransferOrderDetails)
            {
                if (!detail.SourceLocationId.HasValue || !detail.ProductLotId.HasValue)
                    throw new InvalidOperationException("Phiếu thiếu thông tin vị trí nguồn hoặc lớp hàng.");

                detail.MovedQuantity = quantityByDetailId[detail.TransferOrderDetailId];
                detail.ConfirmedByUserId = staffUserId;
                detail.ConfirmedAt = now;
                detail.StaffNote = AppendNote(detail.StaffNote, "Xuat", notes);

                if (!detail.InventoryTransactions.Any(transaction => transaction.TransactionType == "TRANSFER_OUT"))
                {
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "TRANSFER_OUT",
                        ProductId = detail.ProductId,
                        StorageLocationId = detail.SourceLocationId.Value,
                        ProductLotId = detail.ProductLotId.Value,
                        OnHandDelta = -detail.MovedQuantity,
                        ReservedDelta = 0,
                        TransferOrderDetailId = detail.TransferOrderDetailId,
                        PerformedByUserId = staffUserId,
                        TransactionAt = now,
                        Notes = $"Xuất điều chuyển {order.TransferOrderNumber}: {detail.MovedQuantity}"
                    });
                }
            }

            order.Status = StatusInProgress;
            order.ConfirmedByUserId = staffUserId;
            order.ConfirmedAt = now;
            order.Notes = AppendNote(order.Notes, "Xac nhan xuat", notes);
        }

        private async Task ApplyReceiptAsync(
            TransferOrder order,
            long staffUserId,
            IReadOnlyCollection<TransferReceiptItemParam> items,
            string? destinationChangeReason,
            string? notes)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Phải xác nhận vị trí đích cho tất cả dòng hàng.");

            var duplicate = items.GroupBy(item => item.TransferOrderDetailId).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new InvalidOperationException("Dữ liệu vị trí nhận bị trùng dòng hàng.");

            var destinationByDetailId = items.ToDictionary(item => item.TransferOrderDetailId, item => item.DestinationLocationId);
            if (destinationByDetailId.Count != order.TransferOrderDetails.Count ||
                order.TransferOrderDetails.Any(detail => !destinationByDetailId.ContainsKey(detail.TransferOrderDetailId)))
                throw new InvalidOperationException("Phải xác nhận vị trí đích cho tất cả dòng hàng.");

            var destinationChanged = order.TransferOrderDetails.Any(detail =>
                detail.DestinationLocationId != destinationByDetailId[detail.TransferOrderDetailId]);
            if (destinationChanged && string.IsNullOrWhiteSpace(destinationChangeReason))
                throw new InvalidOperationException("Phải nhập lý do khi thay đổi vị trí đích của hàng đang di chuyển.");

            foreach (var detail in order.TransferOrderDetails)
            {
                var destinationId = destinationByDetailId[detail.TransferOrderDetailId];
                if (!detail.SourceLocationId.HasValue || destinationId <= 0 || destinationId == detail.SourceLocationId.Value)
                    throw new InvalidOperationException("Vị trí đích không hợp lệ hoặc trùng vị trí nguồn.");

                var destination = await _context.StorageLocations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(location => location.StorageLocationId == destinationId)
                    ?? throw new InvalidOperationException($"Không tìm thấy vị trí đích ID={destinationId}.");
                if (destination.WarehouseId != order.SourceWarehouseId || !IsLocationActive(destination) || !destination.IsPutawayAllowed)
                    throw new InvalidOperationException($"Vị trí đích '{destination.LocationCode}' không thuộc kho, không hoạt động hoặc không cho phép cất hàng.");

                if (detail.DestinationLocationId != destinationId)
                {
                    detail.StaffNote = AppendNote(
                        detail.StaffNote,
                        "Doi vi tri dich",
                        $"{detail.DestinationLocationId} -> {destinationId}. {destinationChangeReason?.Trim()}");
                    detail.DestinationLocationId = destinationId;
                }
            }

            var now = DateTime.UtcNow;
            foreach (var detail in order.TransferOrderDetails)
            {
                if (!detail.SourceLocationId.HasValue || !detail.DestinationLocationId.HasValue || !detail.ProductLotId.HasValue)
                    throw new InvalidOperationException("Phieu thieu thong tin o nguon, o dich hoac lo hang.");

                if (detail.MovedQuantity <= 0)
                    throw new InvalidOperationException("Chua co so luong da xuat de xac nhan nhap.");

                if (!detail.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_OUT"))
                    throw new InvalidOperationException("Chưa có giao dịch xuất khỏi vị trí nguồn; không thể xác nhận nhập vị trí đích.");

                if (!detail.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_IN"))
                {
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "TRANSFER_IN",
                        ProductId = detail.ProductId,
                        StorageLocationId = detail.DestinationLocationId.Value,
                        ProductLotId = detail.ProductLotId.Value,
                        OnHandDelta = detail.MovedQuantity,
                        ReservedDelta = 0,
                        TransferOrderDetailId = detail.TransferOrderDetailId,
                        PerformedByUserId = staffUserId,
                        TransactionAt = now,
                        Notes = $"Nhap dieu chuyen {order.TransferOrderNumber}: {detail.MovedQuantity}"
                    });
                }

                detail.ConfirmedByUserId = staffUserId;
                detail.ConfirmedAt = now;
                detail.StaffNote = AppendNote(detail.StaffNote, "Nhap", notes);
            }

            order.ConfirmedByUserId = staffUserId;
            order.ConfirmedAt = now;
            order.Status = StatusCompleted;
            order.Notes = AppendNote(order.Notes, "Xac nhan nhap", notes);
        }

        private void CompleteOrder(TransferOrder order, long userId, string? notes)
        {
            order.Status = StatusCompleted;
            order.ConfirmedByUserId = userId;
            order.ConfirmedAt = DateTime.UtcNow;
            order.Notes = AppendNote(order.Notes, "Hoan thanh", notes);
        }

        private async Task ValidateOrderDetailsAsync(TransferOrder order, Func<TransferOrderDetail, decimal> quantitySelector)
        {
            var items = order.TransferOrderDetails.Select(d => new TransferItemParam
            {
                SourceLocationId = d.SourceLocationId ?? 0,
                DestLocationId = d.DestinationLocationId ?? 0,
                ProductId = d.ProductId,
                ProductLotId = d.ProductLotId ?? 0,
                Quantity = quantitySelector(d)
            }).ToList();

            await ValidateTransferItemsAsync(order.SourceWarehouseId, items, i => i.Quantity);
        }

        private async Task ValidateTransferItemsAsync(
            long warehouseId,
            IEnumerable<TransferItemParam> items,
            Func<TransferItemParam, decimal> quantitySelector)
        {
            var itemList = items.ToList();
            if (!itemList.Any())
                throw new InvalidOperationException("Danh sach hang chuyen khong duoc rong.");

            foreach (var item in itemList)
            {
                var quantity = quantitySelector(item);
                if (quantity <= 0)
                    throw new InvalidOperationException("So luong chuyen phai lon hon 0.");

                if (item.ProductLotId <= 0)
                    throw new InvalidOperationException("Phieu chuyen kho bat buoc co thong tin lo hang.");

                if (item.SourceLocationId <= 0 || item.DestLocationId <= 0)
                    throw new InvalidOperationException("Phieu thieu thong tin o nguon hoac o dich.");

                if (item.SourceLocationId == item.DestLocationId)
                    throw new InvalidOperationException("O dich khong duoc trung o nguon.");

                var source = await _context.StorageLocations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.StorageLocationId == item.SourceLocationId)
                    ?? throw new InvalidOperationException($"Khong tim thay o nguon ID={item.SourceLocationId}.");

                var dest = await _context.StorageLocations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.StorageLocationId == item.DestLocationId)
                    ?? throw new InvalidOperationException($"Khong tim thay o dich ID={item.DestLocationId}.");

                if (warehouseId > 0 && (source.WarehouseId != warehouseId || dest.WarehouseId != warehouseId))
                    throw new InvalidOperationException("O nguon va o dich phai thuoc cung kho cua phieu dieu chuyen noi bo.");

                if (!IsLocationActive(source) || !source.IsPickable)
                    throw new InvalidOperationException($"O nguon '{source.LocationCode}' khong san sang xuat hang.");

                if (!IsLocationActive(dest) || !dest.IsPutawayAllowed)
                    throw new InvalidOperationException($"O dich '{dest.LocationCode}' khong cho phep nhan hang.");
            }

            var groupedItems = itemList
                .GroupBy(i => new { i.SourceLocationId, i.ProductId, i.ProductLotId })
                .Select(g => new
                {
                    g.Key.SourceLocationId,
                    g.Key.ProductId,
                    g.Key.ProductLotId,
                    Quantity = g.Sum(quantitySelector)
                });

            foreach (var item in groupedItems)
            {
                var available = await GetAvailableQuantityAsync(item.SourceLocationId, item.ProductId, item.ProductLotId);
                if (item.Quantity > available)
                {
                    throw new InvalidOperationException(
                        $"Ton kho nguon khong du. Yeu cau: {item.Quantity}, kha dung: {available}.");
                }
            }
        }

        private async Task<decimal> GetAvailableQuantityAsync(long sourceLocationId, long productId, long productLotId)
        {
            var inventory = await _context.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.StorageLocationId == sourceLocationId &&
                    i.ProductId == productId &&
                    i.ProductLotId == productLotId);

            return inventory == null
                ? 0
                : inventory.AvailableQuantity ?? (inventory.OnHandQuantity - inventory.ReservedQuantity);
        }

        private async Task ValidateAssignedOperatorAsync(TransferOrder order, long userId)
        {
            await ValidateWarehouseStaffAsync(userId);
            if (order.AssignedToUserId != userId)
                throw new InvalidOperationException("Bạn không phải nhân viên kho được phân công xử lý phiếu điều chuyển này.");
        }

        private async Task ValidateWarehouseStaffAsync(long userId)
        {
            var isWarehouseStaff = await _context.Users
                .AsNoTracking()
                .AnyAsync(user => user.UserId == userId &&
                                  user.Status == "ACTIVE" &&
                                  user.Role.RoleCode == "WAREHOUSE_STAFF");
            if (!isWarehouseStaff)
                throw new InvalidOperationException("Chỉ nhân viên có vai trò WAREHOUSE_STAFF đang hoạt động mới được phân công hoặc xử lý phiếu điều chuyển.");
        }

        private async Task EnsureWarehouseStaffAvailableAsync(long userId, long excludeTransferOrderId)
        {
            var hasActiveInbound = await _context.InboundOrders.AnyAsync(order =>
                order.AssignedToUserId == userId &&
                order.Status != "CANCELLED" &&
                order.Status != "PUTAWAY_COMPLETED" &&
                (order.Status != "COMPLETED" ||
                 order.InboundOrderItems.SelectMany(item => item.InboundOrderDetails)
                     .Any(detail => detail.ConditionStatus == "GOOD" && detail.InventoryTransaction == null)));
            var hasActiveOutbound = await _context.OutboundOrders.AnyAsync(order =>
                order.AssignedToUserId == userId &&
                order.Status != "CANCELLED" &&
                order.Status != "COMPLETED");
            var hasActiveTransfer = await _context.TransferOrders.AnyAsync(order =>
                order.TransferOrderId != excludeTransferOrderId &&
                order.AssignedToUserId == userId &&
                (order.Status == StatusAssigned || order.Status == StatusApprovedLegacy || order.Status == StatusInProgress));

            if (hasActiveInbound || hasActiveOutbound || hasActiveTransfer)
                throw new InvalidOperationException("Nhân viên kho đang phụ trách một phiếu nhập, xuất hoặc điều chuyển khác.");
        }

        private static IQueryable<TransferOrder> ApplyStatusFilter(IQueryable<TransferOrder> query, string status)
        {
            var st = status.Trim().ToUpperInvariant();
            if (st == "PENDING") st = StatusDraft;
            if (st == "REJECTED") st = StatusCancelled;

            return st switch
            {
                "APPROVED" or "ASSIGNED" => query.Where(o => o.Status == StatusAssigned || o.Status == StatusApprovedLegacy),
                "ISSUED" => query.Where(o => o.Status == StatusInProgress && !o.TransferOrderDetails.All(d =>
                    d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_OUT") &&
                    d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_IN"))),
                "RECEIVED" => query.Where(o => o.Status == StatusCompleted || (o.Status == StatusInProgress && o.TransferOrderDetails.Any() && o.TransferOrderDetails.All(d =>
                    d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_OUT") &&
                    d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_IN")))),
                _ => query.Where(o => o.Status == st)
            };
        }

        private static bool IsApprovedStatus(string status)
        {
            return status == StatusAssigned || status == StatusApprovedLegacy;
        }

        private static bool IsLocationActive(StorageLocation location)
        {
            var status = location.Status?.ToUpperInvariant();
            return status == "ACTIVE" || status == "AVAILABLE" || status == "OCCUPIED";
        }

        private static bool HasPostedReceipt(TransferOrder order)
        {
            return order.TransferOrderDetails.Any() &&
                   order.TransferOrderDetails.All(d =>
                       d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_OUT") &&
                       d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_IN"));
        }

        private static string? AppendNote(string? existing, string tag, string? note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return existing;

            var line = $"[{tag}] {note.Trim()}";
            return string.IsNullOrWhiteSpace(existing)
                ? line
                : $"{existing.Trim()}\n{line}";
        }
    }
}
