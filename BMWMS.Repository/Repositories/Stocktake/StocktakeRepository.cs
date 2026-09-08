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

        private const string LocationPending = "PENDING";
        private const string LocationInProgress = "IN_PROGRESS";
        private const string LocationCounted = "COUNTED";
        private const string LocationRecountRequired = "RECOUNT_REQUIRED";

        private const string ResolutionAcceptDifference = "ACCEPT_DIFFERENCE";
        private const string ResolutionNoAdjustment = "NO_ADJUSTMENT";
        private const string ResolutionRecount = "RECOUNT";

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
                    (u.Role.RoleCode == "SYSTEM_ADMIN" ||
                     u.Role.RoleCode == "WAREHOUSE_MANAGER" ||
                     u.Role.RoleCode == "WAREHOUSE_STAFF"))
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<List<ProductLot>> SearchProductLotsAsync(string? keyword, int take = 20)
        {
            var query = _context.ProductLots
                .AsNoTracking()
                .Include(l => l.Product).ThenInclude(p => p.UnitOfMeasure)
                .Include(l => l.Inventories)
                .Where(l => l.Status == "ACTIVE" && l.Product.Status == "ACTIVE")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(l =>
                    l.LotNumber.ToLower().Contains(kw) ||
                    l.Product.ProductCode.ToLower().Contains(kw) ||
                    l.Product.ProductName.ToLower().Contains(kw));
            }

            return await query
                .OrderBy(l => l.Product.ProductCode)
                .ThenBy(l => l.LotNumber)
                .Take(Math.Clamp(take, 1, 100))
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
                    ?? throw new InvalidOperationException("Khong tim thay kho hoat dong de kiem kho.");

                if (assignedToUserId.HasValue && assignedToUserId.Value > 0)
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
                        throw new InvalidOperationException("Danh sach bin chon co bin khong thuoc kho hoac khong hoat dong.");
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
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var session = await _context.StocktakeSessions
                    .Include(s => s.StocktakeLocations)
                    .FirstOrDefaultAsync(s => s.StocktakeSessionId == stocktakeSessionId)
                    ?? throw new InvalidOperationException("Khong tim thay dot kiem kho.");

                if (session.Status != SessionScheduled)
                    throw new InvalidOperationException($"Dot kiem kho dang o trang thai '{session.Status}', khong the bat dau.");

                if (!session.StocktakeLocations.Any())
                {
                    var activeLocations = await _context.StorageLocations
                        .Where(l => l.WarehouseId == session.WarehouseId &&
                            (l.Status == "ACTIVE" || l.Status == "AVAILABLE" || l.Status == "OCCUPIED"))
                        .OrderBy(l => l.LocationCode)
                        .ToListAsync();

                    if (!activeLocations.Any())
                        throw new InvalidOperationException("Kho chua co bin hoat dong de bat dau kiem kho.");

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
                    .ToList();

                var inventories = await _context.Inventories
                    .AsNoTracking()
                    .Where(i => locationIds.Contains(i.StorageLocationId) && i.OnHandQuantity > 0)
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
                        BookQuantity = inventory.OnHandQuantity
                    });
                    existingSet.Add(key);
                }

                var now = DateTime.UtcNow;
                session.Status = SessionInProgress;
                session.StartedAt = now;

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

        public async Task<StocktakeSession> CancelSessionAsync(long stocktakeSessionId, long cancelledByUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var session = await _context.StocktakeSessions
                    .FirstOrDefaultAsync(s => s.StocktakeSessionId == stocktakeSessionId)
                    ?? throw new InvalidOperationException("Khong tim thay dot kiem kho.");

                if (session.Status == SessionCompleted || session.Status == SessionCancelled)
                    throw new InvalidOperationException("Dot kiem kho da ket thuc, khong the huy.");

                session.Status = SessionCancelled;
                session.Notes = AppendNote(session.Notes, "Cancel", notes);

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
                    throw new InvalidOperationException("Chi co the nhap so dem khi dot kiem kho dang IN_PROGRESS.");

                var location = session.StocktakeLocations.FirstOrDefault(l => l.StorageLocationId == storageLocationId)
                    ?? throw new InvalidOperationException("Khong tim thay bin trong dot kiem kho.");

                if (location.CountStatus == LocationCounted)
                    throw new InvalidOperationException("Bin da submit, can mo recount truoc khi sua so dem.");

                var itemById = location.StocktakeItems.ToDictionary(i => i.StocktakeItemId);
                foreach (var line in lines)
                {
                    if (!itemById.TryGetValue(line.StocktakeItemId, out var item))
                        throw new InvalidOperationException($"Dong dem ID={line.StocktakeItemId} khong thuoc bin nay.");

                    if (line.CountedQuantity.HasValue && line.CountedQuantity.Value < 0)
                        throw new InvalidOperationException("So luong thuc dem khong duoc am.");

                    item.CountedQuantity = line.CountedQuantity;
                    item.CountedByUserId = line.CountedQuantity.HasValue ? countedByUserId : null;
                    item.CountedAt = line.CountedQuantity.HasValue ? DateTime.UtcNow : null;
                    item.Notes = line.Notes;
                    
                    // BR-14 & Recount bug fix: Clear previous recount resolution when a new count is provided
                    if (item.Resolution == ResolutionRecount && line.CountedQuantity.HasValue)
                    {
                        item.Resolution = null;
                        item.AdjustmentQuantity = null;
                    }
                }

                location.CountStatus = LocationInProgress;
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

        public async Task<StocktakeSession> SubmitLocationAsync(long stocktakeSessionId, long storageLocationId, long submittedByUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
                if (session.Status != SessionInProgress)
                    throw new InvalidOperationException("Chi co the submit bin khi dot kiem kho dang IN_PROGRESS.");

                var location = session.StocktakeLocations.FirstOrDefault(l => l.StorageLocationId == storageLocationId)
                    ?? throw new InvalidOperationException("Khong tim thay bin trong dot kiem kho.");

                var missing = location.StocktakeItems
                    .Where(i => !i.CountedQuantity.HasValue)
                    .Select(i => $"{i.ProductLot.Product.ProductCode}/{i.ProductLot.LotNumber}")
                    .ToList();

                if (missing.Any())
                    throw new InvalidOperationException($"Chua nhap so dem cho: {string.Join(", ", missing.Take(5))}.");

                var now = DateTime.UtcNow;
                location.CountStatus = LocationCounted;
                location.CountedByUserId = submittedByUserId;
                location.CountedAt = now;
                location.Notes = AppendNote(location.Notes, "Submit", notes);

                foreach (var item in location.StocktakeItems)
                {
                    if (item.CountedByUserId == null)
                        item.CountedByUserId = submittedByUserId;
                    if (item.CountedAt == null)
                        item.CountedAt = now;
                }

                if (session.StocktakeLocations.Any() &&
                    session.StocktakeLocations.All(l => l.CountStatus == LocationCounted))
                {
                    session.Status = SessionCounted;
                    session.SubmittedAt = now;
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

        public async Task<StocktakeItem> AddUnexpectedItemAsync(long stocktakeSessionId, long storageLocationId, long productId, long productLotId, decimal countedQuantity, long countedByUserId, string? notes)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (countedQuantity <= 0)
                    throw new InvalidOperationException("Hang phat hien them phai co so luong thuc dem lon hon 0.");

                var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
                if (session.Status != SessionInProgress)
                    throw new InvalidOperationException("Chi co the them hang phat sinh khi dot kiem kho dang IN_PROGRESS.");

                var location = session.StocktakeLocations.FirstOrDefault(l => l.StorageLocationId == storageLocationId);
                if (location == null)
                {
                    var storageLocation = await _context.StorageLocations
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.StorageLocationId == storageLocationId &&
                            l.WarehouseId == session.WarehouseId &&
                            (l.Status == "ACTIVE" || l.Status == "AVAILABLE" || l.Status == "OCCUPIED"))
                        ?? throw new InvalidOperationException("Bin khong thuoc kho cua dot kiem hoac khong hoat dong.");

                    location = new StocktakeLocation
                    {
                        StocktakeSessionId = session.StocktakeSessionId,
                        StorageLocationId = storageLocation.StorageLocationId,
                        CountStatus = LocationInProgress
                    };
                    session.StocktakeLocations.Add(location);
                }

                if (location.CountStatus == LocationCounted)
                    throw new InvalidOperationException("Bin da submit, khong the them hang phat sinh.");

                _ = await _context.ProductLots
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.ProductLotId == productLotId && l.ProductId == productId && l.Status == "ACTIVE")
                    ?? throw new InvalidOperationException("Khong tim thay lo san pham hoat dong.");

                var existing = await _context.StocktakeItems
                    .FirstOrDefaultAsync(i =>
                        i.StocktakeSessionId == stocktakeSessionId &&
                        i.StorageLocationId == storageLocationId &&
                        i.ProductId == productId &&
                        i.ProductLotId == productLotId);

                var now = DateTime.UtcNow;
                if (existing != null)
                {
                    if (existing.BookQuantity != 0)
                        throw new InvalidOperationException("Mat hang da co trong snapshot, hay nhap tren dong dem hien co.");

                    existing.CountedQuantity = countedQuantity;
                    existing.CountedByUserId = countedByUserId;
                    existing.CountedAt = now;
                    existing.Notes = notes;
                    location.CountStatus = LocationInProgress;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existing;
                }

                var item = new StocktakeItem
                {
                    StocktakeSessionId = stocktakeSessionId,
                    StorageLocationId = storageLocationId,
                    ProductId = productId,
                    ProductLotId = productLotId,
                    BookQuantity = 0,
                    CountedQuantity = countedQuantity,
                    CountedByUserId = countedByUserId,
                    CountedAt = now,
                    Notes = notes
                };

                _context.StocktakeItems.Add(item);
                location.CountStatus = LocationInProgress;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return item;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StocktakeSession> ApplyResolutionsAsync(long stocktakeSessionId, List<StocktakeResolutionParam> resolutions, long reviewedByUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
                if (session.Status != SessionCounted && session.Status != SessionPendingApproval && session.Status != SessionInProgress)
                    throw new InvalidOperationException("Chi co the ghi nhan xu ly chenhlech khi dot kiem da COUNTED hoac IN_PROGRESS.");

                var allRecount = resolutions.All(r =>
                    r.Resolution?.Trim().ToUpperInvariant() == ResolutionRecount);
                if (!allRecount && session.StocktakeLocations.Any(l => l.CountStatus != LocationCounted))
                    throw new InvalidOperationException("Van con bin chua submit hoac dang yeu cau recount.");

                var anyRecount = false;
                foreach (var resolution in resolutions)
                {
                    var item = session.StocktakeItems.FirstOrDefault(i => i.StocktakeItemId == resolution.StocktakeItemId)
                        ?? throw new InvalidOperationException($"Khong tim thay dong kiem kho ID={resolution.StocktakeItemId}.");

                    if (!item.CountedQuantity.HasValue)
                        throw new InvalidOperationException("Dong kiem kho chua co so dem.");

                    var value = NormalizeResolution(resolution.Resolution);
                    item.Resolution = value;
                    item.Notes = AppendNote(item.Notes, "Resolution", resolution.Notes);

                    if (value == ResolutionRecount)
                    {
                        var location = session.StocktakeLocations.First(l => l.StorageLocationId == item.StorageLocationId);
                        location.CountStatus = LocationRecountRequired;
                        location.CountedByUserId = null;
                        location.CountedAt = null;
                        location.Notes = AppendNote(location.Notes, "Recount", resolution.Notes);

                        item.CountedQuantity = null;
                        item.CountedByUserId = null;
                        item.CountedAt = null;
                        item.AdjustmentQuantity = null;
                        anyRecount = true;
                    }
                    else
                    {
                        var diff = CalculateDifference(item);
                        item.AdjustmentQuantity = value == ResolutionAcceptDifference ? diff : 0;
                    }
                }

                if (anyRecount)
                {
                    session.Status = SessionInProgress;
                    session.SubmittedAt = null;
                }
                else if (AllVarianceItemsResolved(session))
                {
                    session.Status = SessionPendingApproval;
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

        public async Task<StocktakeSession> ApproveSessionAsync(long stocktakeSessionId, long approvedByUserId, string? notes)
        {
            var ownedTransaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
                : null;
            try
            {
                var session = await GetTrackedSessionForMutationAsync(stocktakeSessionId);
                if (session.Status != SessionPendingApproval && session.Status != SessionCounted)
                    throw new InvalidOperationException("Dot kiem kho chua san sang phe duyet.");

                if (session.StocktakeLocations.Any(l => l.CountStatus != LocationCounted))
                    throw new InvalidOperationException("Van con bin chua submit hoac dang yeu cau recount.");

                var now = DateTime.UtcNow;
                foreach (var item in session.StocktakeItems)
                {
                    if (!item.CountedQuantity.HasValue)
                        throw new InvalidOperationException("Con dong kiem kho chua co so dem.");

                    var diff = CalculateDifference(item);
                    if (diff == 0)
                    {
                        item.Resolution ??= ResolutionNoAdjustment;
                        item.AdjustmentQuantity = 0;
                    }
                    else
                    {
                        var resolution = NormalizeResolution(item.Resolution);
                        if (resolution == ResolutionRecount)
                            throw new InvalidOperationException("Con dong yeu cau recount, khong the phe duyet.");

                        item.AdjustmentQuantity = resolution == ResolutionAcceptDifference ? diff : 0;
                    }

                    if (item.AdjustmentQuantity.HasValue &&
                        item.AdjustmentQuantity.Value != 0 &&
                        item.InventoryTransaction == null)
                    {
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionType = "STOCKTAKE_ADJUSTMENT",
                            ProductId = item.ProductId,
                            StorageLocationId = item.StorageLocationId,
                            ProductLotId = item.ProductLotId,
                            OnHandDelta = item.AdjustmentQuantity.Value,
                            ReservedDelta = 0,
                            StocktakeItemId = item.StocktakeItemId,
                            PerformedByUserId = approvedByUserId,
                            TransactionAt = now,
                            Notes = $"Stocktake {session.StocktakeNumber}: adjustment {item.AdjustmentQuantity.Value}"
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
                    .ThenInclude(l => l.StocktakeItems)
                        .ThenInclude(i => i.ProductLot)
                            .ThenInclude(pl => pl.Product)
                .Include(s => s.StocktakeItems)
                .FirstOrDefaultAsync(s => s.StocktakeSessionId == stocktakeSessionId)
                ?? throw new InvalidOperationException("Khong tim thay phien kiem kho.");
        }

        private async Task ValidateAssignableUserAsync(long userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Status == "ACTIVE")
                ?? throw new InvalidOperationException("Khong tim thay nhan vien phu trach hoat dong.");

            var roleCode = user.Role?.RoleCode;
            if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "WAREHOUSE_STAFF")
                throw new InvalidOperationException("Nhan vien phu trach khong thuoc nhom kho.");
        }

        private static IQueryable<StocktakeSession> ApplyStatusFilter(IQueryable<StocktakeSession> query, string? status)
        {
            if (string.IsNullOrWhiteSpace(status) || status.ToUpperInvariant() == "ALL")
                return query;

            var st = status.Trim().ToUpperInvariant();
            return st switch
            {
                "HISTORY" => query.Where(s => s.Status == SessionCompleted || s.Status == SessionCancelled),
                _ => query.Where(s => s.Status == st)
            };
        }

        private static string NormalizeResolution(string? resolution)
        {
            var value = resolution?.Trim().ToUpperInvariant();
            return value switch
            {
                ResolutionAcceptDifference => ResolutionAcceptDifference,
                ResolutionNoAdjustment => ResolutionNoAdjustment,
                ResolutionRecount => ResolutionRecount,
                _ => throw new InvalidOperationException("Resolution khong hop le.")
            };
        }

        private static bool AllVarianceItemsResolved(StocktakeSession session)
        {
            return session.StocktakeItems.All(i =>
            {
                var diff = CalculateDifference(i);
                return diff == 0 || (!string.IsNullOrWhiteSpace(i.Resolution) && i.Resolution != ResolutionRecount);
            });
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
