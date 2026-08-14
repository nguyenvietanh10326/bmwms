using BMWMS.Business.DTOs.Stocktake;
using BMWMS.Business.Interfaces.Stocktake;
using BMWMS.Repository.Interfaces.Stocktake;
using BMWMS.Repository.Models;

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

        private const string LocationPending = "PENDING";
        private const string LocationInProgress = "IN_PROGRESS";
        private const string LocationCounted = "COUNTED";
        private const string LocationRecountRequired = "RECOUNT_REQUIRED";

        private const string ResolutionAcceptDifference = "ACCEPT_DIFFERENCE";
        private const string ResolutionNoAdjustment = "NO_ADJUSTMENT";
        private const string ResolutionRecount = "RECOUNT";

        private readonly IStocktakeRepository _stocktakeRepo;

        public StocktakeService(IStocktakeRepository stocktakeRepo)
        {
            _stocktakeRepo = stocktakeRepo;
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
                RoleCode = u.Role?.RoleCode ?? string.Empty
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

            return MapSessionDetail(session, includeBookQuantities: canManage);
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
                return Fail("WarehouseId khong hop le.");
            if (dto.PlannedDate == default)
                return Fail("PlannedDate khong hop le.");

            try
            {
                var session = await _stocktakeRepo.CreateSessionAsync(
                    dto.WarehouseId,
                    dto.PlannedDate,
                    createdByUserId,
                    dto.AssignedToUserId,
                    dto.Notes,
                    dto.StorageLocationIds ?? new List<long>());

                return Success(session, $"Da tao dot kiem kho {session.StocktakeNumber}.");
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
                return Success(session, $"Da bat dau dot kiem kho {session.StocktakeNumber} va tao snapshot ton kho.");
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
                return Success(session, $"Da huy dot kiem kho {session.StocktakeNumber}.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> SaveCountsAsync(long stocktakeSessionId, long storageLocationId, List<StocktakeCountLineDto> lines, long countedByUserId, bool canManage, string? notes = null)
        {
            var access = await EnsureAccessAsync(stocktakeSessionId, countedByUserId, canManage);
            if (!access)
                return Fail("Khong co quyen nhap so dem cho dot kiem kho nay.", stocktakeSessionId);

            try
            {
                var updates = lines.Select(l => new StocktakeCountUpdateParam
                {
                    StocktakeItemId = l.StocktakeItemId,
                    CountedQuantity = l.CountedQuantity,
                    Notes = l.Notes
                }).ToList();

                var session = await _stocktakeRepo.SaveCountsAsync(stocktakeSessionId, storageLocationId, updates, countedByUserId, notes);
                return Success(session, "Da luu so dem.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> SubmitLocationAsync(long stocktakeSessionId, long storageLocationId, long submittedByUserId, bool canManage, string? notes)
        {
            var access = await EnsureAccessAsync(stocktakeSessionId, submittedByUserId, canManage);
            if (!access)
                return Fail("Khong co quyen submit bin nay.", stocktakeSessionId);

            try
            {
                var session = await _stocktakeRepo.SubmitLocationAsync(stocktakeSessionId, storageLocationId, submittedByUserId, notes);
                return Success(session, "Da submit bin kiem dem.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> AddUnexpectedItemAsync(long stocktakeSessionId, UnexpectedStocktakeItemDto dto, long countedByUserId, bool canManage)
        {
            var access = await EnsureAccessAsync(stocktakeSessionId, countedByUserId, canManage);
            if (!access)
                return Fail("Khong co quyen them hang phat sinh cho dot kiem kho nay.", stocktakeSessionId);

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
                    Message = "Da them hang phat sinh vao dot kiem kho.",
                    StocktakeSessionId = item.StocktakeSessionId,
                    Status = SessionInProgress
                };
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> ApplyResolutionsAsync(long stocktakeSessionId, List<StocktakeResolutionDto> resolutions, long reviewedByUserId)
        {
            try
            {
                var repoParams = (resolutions ?? new List<StocktakeResolutionDto>())
                    .Select(r => new StocktakeResolutionParam
                    {
                        StocktakeItemId = r.StocktakeItemId,
                        Resolution = r.Resolution,
                        Notes = r.Notes
                    })
                    .ToList();

                var session = await _stocktakeRepo.ApplyResolutionsAsync(stocktakeSessionId, repoParams, reviewedByUserId);
                var message = session.Status == SessionInProgress
                    ? "Da mo recount cho bin lien quan."
                    : session.Status == SessionPendingApproval
                        ? "Da ghi nhan xu ly chenhlech, san sang phe duyet."
                        : "Da luu xu ly chenhlech.";
                return Success(session, message);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
        }

        public async Task<StocktakeActionResultDto> ApproveSessionAsync(long stocktakeSessionId, long approvedByUserId, string? notes)
        {
            try
            {
                var session = await _stocktakeRepo.ApproveSessionAsync(stocktakeSessionId, approvedByUserId, notes);
                return Success(session, $"Da phe duyet va hoan tat dot kiem kho {session.StocktakeNumber}.");
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, stocktakeSessionId);
            }
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
                RecountLocations = locations.Count(l => l.CountStatus == LocationRecountRequired),
                TotalItems = items.Count,
                CountedItems = items.Count(i => i.CountedQuantity.HasValue),
                VarianceItems = items.Count(i => HasVariance(i)),
                TotalDifferenceQuantity = items.Sum(i => CalculateDifferenceOrZero(i)),
                Notes = session.Notes,
                CanStart = session.Status == SessionScheduled,
                CanCancel = session.Status != SessionCompleted && session.Status != SessionCancelled,
                CanReview = session.Status == SessionCounted || session.Status == SessionPendingApproval,
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
                RecountItems = items.Count(i => i.Resolution == ResolutionRecount),
                TotalDifferenceQuantity = items.Sum(i => CalculateDifferenceOrZero(i)),
                CanCount = sessionStatus == SessionInProgress &&
                    (location.CountStatus == LocationPending ||
                     location.CountStatus == LocationInProgress ||
                     location.CountStatus == LocationRecountRequired),
                CanSubmit = sessionStatus == SessionInProgress &&
                    location.CountStatus != LocationCounted
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
                SessionScheduled => ("Da len lich", "bg-secondary"),
                SessionInProgress => ("Dang kiem", "bg-primary"),
                SessionCounted => ("Da dem xong", "bg-info text-dark"),
                SessionPendingApproval => ("Cho phe duyet", "bg-warning text-dark"),
                SessionCompleted => ("Hoan tat", "bg-success"),
                SessionCancelled => ("Da huy", "bg-danger"),
                _ => (status, "bg-secondary")
            };
        }

        private static (string Label, string Css) GetLocationStatusBadge(string status)
        {
            return status switch
            {
                LocationPending => ("Cho dem", "bg-secondary"),
                LocationInProgress => ("Dang dem", "bg-primary"),
                LocationCounted => ("Da submit", "bg-success"),
                LocationRecountRequired => ("Can dem lai", "bg-warning text-dark"),
                _ => (status, "bg-secondary")
            };
        }

        private static bool HasVariance(StocktakeItem item)
        {
            return item.CountedQuantity.HasValue && item.CountedQuantity.Value - item.BookQuantity != 0;
        }

        private static bool CanApproveSession(StocktakeSession session)
        {
            if (session.Status == SessionPendingApproval)
                return true;

            return session.Status == SessionCounted &&
                session.StocktakeItems.All(i => !HasVariance(i));
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
