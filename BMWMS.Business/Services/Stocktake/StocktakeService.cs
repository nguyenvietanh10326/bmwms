using System.Data;
using BMWMS.Business.Configuration;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.DTOs.Stocktake;
using BMWMS.Business.Interfaces;
using BMWMS.Business.Interfaces.Stocktake;
using BMWMS.Repository.Interfaces.Stocktake;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Business.Services.Stocktake
{
    public class StocktakeService : IStocktakeService
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

        private readonly IStocktakeRepository _stocktakeRepo;
        private readonly BmwmsContext _context;
        private readonly ICapacityEvaluationService _capacityEvaluationService;
        private readonly WarehouseCapacityOptions _capacityOptions;
        private readonly IAuditLogService _auditLogService;

        public StocktakeService(
            IStocktakeRepository stocktakeRepo,
            BmwmsContext context,
            ICapacityEvaluationService capacityEvaluationService,
            WarehouseCapacityOptions capacityOptions,
            IAuditLogService auditLogService)
        {
            _stocktakeRepo = stocktakeRepo;
            _context = context;
            _capacityEvaluationService = capacityEvaluationService;
            _capacityOptions = capacityOptions;
            _auditLogService = auditLogService;
        }

        public async Task<List<StocktakeLocationOptionDto>> GetLocationOptionsAsync(long warehouseId, List<long>? rackIds = null, List<long>? productGroupIds = null)
        {
            var locations = await _stocktakeRepo.GetActiveLocationsByWarehouseAsync(warehouseId, rackIds, productGroupIds);
            return locations.Select(l => new StocktakeLocationOptionDto
            {
                LocationId = l.StorageLocationId,
                LocationCode = l.LocationCode,
                LocationName = l.LocationName ?? l.LocationCode,
                LocationPath = BuildLocationPath(l)
            }).ToList();
        }

        public async Task<List<StocktakeStaffOptionDto>> GetStaffUsersAsync()
        {
            var users = await _stocktakeRepo.GetAssignableUsersAsync();
            return users.Select(u => new StocktakeStaffOptionDto
            {
                UserId = u.UserId,
                FullName = u.FullName ?? u.Username,
                Username = u.Username,
                RoleCode = u.Role?.RoleCode ?? string.Empty,
                RoleName = u.Role?.RoleName ?? string.Empty
            }).ToList();
        }

        public async Task<List<StocktakeProductLotOptionDto>> SearchProductLotsAsync(string? keyword, int take = 20)
        {
            var lots = await _stocktakeRepo.SearchProductLotsAsync(keyword, take);
            return lots.Select(l => new StocktakeProductLotOptionDto
            {
                ProductId = l.ProductId,
                ProductCode = l.Product?.ProductCode ?? string.Empty,
                ProductName = l.Product?.ProductName ?? string.Empty,
                UnitOfMeasureId = l.Product?.UnitOfMeasureId ?? 0,
                UnitCode = l.Product?.UnitOfMeasure?.UnitCode ?? string.Empty,
                UnitName = l.Product?.UnitOfMeasure?.UnitName ?? string.Empty,
                ProductLotId = l.ProductLotId,
                LotNumber = l.LotNumber,
                ExpiryDate = l.ExpiryDate,
                OnHandQuantity = l.Inventories.Sum(i => i.OnHandQuantity)
            }).ToList();
        }

        public async Task<StocktakeSessionPagedResultDto> GetSessionsAsync(StocktakeFilterDto filter, long currentUserId, bool canManage)
        {
            filter.PageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            filter.PageSize = filter.PageSize < 1 ? 15 : Math.Min(filter.PageSize, 100);

            var assignedToUserId = canManage ? filter.AssignedToUserId : currentUserId;
            var result = await _stocktakeRepo.GetPagedSessionsAsync(
                filter.Keyword,
                filter.Status,
                filter.WarehouseId,
                assignedToUserId,
                filter.FromDate,
                filter.ToDate,
                filter.PageIndex,
                filter.PageSize);

            return new StocktakeSessionPagedResultDto
            {
                Items = result.Items.Select(s => MapSessionList(s)).ToList(),
                TotalCount = result.TotalCount,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize,
                ScheduledCount = result.ScheduledCount,
                InProgressCount = result.InProgressCount,
                CountedCount = result.CountedCount,
                PendingApprovalCount = result.PendingApprovalCount,
                CompletedCount = result.CompletedCount,
                CancelledCount = result.CancelledCount
            };
        }

        public async Task<StocktakeSessionDetailDto?> GetSessionDetailAsync(long stocktakeSessionId, long currentUserId, bool canManage)
        {
            var session = await _stocktakeRepo.GetSessionDetailAsync(stocktakeSessionId);
            if (session == null || !CanAccess(session, currentUserId, canManage))
                return null;

            return MapSessionDetail(session, includeBookQuantities: true);
        }

        public async Task<StocktakeCountTaskDto?> GetCountTaskAsync(long stocktakeSessionId, long storageLocationId, long currentUserId, bool canManage)
        {
            var location = await _stocktakeRepo.GetLocationCountTaskAsync(stocktakeSessionId, storageLocationId);
            if (location == null || !CanAccess(location.StocktakeSession, currentUserId, canManage))
                return null;

            var (label, css) = GetLocationStatusBadge(location.CountStatus);
            return new StocktakeCountTaskDto
            {
                StocktakeSessionId = location.StocktakeSessionId,
                StocktakeNumber = location.StocktakeSession.StocktakeNumber,
                SessionStatus = location.StocktakeSession.Status,
                WarehouseId = location.StocktakeSession.WarehouseId,
                WarehouseCode = location.StocktakeSession.Warehouse?.WarehouseCode ?? string.Empty,
                WarehouseName = location.StocktakeSession.Warehouse?.WarehouseName ?? string.Empty,
                StorageLocationId = location.StorageLocationId,
                LocationCode = location.StorageLocation?.LocationCode ?? string.Empty,
                LocationName = location.StorageLocation?.LocationName ?? location.StorageLocation?.LocationCode ?? string.Empty,
                LocationPath = BuildLocationPath(location.StorageLocation),
                CountStatus = location.CountStatus,
                CountStatusLabel = label,
                CountStatusCss = css,
                Notes = location.Notes,
                Lines = location.StocktakeItems
                    .OrderBy(i => i.ProductLot.Product.ProductCode)
                    .ThenBy(i => i.ProductLot.LotNumber)
                    .Select(i => MapCountLine(i, includeBookQuantities: false))
                    .ToList()
            };
        }

        public async Task<StocktakeActionResultDto> CreateSessionAsync(CreateStocktakeSessionDto dto, long createdByUserId)
        {
            if (dto.WarehouseId <= 0)
                return Fail("Kho không hợp lệ.");
            if (dto.PlannedDate == default)
                return Fail("Ngày dự kiến không hợp lệ.");
            if (!dto.AssignedToUserId.HasValue || dto.AssignedToUserId.Value <= 0)
                return Fail("Phải chọn nhân viên kho phụ trách.");

            try
            {
                var session = await _stocktakeRepo.CreateSessionAsync(
                    dto.WarehouseId,
                    dto.PlannedDate,
                    createdByUserId,
                    dto.AssignedToUserId,
                    dto.Notes,
                    dto.StorageLocationIds ?? new List<long>());

                return Success(session, $"Đã tạo phiếu kiểm kho {session.StocktakeNumber}.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        public async Task<StocktakeActionResultDto> StartSessionAsync(long stocktakeSessionId, long startedByUserId)
        {
            try
            {
                var session = await _stocktakeRepo.StartSessionAsync(stocktakeSessionId, startedByUserId);
                return Success(session, $"Đã bắt đầu phiếu {session.StocktakeNumber} và chụp tồn snapshot.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> CancelSessionAsync(long stocktakeSessionId, long cancelledByUserId, string? notes)
        {
            try
            {
                var session = await _stocktakeRepo.CancelSessionAsync(stocktakeSessionId, cancelledByUserId, notes);
                return Success(session, $"Đã hủy phiếu kiểm kho {session.StocktakeNumber}.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> SaveCountsAsync(long stocktakeSessionId, long storageLocationId, List<StocktakeCountLineDto> lines, long countedByUserId, bool canManage, string? notes = null)
        {
            var access = await EnsureAccessAsync(stocktakeSessionId, countedByUserId, canManage: false);
            if (!access)
                return Fail("Bạn không có quyền nhập số đếm cho phiếu này.", stocktakeSessionId);

            try
            {
                var updates = lines.Select(l => new StocktakeCountUpdateParam
                {
                    StocktakeItemId = l.StocktakeItemId,
                    CountedQuantity = l.CountedQuantity,
                    Notes = l.Notes
                }).ToList();

                var session = await _stocktakeRepo.SaveCountsAsync(stocktakeSessionId, storageLocationId, updates, countedByUserId, notes);
                return Success(session, "Đã lưu số đếm.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> SubmitSessionAsync(long stocktakeSessionId, long submittedByUserId)
        {
            var access = await EnsureAccessAsync(stocktakeSessionId, submittedByUserId, canManage: false);
            if (!access)
                return Fail("Bạn không có quyền gửi kết quả phiếu kiểm kho này.", stocktakeSessionId);

            try
            {
                var session = await _stocktakeRepo.SubmitSessionAsync(stocktakeSessionId, submittedByUserId);
                var message = session.Status == SessionCompleted
                    ? "Tất cả số đếm đều khớp. Phiếu kiểm kho đã hoàn tất."
                    : "Đã gửi toàn bộ kết quả chênh lệch cho quản lý phê duyệt.";
                return Success(session, message);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> AddUnexpectedItemAsync(long stocktakeSessionId, UnexpectedStocktakeItemDto dto, long countedByUserId, bool canManage)
        {
            var access = await EnsureAccessAsync(stocktakeSessionId, countedByUserId, canManage: false);
            if (!access)
                return Fail("Bạn không có quyền thêm hàng phát sinh vào phiếu này.", stocktakeSessionId);

            try
            {
                var item = await _stocktakeRepo.AddUnexpectedItemAsync(
                    stocktakeSessionId,
                    dto.StorageLocationId,
                    dto.ProductId,
                    dto.ProductLotId,
                    dto.CountedQuantity,
                    countedByUserId,
                    dto.Notes);

                return new StocktakeActionResultDto
                {
                    Success = true,
                    Message = "Đã thêm hàng phát sinh vào phiếu kiểm kho.",
                    StocktakeSessionId = item.StocktakeSessionId,
                    Status = SessionInProgress
                };
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> ApproveSessionAsync(
            long stocktakeSessionId,
            long approvedByUserId,
            StocktakeNoteDto request)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var snapshot = await _context.StocktakeSessions
                    .AsNoTracking()
                    .Include(session => session.StocktakeItems)
                    .Include(session => session.StocktakeLocations)
                        .ThenInclude(location => location.StorageLocation)
                    .FirstOrDefaultAsync(session => session.StocktakeSessionId == stocktakeSessionId)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu kiểm kho.");

                if (snapshot.Status != SessionPendingApproval)
                    throw new InvalidOperationException("Chỉ có thể phê duyệt phiếu đang chờ xử lý chênh lệch.");

                var inventoryKeys = snapshot.StocktakeItems
                    .Select(item => new { item.ProductId, item.ProductLotId, item.StorageLocationId })
                    .ToList();
                var productIds = inventoryKeys.Select(key => key.ProductId).Distinct().ToList();
                var productLotIds = inventoryKeys.Select(key => key.ProductLotId).Distinct().ToList();
                var storageLocationIds = inventoryKeys.Select(key => key.StorageLocationId).Distinct().ToList();
                var inventoryRows = await _context.Inventories
                    .AsNoTracking()
                    .Where(inventory => productIds.Contains(inventory.ProductId) &&
                        productLotIds.Contains(inventory.ProductLotId) &&
                        storageLocationIds.Contains(inventory.StorageLocationId))
                    .Select(inventory => new
                    {
                        inventory.ProductId,
                        inventory.ProductLotId,
                        inventory.StorageLocationId,
                        inventory.ReservedQuantity
                    })
                    .ToListAsync();
                var reservedByKey = inventoryRows.ToDictionary(
                    row => (row.ProductId, row.ProductLotId, row.StorageLocationId),
                    row => row.ReservedQuantity);
                var exceptionRows = snapshot.StocktakeItems
                    .Where(item => item.CountedQuantity.HasValue &&
                        reservedByKey.GetValueOrDefault((item.ProductId, item.ProductLotId, item.StorageLocationId)) > item.CountedQuantity.Value)
                    .ToList();
                var exceptionItemIds = exceptionRows
                    .Select(item => item.StocktakeItemId)
                    .ToHashSet();

                var adjustments = snapshot.StocktakeItems
                    .Select(item => new
                    {
                        Item = item,
                        Adjustment = (item.CountedQuantity ?? 0) - item.BookQuantity
                    })
                    .Where(row => row.Adjustment != 0 &&
                        !exceptionItemIds.Contains(row.Item.StocktakeItemId))
                    .Select(row => new CapacityAllocationDto
                    {
                        StorageLocationId = row.Item.StorageLocationId,
                        ProductId = row.Item.ProductId,
                        Quantity = row.Adjustment
                    })
                    .ToList();

                var evaluations = new Dictionary<long, LocationCapacityEvaluationDto>();
                if (_capacityOptions.Enabled && adjustments.Count > 0)
                {
                    evaluations = (await _capacityEvaluationService.EvaluateAsync(
                            adjustments,
                            acquireLocationLocks: true))
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    EnsureStocktakeCapacityDecision(
                        evaluations.Values,
                        request.AcknowledgeCapacityWarning,
                        request.CapacityWarningReason);
                }

                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sys.sp_set_session_context @key=N'StocktakeSessionID', @value={stocktakeSessionId}");

                var session = await _stocktakeRepo.ApproveSessionAsync(
                    stocktakeSessionId,
                    approvedByUserId,
                    request.Notes,
                    exceptionItemIds);
                await _auditLogService.StageAsync(new AuditEventDto
                {
                    UserId = approvedByUserId,
                    ActionType = "APPROVE_STOCKTAKE",
                    EntityName = AuditEntities.StocktakeSession,
                    EntityId = session.StocktakeSessionId.ToString(),
                    NewValues = new
                    {
                        session.StocktakeNumber,
                        session.Status,
                        Adjustments = adjustments,
                        CapacityEnabled = _capacityOptions.Enabled,
                        CapacityEvaluations = evaluations.Values,
                        WarningAcknowledged = request.AcknowledgeCapacityWarning,
                        WarningReason = request.CapacityWarningReason?.Trim()
                    }
                });
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                var message = exceptionRows.Count == 0
                    ? $"Đã khớp tồn và hoàn tất phiếu kiểm kho {session.StocktakeNumber}."
                    : $"Cảnh báo: các vị trí {string.Join(", ", exceptionRows
                        .Select(item => snapshot.StocktakeLocations
                            .FirstOrDefault(location => location.StorageLocationId == item.StorageLocationId)
                            ?.StorageLocation?.LocationCode ?? $"ID {item.StorageLocationId}")
                        .Distinct())} không thể khớp vì số đếm nhỏ hơn số lượng đã giữ. Các bin được chọn hợp lệ đã khớp; phiếu đã hoàn tất và đã mở khóa các vị trí.";
                return Success(session, message);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return Fail(ex.Message, stocktakeSessionId);
            }
            finally
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sys.sp_set_session_context @key=N'StocktakeSessionID', @value=NULL");
            }
        }

        public async Task<StocktakeActionResultDto> SaveSessionCountsAsync(
            long stocktakeSessionId,
            SaveStocktakeSessionCountsDto request,
            long countedByUserId)
        {
            var access = await EnsureAccessAsync(stocktakeSessionId, countedByUserId, canManage: false);
            if (!access)
                return Fail("Bạn không có quyền nhập phiếu kiểm kho này.", stocktakeSessionId);

            try
            {
                var updates = request.Lines.Select(line => new StocktakeCountUpdateParam
                {
                    StocktakeItemId = line.StocktakeItemId,
                    CountedQuantity = line.CountedQuantity,
                    Notes = line.Notes
                }).ToList();
                var session = await _stocktakeRepo.SaveSessionCountsAsync(
                    stocktakeSessionId,
                    updates,
                    request.ConfirmedEmptyLocationIds,
                    countedByUserId);
                return Success(session, "Đã lưu số lượng kiểm kho.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> RejectSessionAsync(
            long stocktakeSessionId,
            long rejectedByUserId,
            string? reason)
        {
            try
            {
                var session = await _stocktakeRepo.RejectSessionAsync(
                    stocktakeSessionId,
                    rejectedByUserId,
                    reason ?? string.Empty);
                await _auditLogService.StageAsync(new AuditEventDto
                {
                    UserId = rejectedByUserId,
                    ActionType = "REJECT_STOCKTAKE",
                    EntityName = AuditEntities.StocktakeSession,
                    EntityId = session.StocktakeSessionId.ToString(),
                    NewValues = new { session.Status, Reason = reason?.Trim() }
                });
                await _context.SaveChangesAsync();
                return Success(session, "Đã từ chối kết quả kiểm kho và mở khóa các vị trí.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        private void EnsureStocktakeCapacityDecision(
            IEnumerable<LocationCapacityEvaluationDto> evaluations,
            bool warningAcknowledged,
            string? warningReason)
        {
            var values = evaluations.ToList();
            var exceeded = values
                .Where(value => value.OverallStatus == CapacityEvaluationStatuses.Exceeded)
                .SelectMany(value => value.Scopes
                    .Where(scope => scope.OverallStatus == CapacityEvaluationStatuses.Exceeded)
                    .Select(scope => $"{scope.ScopeType} {scope.ScopeCode}")
                    .DefaultIfEmpty($"BIN {value.LocationCode}"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code)
                .ToList();
            if (exceeded.Count > 0)
                throw new InvalidOperationException(
                    $"Không thể duyệt vì điều chỉnh tăng làm vượt sức chứa tại: {string.Join(", ", exceeded)}.");

            var incomplete = values
                .Where(value => value.OverallStatus is CapacityEvaluationStatuses.Unknown or CapacityEvaluationStatuses.NotConfigured)
                .SelectMany(value => value.Scopes
                    .Where(scope => scope.OverallStatus is CapacityEvaluationStatuses.Unknown or CapacityEvaluationStatuses.NotConfigured)
                    .Select(scope => $"{scope.ScopeType} {scope.ScopeCode}")
                    .DefaultIfEmpty($"BIN {value.LocationCode}"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code)
                .ToList();
            if (_capacityOptions.IsStrict && incomplete.Count > 0)
                throw new InvalidOperationException(
                    $"Chưa đủ cấu hình để kiểm tra sức chứa tại: {string.Join(", ", incomplete)}.");

            if (incomplete.Count > 0 && (!warningAcknowledged || string.IsNullOrWhiteSpace(warningReason)))
                throw new InvalidOperationException(
                    $"Chưa đủ dữ liệu sức chứa tại {string.Join(", ", incomplete)}. Vui lòng xác nhận cảnh báo và nhập lý do để duyệt.");
        }

        private async Task<bool> EnsureAccessAsync(long stocktakeSessionId, long currentUserId, bool canManage)
        {
            if (canManage)
                return true;

            var session = await _stocktakeRepo.GetSessionDetailAsync(stocktakeSessionId);
            return session != null && CanAccess(session, currentUserId, canManage);
        }

        private static bool CanAccess(StocktakeSession session, long currentUserId, bool canManage)
        {
            return canManage || (session.AssignedToUserId.HasValue && session.AssignedToUserId.Value == currentUserId);
        }

        private static StocktakeSessionListDto MapSessionList(StocktakeSession session)
        {
            var (label, css) = GetSessionStatusBadge(session.Status);
            var locations = session.StocktakeLocations.ToList();
            var items = session.StocktakeItems.ToList();

            return new StocktakeSessionListDto
            {
                StocktakeSessionId = session.StocktakeSessionId,
                StocktakeNumber = session.StocktakeNumber,
                WarehouseId = session.WarehouseId,
                WarehouseCode = session.Warehouse?.WarehouseCode ?? string.Empty,
                WarehouseName = session.Warehouse?.WarehouseName ?? string.Empty,
                PlannedDate = session.PlannedDate,
                Status = session.Status,
                StatusLabel = label,
                StatusCss = css,
                CreatedByName = session.CreatedByUser?.FullName ?? string.Empty,
                AssignedToName = session.AssignedToUser?.FullName,
                CreatedAt = session.CreatedAt,
                StartedAt = session.StartedAt,
                SubmittedAt = session.SubmittedAt,
                ApprovedAt = session.ApprovedAt,
                TotalLocations = locations.Count,
                CountedLocations = locations.Count(l => l.CountStatus == LocationCounted),
                PendingLocations = locations.Count(l => l.CountStatus == LocationPending || l.CountStatus == LocationInProgress),
                RecountLocations = 0,
                TotalItems = items.Count,
                CountedItems = items.Count(i => i.CountedQuantity.HasValue),
                VarianceItems = items.Count(i => HasVariance(i)),
                TotalDifferenceQuantity = items.Sum(i => CalculateDifferenceOrZero(i)),
                Notes = session.Notes,
                CanStart = session.Status == SessionScheduled,
                CanCancel = session.Status != SessionCompleted && session.Status != SessionCancelled && session.Status != SessionRejected,
                CanReview = session.Status == SessionPendingApproval,
                CanApprove = CanApproveSession(session)
            };
        }

        private static StocktakeSessionDetailDto MapSessionDetail(StocktakeSession session, bool includeBookQuantities)
        {
            var dto = MapSessionList(session);
            var items = session.StocktakeItems.ToList();

            return new StocktakeSessionDetailDto
            {
                StocktakeSessionId = session.StocktakeSessionId,
                StocktakeNumber = session.StocktakeNumber,
                WarehouseId = session.WarehouseId,
                WarehouseCode = session.Warehouse?.WarehouseCode ?? string.Empty,
                WarehouseName = session.Warehouse?.WarehouseName ?? string.Empty,
                PlannedDate = session.PlannedDate,
                Status = session.Status,
                StatusLabel = dto.StatusLabel,
                StatusCss = dto.StatusCss,
                CreatedByName = session.CreatedByUser?.FullName ?? string.Empty,
                CreatedAt = session.CreatedAt,
                AssignedToName = session.AssignedToUser?.FullName,
                StartedAt = session.StartedAt,
                SubmittedAt = session.SubmittedAt,
                ApprovedByName = session.ApprovedByUser?.FullName,
                ApprovedAt = session.ApprovedAt,
                Notes = session.Notes,
                TotalLocations = dto.TotalLocations,
                CountedLocations = dto.CountedLocations,
                PendingLocations = dto.PendingLocations,
                RecountLocations = dto.RecountLocations,
                TotalItems = dto.TotalItems,
                CountedItems = dto.CountedItems,
                VarianceItems = dto.VarianceItems,
                TotalBookQuantity = includeBookQuantities ? items.Sum(i => i.BookQuantity) : 0,
                TotalCountedQuantity = items.Sum(i => i.CountedQuantity ?? 0),
                TotalDifferenceQuantity = includeBookQuantities ? dto.TotalDifferenceQuantity : 0,
                CanStart = dto.CanStart,
                CanCancel = dto.CanCancel,
                CanReview = dto.CanReview,
                CanApprove = dto.CanApprove,
                Locations = session.StocktakeLocations
                    .OrderBy(l => l.StorageLocation.LocationCode)
                    .Select(l => MapLocation(l, items.Where(i => i.StorageLocationId == l.StorageLocationId).ToList(), session.Status))
                    .ToList(),
                Items = items
                    .OrderBy(i => i.StorageLocation.LocationCode)
                    .ThenBy(i => i.ProductLot.Product.ProductCode)
                    .ThenBy(i => i.ProductLot.LotNumber)
                    .Select(i => MapCountLine(i, includeBookQuantities))
                    .ToList()
            };
        }

        private static StocktakeLocationDto MapLocation(StocktakeLocation location, List<StocktakeItem> items, string sessionStatus)
        {
            var (label, css) = GetLocationStatusBadge(location.CountStatus);
            return new StocktakeLocationDto
            {
                StocktakeSessionId = location.StocktakeSessionId,
                StorageLocationId = location.StorageLocationId,
                LocationCode = location.StorageLocation?.LocationCode ?? string.Empty,
                LocationName = location.StorageLocation?.LocationName ?? location.StorageLocation?.LocationCode ?? string.Empty,
                LocationPath = BuildLocationPath(location.StorageLocation),
                CountStatus = location.CountStatus,
                CountStatusLabel = label,
                CountStatusCss = css,
                CountedByUserId = location.CountedByUserId,
                CountedByName = location.CountedByUser?.FullName,
                CountedAt = location.CountedAt,
                Notes = location.Notes,
                TotalItems = items.Count,
                CountedItems = items.Count(i => i.CountedQuantity.HasValue),
                RecountItems = 0,
                TotalDifferenceQuantity = items.Sum(i => CalculateDifferenceOrZero(i)),
                CanCount = sessionStatus == SessionInProgress &&
                    (location.CountStatus == LocationPending ||
                     location.CountStatus == LocationInProgress ||
                     location.CountStatus == LocationCounted),
                CanSubmit = false
            };
        }

        private static StocktakeCountLineDto MapCountLine(StocktakeItem item, bool includeBookQuantities)
        {
            var product = item.ProductLot?.Product;
            var location = item.StorageLocation;
            var difference = item.CountedQuantity.HasValue ? item.CountedQuantity.Value - item.BookQuantity : (decimal?)null;

            return new StocktakeCountLineDto
            {
                StocktakeItemId = item.StocktakeItemId,
                StocktakeSessionId = item.StocktakeSessionId,
                StorageLocationId = item.StorageLocationId,
                LocationCode = location?.LocationCode ?? string.Empty,
                LocationName = location?.LocationName ?? location?.LocationCode ?? string.Empty,
                LocationPath = BuildLocationPath(location),
                ProductId = item.ProductId,
                ProductCode = product?.ProductCode ?? string.Empty,
                ProductName = product?.ProductName ?? string.Empty,
                UnitOfMeasureId = product?.UnitOfMeasureId ?? 0,
                UnitCode = product?.UnitOfMeasure?.UnitCode ?? string.Empty,
                UnitName = product?.UnitOfMeasure?.UnitName ?? string.Empty,
                QuantityScale = product?.UnitOfMeasure?.QuantityScale ?? 0,
                ProductLotId = item.ProductLotId,
                LotNumber = item.ProductLot?.LotNumber ?? string.Empty,
                ExpiryDate = item.ProductLot?.ExpiryDate,
                BookQuantity = includeBookQuantities ? item.BookQuantity : null,
                CountedQuantity = item.CountedQuantity,
                DifferenceQuantity = includeBookQuantities ? difference : null,
                AdjustmentQuantity = includeBookQuantities ? item.AdjustmentQuantity : null,
                Resolution = includeBookQuantities ? item.Resolution : null,
                Notes = item.Notes,
                IsUnexpected = item.BookQuantity == 0
            };
        }

        private static (string Label, string Css) GetSessionStatusBadge(string status)
        {
            return status switch
            {
                SessionScheduled => ("Đã lên lịch", "bg-secondary"),
                SessionInProgress => ("Đang kiểm", "bg-primary"),
                SessionCounted => ("Đã đếm xong", "bg-info text-dark"),
                SessionPendingApproval => ("Chờ phê duyệt", "bg-warning text-dark"),
                SessionCompleted => ("Hoàn tất", "bg-success"),
                SessionCancelled => ("Đã hủy", "bg-danger"),
                SessionRejected => ("Đã từ chối", "bg-danger"),
                _ => (status, "bg-secondary")
            };
        }

        private static (string Label, string Css) GetLocationStatusBadge(string status)
        {
            return status switch
            {
                LocationPending => ("Chờ nhập", "bg-secondary"),
                LocationInProgress => ("Đang nhập", "bg-primary"),
                LocationCounted => ("Đã nhập đủ", "bg-success"),
                _ => (status, "bg-secondary")
            };
        }

        private static bool HasVariance(StocktakeItem item)
        {
            return item.CountedQuantity.HasValue && item.CountedQuantity.Value - item.BookQuantity != 0;
        }

        private static bool CanApproveSession(StocktakeSession session)
        {
            return session.Status == SessionPendingApproval;
        }

        private static decimal CalculateDifferenceOrZero(StocktakeItem item)
        {
            return item.CountedQuantity.HasValue ? item.CountedQuantity.Value - item.BookQuantity : 0;
        }

        private static string BuildLocationPath(StorageLocation? location)
        {
            if (location == null)
                return string.Empty;

            var zone = location.StorageRack?.WarehouseZone?.ZoneCode;
            var rack = location.StorageRack?.RackCode;
            if (string.IsNullOrWhiteSpace(zone) && string.IsNullOrWhiteSpace(rack))
                return location.LocationCode;

            return $"{zone}/{rack}/{location.LocationCode}";
        }

        private static StocktakeActionResultDto Success(StocktakeSession session, string message)
        {
            return new StocktakeActionResultDto
            {
                Success = true,
                Message = message,
                StocktakeSessionId = session.StocktakeSessionId,
                StocktakeNumber = session.StocktakeNumber,
                Status = session.Status
            };
        }

        private static StocktakeActionResultDto Fail(string message, long? stocktakeSessionId = null)
        {
            return new StocktakeActionResultDto
            {
                Success = false,
                Message = message,
                StocktakeSessionId = stocktakeSessionId
            };
        }
    }
}
