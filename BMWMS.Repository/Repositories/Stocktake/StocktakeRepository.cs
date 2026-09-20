using BMWMS.Repository.Common;
using BMWMS.Repository.Interfaces.Stocktake;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories.Stocktake
{
    public class StocktakeRepository : IStocktakeRepository
    {
        private const string SessionScheduled = "SCHEDULED";
        private const string SessionInProgress = "IN_PROGRESS";
        private const string SessionCounted = "COUNTED";
        private const string SessionPendingApproval = "PENDING_APPROVAL";
        private const string SessionCompleted = "COMPLETED";
        private const string SessionCancelled = "CANCELLED";
        private const string SessionRejected = "REJECTED";

        private const string LocationPending = "PENDING";
        private const string LocationInProgress = "IN_PROGRESS";
        private const string LocationCounted = "COUNTED";

        private const string ResolutionAcceptDifference = "ACCEPT_DIFFERENCE";
        private const string ResolutionNoAdjustment = "NO_ADJUSTMENT";

        private readonly BmwmsContext _context;

        public StocktakeRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<List<Warehouse>> GetActiveWarehousesAsync()
        {
            return await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Status == "ACTIVE")
                .OrderBy(w => w.WarehouseName)
                .ToListAsync();
        }

        public async Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId, List<long>? rackIds = null, List<long>? productGroupIds = null)
        {
            var query = _context.StorageLocations
                .AsNoTracking()
                .Include(l => l.StorageRack).ThenInclude(r => r!.WarehouseZone)
                .Where(l => l.WarehouseId == warehouseId &&
                    (l.Status == "ACTIVE" || l.Status == "AVAILABLE" || l.Status == "OCCUPIED"));

            if (rackIds != null && rackIds.Any())
            {
                query = query.Where(l => rackIds.Contains(l.RackId ?? 0L));
            }

            if (productGroupIds != null && productGroupIds.Any())
            {
                // Find locations that contain products belonging to the selected product groups
                query = query.Where(l => l.Inventories.Any(i => productGroupIds.Contains(i.Product.ProductGroupId)));
            }

            return await query
                .OrderBy(l => l.LocationCode)
                .ToListAsync();
        }

        public async Task<List<User>> GetAssignableUsersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.Status == "ACTIVE" && u.Role != null &&
                    u.Role.RoleCode == "WAREHOUSE_STAFF")
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<(List<StocktakeSession> Items, int TotalCount, int ScheduledCount, int InProgressCount, int CountedCount, int PendingApprovalCount, int CompletedCount, int CancelledCount)>
            GetPagedSessionsAsync(
                string? keyword,
                string? status,
                long? warehouseId,
                long? assignedToUserId,
                DateOnly? fromDate,
                DateOnly? toDate,
                int pageIndex,
                int pageSize)
        {
            var query = BuildSessionListQuery();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(s =>
                    s.StocktakeNumber.ToLower().Contains(kw) ||
                    (s.Notes != null && s.Notes.ToLower().Contains(kw)));
            }

            if (warehouseId.HasValue && warehouseId.Value > 0)
                query = query.Where(s => s.WarehouseId == warehouseId.Value);

            if (assignedToUserId.HasValue && assignedToUserId.Value > 0)
                query = query.Where(s => s.AssignedToUserId == assignedToUserId.Value);

            if (fromDate.HasValue)
                query = query.Where(s => s.PlannedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.PlannedDate <= toDate.Value);

            var statuslessQuery = query;
            var scheduledCount = await statuslessQuery.CountAsync(s => s.Status == SessionScheduled);
            var inProgressCount = await statuslessQuery.CountAsync(s => s.Status == SessionInProgress);
            var countedCount = await statuslessQuery.CountAsync(s => s.Status == SessionCounted);
            var pendingApprovalCount = await statuslessQuery.CountAsync(s => s.Status == SessionPendingApproval);
            var completedCount = await statuslessQuery.CountAsync(s => s.Status == SessionCompleted);
            var cancelledCount = await statuslessQuery.CountAsync(s => s.Status == SessionCancelled);

            query = ApplyStatusFilter(query, status);

            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            pageSize = pageSize < 1 ? 15 : Math.Min(pageSize, 100);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount, scheduledCount, inProgressCount, countedCount, pendingApprovalCount, completedCount, cancelledCount);
        }

        public async Task<StocktakeSession?> GetSessionDetailAsync(long stocktakeSessionId)
        {
            return await BuildSessionDetailQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StocktakeSessionId == stocktakeSessionId);
        }

        public async Task<StocktakeSession?> GetSessionForUpdateAsync(long stocktakeSessionId)
        {
            return await BuildSessionDetailQuery()
                .FirstOrDefaultAsync(s => s.StocktakeSessionId == stocktakeSessionId);
        }

        public async Task<StocktakeSession> CreateSessionAsync(long warehouseId, DateOnly plannedDate, long createdByUserId, long? assignedToUserId, string? notes, List<long> storageLocationIds)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var warehouse = await _context.Warehouses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.Status == "ACTIVE")
                    ?? throw new InvalidOperationException("Không tìm thấy kho đang hoạt động.");

                if (!assignedToUserId.HasValue || assignedToUserId.Value <= 0)
                    throw new InvalidOperationException("Phải giao phiếu kiểm kho cho một nhân viên kho.");
                await ValidateAssignableUserAsync(assignedToUserId.Value);

                var locationIds = storageLocationIds
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                List<StorageLocation> selectedLocations = new();
                if (locationIds.Any())
                {
                    selectedLocations = await _context.StorageLocations
                        .AsNoTracking()
                        .Where(l => locationIds.Contains(l.StorageLocationId) &&
                            l.WarehouseId == warehouseId &&
                            (l.Status == "ACTIVE" || l.Status == "AVAILABLE" || l.Status == "OCCUPIED"))
                        .ToListAsync();

                    if (selectedLocations.Count != locationIds.Count)
                        throw new InvalidOperationException("Danh sách có vị trí không thuộc kho hoặc không hoạt động.");
                }

                var session = new StocktakeSession
                {
                    StocktakeNumber = $"STK-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}",
                    WarehouseId = warehouse.WarehouseId,
                    PlannedDate = plannedDate,
                    Status = SessionScheduled,
                    CreatedByUserId = createdByUserId > 0 ? createdByUserId : 1,
                    AssignedToUserId = assignedToUserId > 0 ? assignedToUserId : null,
                    CreatedAt = DateTime.UtcNow,
                    Notes = notes
                };

                _context.StocktakeSessions.Add(session);
                await _context.SaveChangesAsync();

                foreach (var location in selectedLocations)
                {
                    _context.StocktakeLocations.Add(new StocktakeLocation
                    {
                        StocktakeSessionId = session.StocktakeSessionId,
                        StorageLocationId = location.StorageLocationId,
                        CountStatus = LocationPending
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return session;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StocktakeSession> StartSessionAsync(long stocktakeSessionId, long startedByUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var session = await _context.StocktakeSessions
                    .Include(s => s.StocktakeLocations)
                    .FirstOrDefaultAsync(s => s.StocktakeSessionId == stocktakeSessionId)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu kiểm kho.");

                if (session.Status != SessionScheduled)
                    throw new InvalidOperationException($"Phiếu đang ở trạng thái '{session.Status}', không thể bắt đầu.");

                if (session.PlannedDate > DateOnly.FromDateTime(DateTime.Today))
                    throw new InvalidOperationException($"Chưa đến ngày thực hiện kiểm kho ({session.PlannedDate:dd/MM/yyyy}). Không thể bắt đầu trước thời gian dự kiến.");

                if (session.AssignedToUserId != startedByUserId)
                    throw new UnauthorizedAccessException("Chỉ nhân viên được giao mới có thể bắt đầu phiếu kiểm kho.");

                if (!session.StocktakeLocations.Any())
                {
                    var activeLocations = await _context.StorageLocations
                        .Where(l => l.WarehouseId == session.WarehouseId &&
                            (l.Status == "ACTIVE" || l.Status == "AVAILABLE" || l.Status == "OCCUPIED"))
                        .OrderBy(l => l.LocationCode)
                        .ToListAsync();

                    if (!activeLocations.Any())
                        throw new InvalidOperationException("Kho chưa có vị trí hoạt động để bắt đầu kiểm kho.");

                    foreach (var location in activeLocations)
                    {
                        session.StocktakeLocations.Add(new StocktakeLocation
                        {
                            StocktakeSessionId = session.StocktakeSessionId,
                            StorageLocationId = location.StorageLocationId,
                            CountStatus = LocationPending
                        });
                    }
                }

                foreach (var location in session.StocktakeLocations)
                {
                    if (string.IsNullOrWhiteSpace(location.CountStatus))
                        location.CountStatus = LocationPending;
                }

                var locationIds = session.StocktakeLocations
                    .Select(l => l.StorageLocationId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                foreach (var locationId in locationIds)
                {
                    _ = await _context.Database.SqlQueryRaw<long>(
                            "SELECT StorageLocationID AS Value FROM dbo.StorageLocations WITH (UPDLOCK, HOLDLOCK) WHERE StorageLocationID = {0}",
                            locationId)
                        .SingleAsync();
                }

                foreach (var locationId in locationIds)
                {
                    var isLocked = await _context.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.StocktakeLocationLocks WHERE StorageLocationID = {0}) THEN 1 ELSE 0 END AS Value",
                            locationId)
                        .SingleAsync();
                    if (isLocked == 1)
                        throw new InvalidOperationException($"Vị trí ID={locationId} đang bị khóa bởi phiếu kiểm kho khác.");
                }

                var inventories = await _context.Inventories
                    .AsNoTracking()
                    .Where(i => locationIds.Contains(i.StorageLocationId) &&
                        (i.OnHandQuantity > 0 || i.ReservedQuantity > 0) &&
                        i.Product.Status == "ACTIVE" &&
                        i.ProductLot.Status == "AVAILABLE" &&
                        i.StorageLocation.LocationType == "BIN" &&
                        i.StorageLocation.IsPickable &&
                        (i.StorageLocation.Status == "ACTIVE" ||
                         i.StorageLocation.Status == "AVAILABLE" ||
                         i.StorageLocation.Status == "OCCUPIED"))
                    .ToListAsync();

                var existingKeys = await _context.StocktakeItems
                    .Where(i => i.StocktakeSessionId == session.StocktakeSessionId)
                    .Select(i => new { i.StorageLocationId, i.ProductId, i.ProductLotId })
                    .ToListAsync();

                var existingSet = existingKeys
                    .Select(k => BuildItemKey(k.StorageLocationId, k.ProductId, k.ProductLotId))
                    .ToHashSet();

                foreach (var inventory in inventories)
                {
                    var key = BuildItemKey(inventory.StorageLocationId, inventory.ProductId, inventory.ProductLotId);
                    if (existingSet.Contains(key))
                        continue;

                    _context.StocktakeItems.Add(new StocktakeItem
                    {
                        StocktakeSessionId = session.StocktakeSessionId,
                        StorageLocationId = inventory.StorageLocationId,
                        ProductId = inventory.ProductId,
                        ProductLotId = inventory.ProductLotId,
                        BookQuantity = inventory.OnHandQuantity - inventory.ReservedQuantity
                    });
                    existingSet.Add(key);
                }

                var now = DateTime.UtcNow;
                session.Status = SessionInProgress;
                session.StartedAt = now;
                session.Notes = AppendNote(session.Notes, "AVAILABLE_SNAPSHOT",
                    "BookQuantity và CountedQuantity dùng số lượng khả dụng sẵn sàng xuất bán.");

                await _context.SaveChangesAsync();
                foreach (var locationId in locationIds)
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($@"
                        INSERT dbo.StocktakeLocationLocks(StorageLocationID, StocktakeSessionID, LockedByUserID, LockedAt)
                        VALUES ({locationId}, {session.StocktakeSessionId}, {startedByUserId}, {now})");
                }
                await transaction.CommitAsync();
                return session;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StocktakeSession> CancelSessionAsync(long stocktakeSessionId, long cancelledByUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var session = await _context.StocktakeSessions
                    .FirstOrDefaultAsync(s => s.StocktakeSessionId == stocktakeSessionId)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu kiểm kho.");

                if (session.Status == SessionCompleted || session.Status == SessionCancelled)
                    throw new InvalidOperationException("Phiếu kiểm kho đã kết thúc, không thể hủy.");

                session.Status = SessionCancelled;
                session.Notes = AppendNote(session.Notes, "Cancel", notes);

                await _context.SaveChangesAsync();
                await ReleaseLocationLocksAsync(stocktakeSessionId);
                await transaction.CommitAsync();
                return session;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StocktakeLocation?> GetLocationCountTaskAsync(long stocktakeSessionId, long storageLocationId)
        {
            return await _context.StocktakeLocations
                .AsNoTracking()
                .Include(l => l.StocktakeSession).ThenInclude(s => s.Warehouse)
                .Include(l => l.StocktakeSession).ThenInclude(s => s.AssignedToUser)
                .Include(l => l.StorageLocation).ThenInclude(sl => sl.StorageRack).ThenInclude(r => r!.WarehouseZone)
                .Include(l => l.StocktakeItems).ThenInclude(i => i.ProductLot).ThenInclude(pl => pl.Product).ThenInclude(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(l => l.StocktakeSessionId == stocktakeSessionId && l.StorageLocationId == storageLocationId);
        }

        public async Task<StocktakeSession> SaveCountsAsync(long stocktakeSessionId, long storageLocationId, List<StocktakeCountUpdateParam> lines, long countedByUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
                if (session.Status != SessionInProgress)
                    throw new InvalidOperationException("Chỉ có thể nhập số đếm khi phiếu đang được kiểm.");

                if (session.PlannedDate > DateOnly.FromDateTime(DateTime.Today))
                    throw new InvalidOperationException($"Chưa đến ngày thực hiện kiểm kho ({session.PlannedDate:dd/MM/yyyy}).");

                var location = session.StocktakeLocations.FirstOrDefault(l => l.StorageLocationId == storageLocationId)
                    ?? throw new InvalidOperationException("Không tìm thấy vị trí trong phiếu kiểm kho.");

                var itemById = location.StocktakeItems.ToDictionary(i => i.StocktakeItemId);
                foreach (var line in lines)
                {
                    if (!itemById.TryGetValue(line.StocktakeItemId, out var item))
                        throw new InvalidOperationException($"Dòng đếm ID={line.StocktakeItemId} không thuộc vị trí này.");

                    if (line.CountedQuantity.HasValue && line.CountedQuantity.Value < 0)
                        throw new InvalidOperationException("Số lượng thực đếm không được âm.");
                    var quantityScale = item.ProductLot.Product.UnitOfMeasure.QuantityScale;
                    if (line.CountedQuantity.HasValue &&
                        decimal.Round(line.CountedQuantity.Value, quantityScale) != line.CountedQuantity.Value)
                        throw new InvalidOperationException(
                            $"Số lượng {item.ProductLot.Product.ProductCode} chỉ được có tối đa {quantityScale} chữ số thập phân.");

                    item.CountedQuantity = line.CountedQuantity;
                    item.CountedByUserId = line.CountedQuantity.HasValue ? countedByUserId : null;
                    item.CountedAt = line.CountedQuantity.HasValue ? DateTime.UtcNow : null;
                    var (targetId, targetCode, _) = StocktakeLocationHelper.ParseTargetLocation(item.Notes);
                    item.Notes = targetId.HasValue
                        ? StocktakeLocationHelper.FormatNotesWithTargetLocation(targetId, targetCode, line.Notes)
                        : line.Notes?.Trim();
                    
                    item.Resolution = null;
                    item.AdjustmentQuantity = null;
                }

                location.CountStatus = location.StocktakeItems.All(i => i.CountedQuantity.HasValue)
                    ? LocationCounted
                    : LocationInProgress;
                location.Notes = AppendNote(location.Notes, "Count", notes);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return session;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StocktakeItem> AddUnbookedItemAsync(
            long stocktakeSessionId,
            AddUnbookedStocktakeItemParam param,
            long countedByUserId)
        {
            var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
            if (session.Status != SessionInProgress)
                throw new InvalidOperationException("Chỉ có thể thêm hàng phát sinh khi phiếu kiểm kho đang ở trạng thái 'Đang kiểm'.");

            if (session.PlannedDate > DateOnly.FromDateTime(DateTime.Today))
                throw new InvalidOperationException($"Chưa đến ngày thực hiện kiểm kho ({session.PlannedDate:dd/MM/yyyy}). Không thể thêm hàng phát sinh.");

            var stocktakeLocation = session.StocktakeLocations
                .FirstOrDefault(l => l.StorageLocationId == param.StorageLocationId);
            if (stocktakeLocation == null)
                throw new InvalidOperationException("Vị trí phát hiện không thuộc phạm vi kiểm kê của phiếu này.");

            var product = await _context.Products
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.ProductGroup)
                .FirstOrDefaultAsync(p => p.ProductId == param.ProductId);
            if (product == null || product.Status != "ACTIVE")
                throw new InvalidOperationException("Sản phẩm không tồn tại hoặc đã bị ngừng hoạt động.");

            var quantityScale = product.UnitOfMeasure.QuantityScale;
            if (param.CountedQuantity <= 0)
                throw new InvalidOperationException("Số lượng đếm thực tế phải lớn hơn 0.");

            if (decimal.Round(param.CountedQuantity, quantityScale) != param.CountedQuantity)
                throw new InvalidOperationException($"Số lượng {product.ProductCode} chỉ được có tối đa {quantityScale} chữ số thập phân.");

            if (product.TrackExpiry && !param.ExpiryDate.HasValue)
                throw new InvalidOperationException($"Sản phẩm '{product.ProductName}' bắt buộc phải nhập Hạn sử dụng.");

            long? targetLocationId = null;
            string? targetLocationCode = null;
            if (param.TargetStorageLocationId.HasValue &&
                param.TargetStorageLocationId.Value > 0 &&
                param.TargetStorageLocationId.Value != param.StorageLocationId)
            {
                var targetLoc = await _context.StorageLocations
                    .Include(l => l.StorageRack)
                        .ThenInclude(r => r.WarehouseZone)
                    .FirstOrDefaultAsync(l => l.StorageLocationId == param.TargetStorageLocationId.Value);

                if (targetLoc == null || targetLoc.Status != "ACTIVE")
                    throw new InvalidOperationException("Vị trí phân bổ được chọn không tồn tại hoặc đã ngừng hoạt động.");

                if (targetLoc.WarehouseId != session.WarehouseId)
                    throw new InvalidOperationException("Vị trí phân bổ phải thuộc cùng kho với phiếu kiểm kê.");

                if (targetLoc.StorageRack?.WarehouseZone?.ProductGroupId.HasValue == true &&
                    targetLoc.StorageRack.WarehouseZone.ProductGroupId.Value != product.ProductGroupId)
                    throw new InvalidOperationException($"Vị trí phân bổ '{targetLoc.LocationCode}' thuộc khu vực không tương thích với nhóm hàng của sản phẩm.");

                targetLocationId = targetLoc.StorageLocationId;
                targetLocationCode = targetLoc.LocationCode;
            }

            var firstReceived = param.FirstReceivedDate ?? DateOnly.FromDateTime(DateTime.Today);
            var lot = await _context.ProductLots
                .FirstOrDefaultAsync(l => l.ProductId == product.ProductId &&
                                          l.ExpiryDate == param.ExpiryDate &&
                                          l.FirstReceivedDate == firstReceived);

            if (lot == null)
            {
                var lotNumber = $"LOT-{product.ProductCode}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                lot = new ProductLot
                {
                    ProductId = product.ProductId,
                    LotNumber = lotNumber,
                    FirstReceivedDate = firstReceived,
                    ExpiryDate = param.ExpiryDate,
                    Status = "AVAILABLE",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductLots.Add(lot);
                await _context.SaveChangesAsync();
            }

            var existingItem = session.StocktakeItems
                .FirstOrDefault(i => i.StorageLocationId == param.StorageLocationId &&
                                     i.ProductId == param.ProductId &&
                                     i.ProductLotId == lot.ProductLotId);

            var now = DateTime.UtcNow;
            string formattedNotes = StocktakeLocationHelper.FormatNotesWithTargetLocation(targetLocationId, targetLocationCode, param.Notes);

            if (existingItem != null)
            {
                existingItem.CountedQuantity = (existingItem.CountedQuantity ?? 0) + param.CountedQuantity;
                existingItem.CountedByUserId = countedByUserId;
                existingItem.CountedAt = now;
                if (!string.IsNullOrWhiteSpace(formattedNotes))
                    existingItem.Notes = string.IsNullOrWhiteSpace(existingItem.Notes)
                        ? formattedNotes
                        : $"{existingItem.Notes} | {formattedNotes}";

                await _context.SaveChangesAsync();
                return existingItem;
            }

            var newItem = new StocktakeItem
            {
                StocktakeSessionId = session.StocktakeSessionId,
                StorageLocationId = param.StorageLocationId,
                ProductId = product.ProductId,
                ProductLotId = lot.ProductLotId,
                BookQuantity = 0,
                CountedQuantity = param.CountedQuantity,
                CountedByUserId = countedByUserId,
                CountedAt = now,
                Notes = formattedNotes
            };

            _context.StocktakeItems.Add(newItem);

            if (stocktakeLocation.CountStatus == LocationPending)
            {
                stocktakeLocation.CountStatus = LocationInProgress;
                stocktakeLocation.CountedByUserId = countedByUserId;
                stocktakeLocation.CountedAt = now;
            }

            await _context.SaveChangesAsync();
            return newItem;
        }

        public async Task<bool> RemoveUnbookedItemAsync(long stocktakeSessionId, long stocktakeItemId, long currentUserId)
        {
            var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
            if (session.Status != SessionInProgress)
                throw new InvalidOperationException("Chỉ có thể xóa hàng phát sinh khi phiếu kiểm kho đang ở trạng thái 'Đang kiểm'.");

            var item = session.StocktakeItems.FirstOrDefault(i => i.StocktakeItemId == stocktakeItemId);
            if (item == null)
                throw new KeyNotFoundException("Không tìm thấy dòng kiểm kho.");

            if (item.BookQuantity > 0)
                throw new InvalidOperationException("Không thể xóa dòng hàng đã có tồn sổ sách. Chỉ có thể xóa dòng hàng phát sinh ngoài sổ (Sổ = 0).");

            _context.StocktakeItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<StocktakeItem> SetTargetLocationAsync(
            long stocktakeSessionId,
            long stocktakeItemId,
            long? targetStorageLocationId,
            long currentUserId)
        {
            var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
            if (session.Status is SessionCompleted or SessionCancelled or SessionRejected)
                throw new InvalidOperationException("Phiếu kiểm kho đã đóng, không thể thay đổi vị trí.");

            var item = session.StocktakeItems.FirstOrDefault(i => i.StocktakeItemId == stocktakeItemId);
            if (item == null)
                throw new KeyNotFoundException("Không tìm thấy dòng kiểm kho.");

            string? targetLocationCode = null;
            if (targetStorageLocationId.HasValue && targetStorageLocationId.Value > 0)
            {
                var loc = await _context.StorageLocations.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.StorageLocationId == targetStorageLocationId.Value && l.WarehouseId == session.WarehouseId)
                    ?? throw new InvalidOperationException("Vị trí mục tiêu không tồn tại hoặc không thuộc kho hiện tại.");

                targetLocationCode = loc.LocationCode;
            }

            item.Notes = StocktakeLocationHelper.FormatNotesWithTargetLocation(
                targetStorageLocationId.HasValue && targetStorageLocationId.Value > 0 ? targetStorageLocationId.Value : null,
                targetLocationCode,
                item.Notes);

            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<List<StorageLocation>> GetCompatibleLocationsAsync(long warehouseId, long productId, decimal quantity)
        {
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
                return new List<StorageLocation>();

            var query = _context.StorageLocations.AsNoTracking()
                .Include(l => l.StorageRack)
                    .ThenInclude(r => r.WarehouseZone)
                .Include(l => l.Inventories)
                .Where(l => l.WarehouseId == warehouseId &&
                            l.Status == "ACTIVE" &&
                            l.IsPutawayAllowed);

            if (product.ProductGroupId > 0)
            {
                query = query.Where(l => l.StorageRack != null &&
                                         l.StorageRack.WarehouseZone.ProductGroupId == product.ProductGroupId);
            }

            return await query.OrderBy(l => l.LocationCode).ToListAsync();
        }

        public async Task<StocktakeSession> SubmitSessionAsync(long stocktakeSessionId, long submittedByUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
                if (session.Status != SessionInProgress)
                    throw new InvalidOperationException("Chỉ có thể gửi kết quả khi phiếu đang được kiểm.");
                if (session.PlannedDate > DateOnly.FromDateTime(DateTime.Today))
                    throw new InvalidOperationException($"Chưa đến ngày thực hiện kiểm kho ({session.PlannedDate:dd/MM/yyyy}).");
                if (session.AssignedToUserId != submittedByUserId)
                    throw new UnauthorizedAccessException("Chỉ nhân viên được giao mới có thể gửi kết quả kiểm kho.");

                var missing = session.StocktakeItems
                    .Where(i => !i.CountedQuantity.HasValue)
                    .Select(i => $"{i.ProductLot.Product.ProductCode}/{i.ProductLot.LotNumber}")
                    .ToList();
                if (missing.Any())
                    throw new InvalidOperationException($"Còn {missing.Count} dòng chưa nhập: {string.Join(", ", missing.Take(5))}.");

                var incompleteLocations = session.StocktakeLocations
                    .Where(location => location.CountStatus != LocationCounted)
                    .Select(location => location.StorageLocation.LocationCode)
                    .ToList();
                if (incompleteLocations.Any())
                    throw new InvalidOperationException($"Còn vị trí chưa nhập xong: {string.Join(", ", incompleteLocations.Take(5))}.");

                var now = DateTime.UtcNow;
                foreach (var location in session.StocktakeLocations)
                {
                    location.CountedByUserId ??= submittedByUserId;
                    location.CountedAt ??= now;
                }

                var hasVariance = session.StocktakeItems.Any(item =>
                    item.CountedQuantity!.Value != item.BookQuantity);
                session.SubmittedAt = now;
                if (hasVariance)
                {
                    session.Status = SessionPendingApproval;
                }
                else
                {
                    foreach (var item in session.StocktakeItems)
                    {
                        item.Resolution = ResolutionNoAdjustment;
                        item.AdjustmentQuantity = 0;
                        item.ApprovedAt = now;
                    }
                    session.Status = SessionCompleted;
                    session.ApprovedAt = now;
                }

                await _context.SaveChangesAsync();
                if (!hasVariance)
                    await ReleaseLocationLocksAsync(stocktakeSessionId);
                await transaction.CommitAsync();
                return session;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StocktakeSession> ApproveSessionAsync(
            long stocktakeSessionId,
            long approvedByUserId,
            string? notes)
        {
            var ownedTransaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
                : null;
            try
            {
                var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
                if (session.Status != SessionPendingApproval)
                    throw new InvalidOperationException("Phiếu kiểm kho chưa sẵn sàng để phê duyệt.");

                if (session.StocktakeLocations.Any(l => l.CountStatus != LocationCounted))
                    throw new InvalidOperationException("Vẫn còn vị trí chưa nhập đủ số đếm.");

                var now = DateTime.UtcNow;
                foreach (var item in session.StocktakeItems.Where(item => item.InventoryTransaction == null))
                {
                    if (!item.CountedQuantity.HasValue)
                        throw new InvalidOperationException("Còn dòng kiểm kho chưa có số đếm.");

                    var diff = CalculateDifference(item);
                    if (diff == 0)
                    {
                        item.Resolution ??= ResolutionNoAdjustment;
                        item.AdjustmentQuantity = 0;
                    }
                    else
                    {
                        item.Resolution = ResolutionAcceptDifference;
                        item.AdjustmentQuantity = diff;
                    }

                    if (item.AdjustmentQuantity.HasValue &&
                        item.AdjustmentQuantity.Value != 0 &&
                        item.InventoryTransaction == null)
                    {
                        var effectiveLocationId = item.StorageLocationId;
                        var (targetId, targetCode, _) = StocktakeLocationHelper.ParseTargetLocation(item.Notes);
                        if (targetId.HasValue && targetId.Value > 0)
                        {
                            effectiveLocationId = targetId.Value;
                        }

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionType = "STOCKTAKE_ADJUSTMENT",
                            ProductId = item.ProductId,
                            StorageLocationId = effectiveLocationId,
                            ProductLotId = item.ProductLotId,
                            OnHandDelta = item.AdjustmentQuantity.Value,
                            ReservedDelta = 0,
                            StocktakeItemId = item.StocktakeItemId,
                            PerformedByUserId = approvedByUserId,
                            TransactionAt = now,
                            Notes = targetId.HasValue
                                ? $"Stocktake {session.StocktakeNumber}: adjustment +{item.AdjustmentQuantity.Value} (Phát hiện tại {item.StorageLocation?.LocationCode}, phân bổ về {targetCode ?? effectiveLocationId.ToString()})"
                                : $"Stocktake {session.StocktakeNumber}: adjustment {item.AdjustmentQuantity.Value}"
                        });
                    }

                    item.ApprovedByUserId = approvedByUserId;
                    item.ApprovedAt = now;
                }

                session.Status = SessionCompleted;
                session.ApprovedByUserId = approvedByUserId;
                session.ApprovedAt = now;
                session.SubmittedAt ??= now;
                session.Notes = AppendNote(session.Notes, "Approve", notes);

                await _context.SaveChangesAsync();
                await ReleaseLocationLocksAsync(stocktakeSessionId);
                if (ownedTransaction != null)
                    await ownedTransaction.CommitAsync();
                return session;
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

        public async Task<StocktakeSession> SaveSessionCountsAsync(
            long stocktakeSessionId,
            List<StocktakeCountUpdateParam> lines,
            List<long> confirmedEmptyLocationIds,
            long countedByUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
                if (session.Status != SessionInProgress)
                    throw new InvalidOperationException("Chỉ có thể nhập số lượng khi phiếu đang được kiểm.");
                if (session.AssignedToUserId != countedByUserId)
                    throw new UnauthorizedAccessException("Chỉ nhân viên được giao mới có thể nhập phiếu kiểm kho này.");

                var duplicateIds = lines.GroupBy(line => line.StocktakeItemId)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();
                if (duplicateIds.Count > 0)
                    throw new InvalidOperationException("Danh sách nhập có dòng hàng bị trùng.");

                var itemById = session.StocktakeItems.ToDictionary(item => item.StocktakeItemId);
                foreach (var line in lines)
                {
                    if (!itemById.TryGetValue(line.StocktakeItemId, out var item))
                        throw new InvalidOperationException($"Dòng kiểm kho ID={line.StocktakeItemId} không thuộc phiếu này.");
                    if (line.CountedQuantity is < 0)
                        throw new InvalidOperationException("Số lượng thực đếm không được âm.");
                    var quantityScale = item.ProductLot.Product.UnitOfMeasure.QuantityScale;
                    if (line.CountedQuantity.HasValue &&
                        decimal.Round(line.CountedQuantity.Value, quantityScale) != line.CountedQuantity.Value)
                        throw new InvalidOperationException(
                            $"Số lượng {item.ProductLot.Product.ProductCode} chỉ được có tối đa {quantityScale} chữ số thập phân.");

                    item.CountedQuantity = line.CountedQuantity;
                    item.CountedByUserId = line.CountedQuantity.HasValue ? countedByUserId : null;
                    item.CountedAt = line.CountedQuantity.HasValue ? DateTime.UtcNow : null;
                    var (targetId, targetCode, _) = StocktakeLocationHelper.ParseTargetLocation(item.Notes);
                    item.Notes = targetId.HasValue
                        ? StocktakeLocationHelper.FormatNotesWithTargetLocation(targetId, targetCode, line.Notes)
                        : line.Notes?.Trim();
                    item.Resolution = null;
                    item.AdjustmentQuantity = null;
                }

                var confirmedEmptySet = confirmedEmptyLocationIds.Where(id => id > 0).ToHashSet();
                if (confirmedEmptySet.Any(id => session.StocktakeLocations.All(location => location.StorageLocationId != id)))
                    throw new InvalidOperationException("Danh sách xác nhận vị trí trống không hợp lệ.");

                foreach (var location in session.StocktakeLocations)
                {
                    var items = location.StocktakeItems.ToList();
                    var isComplete = items.Count > 0
                        ? items.All(item => item.CountedQuantity.HasValue)
                        : confirmedEmptySet.Contains(location.StorageLocationId);
                    location.CountStatus = isComplete ? LocationCounted : LocationInProgress;
                    location.CountedByUserId = isComplete ? countedByUserId : null;
                    location.CountedAt = isComplete ? DateTime.UtcNow : null;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return session;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StocktakeSession> RejectSessionAsync(long stocktakeSessionId, long rejectedByUserId, string reason)
        {
            reason = string.IsNullOrWhiteSpace(reason)
                ? "Quản lý từ chối kết quả kiểm kho."
                : reason.Trim();

            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
                if (session.Status != SessionPendingApproval)
                    throw new InvalidOperationException("Chỉ có thể từ chối phiếu đang chờ phê duyệt.");

                session.Status = SessionRejected;
                session.ApprovedByUserId = rejectedByUserId;
                session.ApprovedAt = DateTime.UtcNow;
                session.Notes = AppendNote(session.Notes, "Reject", reason);

                await _context.SaveChangesAsync();
                await ReleaseLocationLocksAsync(stocktakeSessionId);
                await transaction.CommitAsync();
                return session;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private IQueryable<StocktakeSession> BuildSessionListQuery()
        {
            return _context.StocktakeSessions
                .AsNoTracking()
                .Include(s => s.Warehouse)
                .Include(s => s.CreatedByUser)
                .Include(s => s.AssignedToUser)
                .Include(s => s.ApprovedByUser)
                .Include(s => s.StocktakeLocations)
                .Include(s => s.StocktakeItems);
        }

        private IQueryable<StocktakeSession> BuildSessionDetailQuery()
        {
            return _context.StocktakeSessions
                .Include(s => s.Warehouse)
                .Include(s => s.CreatedByUser)
                .Include(s => s.AssignedToUser)
                .Include(s => s.ApprovedByUser)
                .Include(s => s.StocktakeLocations).ThenInclude(l => l.CountedByUser)
                .Include(s => s.StocktakeLocations).ThenInclude(l => l.StorageLocation).ThenInclude(sl => sl.StorageRack).ThenInclude(r => r!.WarehouseZone)
                .Include(s => s.StocktakeItems).ThenInclude(i => i.CountedByUser)
                .Include(s => s.StocktakeItems).ThenInclude(i => i.ApprovedByUser)
                .Include(s => s.StocktakeItems).ThenInclude(i => i.InventoryTransaction)
                .Include(s => s.StocktakeItems).ThenInclude(i => i.StorageLocation).ThenInclude(sl => sl.StorageRack).ThenInclude(r => r!.WarehouseZone)
                .Include(s => s.StocktakeItems).ThenInclude(i => i.ProductLot).ThenInclude(pl => pl.Product).ThenInclude(p => p.UnitOfMeasure);
        }

        private async Task<StocktakeSession> GetTrackedSessionForMutationAsync(long stocktakeSessionId)
        {
            return await _context.StocktakeSessions
                .Include(s => s.StocktakeLocations)
                    .ThenInclude(l => l.StorageLocation)
                .Include(s => s.StocktakeLocations)
                    .ThenInclude(l => l.StocktakeItems)
                        .ThenInclude(i => i.ProductLot)
                            .ThenInclude(pl => pl.Product)
                                .ThenInclude(product => product.UnitOfMeasure)
                .Include(s => s.StocktakeItems)
                    .ThenInclude(i => i.InventoryTransaction)
                .FirstOrDefaultAsync(s => s.StocktakeSessionId == stocktakeSessionId)
                ?? throw new InvalidOperationException("Không tìm thấy phiếu kiểm kho.");
        }

        private async Task ValidateAssignableUserAsync(long userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Status == "ACTIVE")
                ?? throw new InvalidOperationException("Không tìm thấy nhân viên phụ trách đang hoạt động.");

            var roleCode = user.Role?.RoleCode;
            if (roleCode != "WAREHOUSE_STAFF")
                throw new InvalidOperationException("Người phụ trách phải có vai trò nhân viên kho.");
        }

        private async Task<int> ReleaseLocationLocksAsync(long stocktakeSessionId, IReadOnlySet<long>? locationIds = null)
        {
            if (locationIds == null || locationIds.Count == 0)
                return await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM dbo.StocktakeLocationLocks WHERE StocktakeSessionID = {stocktakeSessionId}");

            var deleted = 0;
            foreach (var locationId in locationIds)
            {
                deleted += await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM dbo.StocktakeLocationLocks WHERE StocktakeSessionID = {stocktakeSessionId} AND StorageLocationID = {locationId}");
            }

            return deleted;
        }

        private static IQueryable<StocktakeSession> ApplyStatusFilter(IQueryable<StocktakeSession> query, string? status)
        {
            if (string.IsNullOrWhiteSpace(status) || status.ToUpperInvariant() == "ALL")
                return query;

            var st = status.Trim().ToUpperInvariant();
            return st switch
            {
                "HISTORY" => query.Where(s => s.Status == SessionCompleted || s.Status == SessionCancelled || s.Status == SessionRejected),
                _ => query.Where(s => s.Status == st)
            };
        }

        private static decimal CalculateDifference(StocktakeItem item)
        {
            return (item.CountedQuantity ?? 0) - item.BookQuantity;
        }

        private static bool IsLocationActive(StorageLocation location)
        {
            return IsLocationActiveStatus(location.Status);
        }

        private static bool IsLocationActiveStatus(string? status)
        {
            return status == "ACTIVE" || status == "AVAILABLE" || status == "OCCUPIED";
        }

        private static string BuildItemKey(long storageLocationId, long productId, long productLotId)
        {
            return $"{storageLocationId}:{productId}:{productLotId}";
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
