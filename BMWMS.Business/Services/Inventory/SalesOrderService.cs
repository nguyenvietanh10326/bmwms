using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using BMWMS.Repository.Repositories.Inventory;
using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Services.Inventory
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IInventoryRepository _invenRepository;
        private readonly BmwmsContext _context;
        private readonly IAuditLogService? _auditLogService;

        public SalesOrderService(
            ISalesOrderRepository salesOrderRepository,
            IInventoryRepository invenRepository,
            BmwmsContext context, IAuditLogService? auditLogService = null)
        {
            _salesOrderRepository = salesOrderRepository;
            _invenRepository = invenRepository;
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<SalesOrderDetailApiResponse?>
            GetSalesOrderDetailForOutboundAsync(
                long salesOrderId)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(salesOrderId);
            if (salesOrder == null || salesOrder.Status is not
                ("CONFIRMED" or "APPROVED" or "ALLOCATED" or "PARTIALLY_FULFILLED"))
            {
                return null;
            }

            var items = new List<SalesOrderItemDto>();
            if (await _context.OutboundOrders.AnyAsync(o => o.SalesOrderId == salesOrderId && o.Status == "PENDING_APPROVAL")) return null;

            foreach (var detail in salesOrder.SalesOrderDetails.Where(d => d.IsActive))
            {
                var plannedButNotIssued = await _context.OutboundOrderItems
                    .Where(item => item.OutboundOrder.SalesOrderId == salesOrderId &&
                                   (item.OutboundOrder.Status == "DRAFT" ||
                                    item.OutboundOrder.Status == "ASSIGNED" ||
                                    item.OutboundOrder.Status == "IN_PROGRESS") &&
                                   item.ProductId == detail.ProductId)
                    .SumAsync(item => item.RequestedQuantity - item.IssuedQuantity);
                var availableForNewOutbound = Math.Max(
                    0,
                    detail.OrderedQuantity - detail.FulfilledQuantity - plannedButNotIssued);
                var activeReserved = await _context.InventoryReservations
                    .Where(reservation => reservation.SalesOrderDetailId == detail.SalesOrderDetailId &&
                                          (reservation.Status == "ACTIVE" || reservation.Status == "PARTIALLY_CONSUMED"))
                    .SumAsync(reservation => reservation.ReservedQuantity - reservation.ConsumedQuantity);
                availableForNewOutbound = Math.Max(0, Math.Min(
                    availableForNewOutbound,
                    activeReserved - plannedButNotIssued));
                if (availableForNewOutbound <= 0)
                    continue;

                // Lấy danh sách Lot + Bin đã reserve
                var lotBinList =
                    await _salesOrderRepository
                        .GetReservedLotBinInfoAsync(
                            detail.SalesOrderDetailId);

                var lotBinInfo = lotBinList.Any()
                    ? string.Join(", ", lotBinList)
                    : "N/A";

                items.Add(new SalesOrderItemDto
                {
                    ProductId = detail.ProductId,

                    ProductCode =
                        detail.Product?.ProductCode
                        ?? string.Empty,

                    ProductName =
                        detail.Product?.ProductName
                        ?? string.Empty,

                    // Số lượng khách đặt
                    Quantity = detail.OrderedQuantity,

                    // Số lượng còn có thể lập đợt xuất. Việc giữ tồn phải hoàn tất
                    // ở bước Quản lý kho duyệt SO; Outbound không tự giữ bù.
                    ReservedQuantity = availableForNewOutbound,

                    // Đơn vị tính
                    UnitName =
                        detail.Product?.UnitOfMeasure?.UnitName
                        ?? "Đơn vị",
                    QuantityScale = detail.Product?.UnitOfMeasure?.QuantityScale ?? 0,
                    TrackLot = detail.Product?.TrackLot ?? false,

                    // LOT · BIN
                    LotBinInfo = lotBinInfo
                });
            }

            return new SalesOrderDetailApiResponse
            {
                SalesOrderId =
                    salesOrder.SalesOrderId,

                SalesOrderNumber =
                    salesOrder.SalesOrderNumber,

                CustomerName =
                    salesOrder.Customer?.CustomerName
                    ?? "N/A",

                WarehouseId = null,
                WarehouseName = null,

                Items = items
            };
        }
        public async Task<List<UserSelectDto>> GetSalesOrderCreatorsAsync(
       CancellationToken cancellationToken = default)
        {
            var users = await _salesOrderRepository
                .GetSalesOrderCreatorsAsync(cancellationToken);

            return users.Select(x => new UserSelectDto
            {
                UserId = x.UserId,
                FullName = x.FullName
            }).ToList();
        }
        public async Task<List<SalesOrderApiResponse>> GetConfirmedSalesOrdersAsync()
        {
            var salesOrders = await _salesOrderRepository.GetConfirmedSalesOrdersAsync();
            var result = new List<SalesOrderApiResponse>();
            foreach (var so in salesOrders)
            {
                var detail = await GetSalesOrderDetailForOutboundAsync(so.SalesOrderId);
                if (detail?.Items.Count > 0)
                    result.Add(new SalesOrderApiResponse
                    {
                        SalesOrderId = so.SalesOrderId,
                        SalesOrderNumber = so.SalesOrderNumber
                    });
            }
            return result;
        }
        public async Task<PagedResult<SalesOrderListDto>> GetPagedAsync(SalesOrderSearchCriteria criteria)
        {
            criteria.PageIndex = Math.Max(1, criteria.PageIndex);
            criteria.PageSize = Math.Clamp(criteria.PageSize, 10, 100);

            var (entities, totalCount) = await _salesOrderRepository.GetPagedAsync(
                criteria.Keyword,
                criteria.Status,
                criteria.FromDate,
                criteria.ToDate,
                criteria.PageIndex,
                criteria.PageSize, criteria.SortOrder
            );

            var pageIds = entities.Select(x => x.SalesOrderId).ToList();
            var childIds = (await _context.OutboundOrders.Where(o => o.SalesOrderId.HasValue && pageIds.Contains(o.SalesOrderId.Value))
                .Select(o => o.SalesOrderId!.Value).ToListAsync()).Concat(await _context.InboundOrders
                .Where(o => o.SalesOrderId.HasValue && pageIds.Contains(o.SalesOrderId.Value)).Select(o => o.SalesOrderId!.Value).ToListAsync()).ToHashSet();
            var pendingReviews = await _context.OutboundOrders.AsNoTracking()
                .Where(o => o.SourceType == "SALES_ORDER" && o.SalesOrderId.HasValue &&
                    pageIds.Contains(o.SalesOrderId.Value) && o.Status == "PENDING_APPROVAL")
                .Select(o => new { SalesOrderId = o.SalesOrderId!.Value, o.OutboundOrderId }).ToListAsync();
            var reviewIds = pendingReviews.GroupBy(o => o.SalesOrderId)
                .ToDictionary(g => g.Key, g => g.Min(o => o.OutboundOrderId));
            // Mapping từ Entity sang DTO
            var list = entities.Select(x => new SalesOrderListDto
            {
                CanExternalCancel = (x.Status is "CONFIRMED" or "ALLOCATED") && !childIds.Contains(x.SalesOrderId),
                SalesOrderId = x.SalesOrderId,
                SalesOrderNumber = x.SalesOrderNumber,
                CustomerCode = x.Customer?.CustomerCode ?? string.Empty,
                CustomerName = x.Customer?.CustomerName ?? string.Empty,
                OrderDate = x.OrderDate,
                ExpectedIssueDate = x.ExpectedIssueDate,
                Status = NormalizeSalesOrderStatus(x.Status),
                ItemCount = x.SalesOrderDetails.Count,
                PendingOutboundReviewId = reviewIds.TryGetValue(x.SalesOrderId, out var reviewId) ? reviewId : null,
                CreatedByName = x.CreatedByUser?.FullName ?? string.Empty,
                CreatedAt = x.CreatedAt
            }).ToList();

            return new PagedResult<SalesOrderListDto>
            {
                Items = list,
                TotalCount = totalCount,
                PageIndex = criteria.PageIndex,
                PageSize = criteria.PageSize
            };
        }

        private static string NormalizeSalesOrderStatus(string status)
        {
            return status switch
            {
                "ALLOCATED" => "CONFIRMED",
                "PARTIALLY_FULFILLED" => "PARTIALLY_ISSUED",
                "FULFILLED" or "CLOSED" => "ISSUED",
                _ => status
            };
        }

        public async Task<SalesOrderDetailDto?> GetByIdAsync(long salesOrderId)
        {
            var entity = await _salesOrderRepository.GetByIdAsync(salesOrderId);
            if (entity == null) return null;
            var productIds = entity.SalesOrderDetails.Where(d => d.IsActive).Select(d => d.ProductId).ToList();
            var available = await _context.Inventories.AsNoTracking().Where(i => productIds.Contains(i.ProductId))
                .GroupBy(i => i.ProductId).Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)) })
                .ToDictionaryAsync(i => i.ProductId, i => i.Quantity);
            var hasChild = await _context.OutboundOrders.AnyAsync(o => o.SalesOrderId == salesOrderId) ||
                await _context.InboundOrders.AnyAsync(o => o.SalesOrderId == salesOrderId);

            return new SalesOrderDetailDto
            {
                CreatedByUserId = entity.CreatedByUserId,
                RowVersion = Convert.ToBase64String(entity.RowVersion),
                CanExternalCancel = (entity.Status is "CONFIRMED" or "ALLOCATED") && !hasChild && entity.SalesOrderDetails.All(d => d.FulfilledQuantity == 0),
                CanEdit = entity.Status == "DRAFT" &&
                    !hasChild,
                SalesOrderId = entity.SalesOrderId,
                SalesOrderNumber = entity.SalesOrderNumber,
                CustomerId = entity.CustomerId,
                CustomerCode = entity.Customer?.CustomerCode ?? string.Empty,
                CustomerName = entity.Customer?.CustomerName ?? string.Empty,
                OrderDate = entity.OrderDate,
                ExpectedIssueDate = entity.ExpectedIssueDate,
                Status = entity.Status,
                Notes = entity.Notes,
                CreatedByName = entity.CreatedByUser?.FullName ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                ConfirmedByName = entity.ConfirmedByUser?.FullName,
                ConfirmedAt = entity.ConfirmedAt,
                OutboundBatches = entity.OutboundOrders.Where(o => o.SourceType == "SALES_ORDER")
                    .OrderBy(o => o.CreatedAt).ThenBy(o => o.OutboundOrderId)
                    .Select(o => new SalesOrderOutboundBatchDto
                    {
                        OutboundOrderId = o.OutboundOrderId, OutboundOrderNumber = o.OutboundOrderNumber,
                        Status = o.Status, CreatedAt = o.CreatedAt,
                        AssignedToUserName = o.AssignedToUser?.FullName ?? o.AssignedToUser?.Username,
                        ReviewedByName = o.Status == "COMPLETED" ? o.ConfirmedByUser?.FullName ?? o.ConfirmedByUser?.Username : null,
                        ReviewedAt = o.Status == "COMPLETED" ? o.ConfirmedAt : null
                    }).ToList(),
                Items = entity.SalesOrderDetails.Where(d => d.IsActive).Select(d => new SalesOrderItemDtos
                {
                    SalesOrderDetailId = d.SalesOrderDetailId,
                    ProductId = d.ProductId,
                    ProductCode = d.Product?.ProductCode ?? string.Empty,
                    ProductName = d.Product?.ProductName ?? string.Empty,
                    UnitName = d.Product?.UnitOfMeasure?.UnitName ?? string.Empty,
                    QuantityScale = d.Product?.UnitOfMeasure?.QuantityScale ?? 0,
                    TrackLot = d.Product?.TrackLot ?? false,
                    OrderedQuantity = d.OrderedQuantity,
                    ReservedQuantity = d.ReservedQuantity,
                    FulfilledQuantity = d.FulfilledQuantity,
                    AvailableQuantity = available.GetValueOrDefault(d.ProductId),
                    Notes = d.Notes
                }).ToList()
            };
        }

        public async Task<List<SalesOrderProductLookupDto>> GetActiveProductLookupsAsync()
        {
            var products = await _salesOrderRepository.GetActiveProductsForLookupAsync();

            return products.Select(p => new SalesOrderProductLookupDto
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                ProductGroupId = p.ProductGroupId,
                ProductGroupName = p.ProductGroup?.GroupName ?? string.Empty,
                Barcode = p.Barcode,
                UnitOfMeasure = p.UnitOfMeasure?.UnitName ?? string.Empty,
                QuantityScale = p.UnitOfMeasure?.QuantityScale ?? 0,
                TrackLot = p.TrackLot,
                TrackExpiry = p.TrackExpiry,
                RotationMethod = p.RotationMethod,
                OnHandQuantity = p.Inventories.Sum(i => i.OnHandQuantity),
                ReservedQuantity = p.Inventories.Sum(i => i.ReservedQuantity),
                AvailableQuantity = p.Inventories.Sum(i => i.OnHandQuantity - i.ReservedQuantity)
            }).ToList();
        }

        public async Task<SalesOrderDetailDto> CreateDraftAsync(CreateUpdateSalesOrderDto dto)
        {
            var creator = await _context.Users.Include(u => u.Role).AsNoTracking().FirstOrDefaultAsync(u => u.UserId == dto.CurrentUserId && u.Status == "ACTIVE");
            if (creator?.Role?.RoleCode is not ("SALES_STAFF" or "SYSTEM_ADMIN"))
                throw new UnauthorizedAccessException("Chỉ Sales Staff được tạo đơn bán hàng.");
            if (dto.CustomerId <= 0)
                throw new ArgumentException("Vui lòng chọn khách hàng.");

            if (dto.OrderDate == default)
                throw new ArgumentException("Ngày đặt hàng không hợp lệ.");

            if (dto.ExpectedIssueDate.HasValue && dto.ExpectedIssueDate.Value < dto.OrderDate)
                throw new ArgumentException("Ngày xuất dự kiến không được trước ngày đặt hàng.");

            if (dto.Items == null || dto.Items.Count == 0)
                throw new ArgumentException("Đơn bán hàng phải có ít nhất một sản phẩm.");

            if (dto.Items.Any(i => i.ProductId <= 0 || i.OrderedQuantity <= 0))
                throw new ArgumentException("Mỗi sản phẩm phải có số lượng đặt lớn hơn 0.");

            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            if (productIds.Distinct().Count() != productIds.Count)
                throw new ArgumentException("Mỗi sản phẩm chỉ được xuất hiện một lần trong đơn bán hàng.");

            var customer = await _salesOrderRepository.GetActiveCustomerAsync(dto.CustomerId);
            if (customer == null)
                throw new ArgumentException("Khách hàng không tồn tại hoặc đã ngừng hoạt động.");

            var activeProducts = await _salesOrderRepository.GetActiveProductsAsync(productIds);
            if (activeProducts.Count != productIds.Count)
                throw new ArgumentException("Một hoặc nhiều sản phẩm không tồn tại hoặc đã ngừng hoạt động.");

            var productsById = activeProducts.ToDictionary(p => p.ProductId);
            foreach (var item in dto.Items)
                QuantityRules.EnsureValid(productsById[item.ProductId], item.OrderedQuantity, "Số lượng đặt");

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await OrderWorkflowLock.AcquireAsync(_context, "SO_NUMBER", DateTime.Today.Year);
                string newSoNumber = await _salesOrderRepository.GenerateSalesOrderNumberAsync();

                var order = new SalesOrder
                {
                    SalesOrderNumber = newSoNumber,
                    CustomerId = dto.CustomerId,
                    OrderDate = dto.OrderDate,
                    ExpectedIssueDate = dto.ExpectedIssueDate,
                    Status = "DRAFT",
                    Notes = dto.Notes,
                    AllocationStrategy = dto.AllocationStrategy,
                    CreatedByUserId = dto.CurrentUserId,
                    CreatedAt = DateTime.UtcNow,
                    SalesOrderDetails = dto.Items.Select(i => new SalesOrderDetail
                    {
                        ProductId = i.ProductId,
                        OrderedQuantity = i.OrderedQuantity,
                        ReservedQuantity = 0,
                        FulfilledQuantity = 0,
                        Notes = i.Notes
                    }).ToList()
                };

                var created = await _salesOrderRepository.CreateAsync(order);
                foreach (var detail in created.SalesOrderDetails)
                {
                    var product = productsById[detail.ProductId];
                    var reserved = await _invenRepository.ReserveStockForOrderAsync(
                        detail.ProductId, detail.OrderedQuantity, detail.SalesOrderDetailId,
                        dto.CurrentUserId, product.RotationMethod);
                    if (!reserved)
                        throw new InvalidOperationException($"Tồn khả dụng của {product.ProductCode} không đủ để giữ cho đơn nháp.");
                    detail.ReservedQuantity = detail.OrderedQuantity;
                }

                if (_auditLogService != null) await _auditLogService.StageAsync(new AuditEventDto { UserId = dto.CurrentUserId,
                    ActionType = "CREATE_SALES_ORDER", EntityName = AuditEntities.SalesOrder, EntityId = created.SalesOrderId.ToString(),
                    NewValues = new { created.SalesOrderNumber, created.Status, created.CustomerId, dto.Items } });
                await _context.SaveChangesAsync();
                var response = await GetByIdAsync(created.SalesOrderId)
                    ?? throw new InvalidOperationException("Không đọc được SO vừa tạo; thao tác chưa được ghi nhận.");
                await transaction.CommitAsync();
                return response;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateDraftAsync(CreateUpdateSalesOrderDto dto)
        {
            var actor = await _context.Users.Include(u => u.Role).AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == dto.CurrentUserId && u.Status == "ACTIVE");
            if (actor?.Role?.RoleCode is not ("SALES_STAFF" or "SYSTEM_ADMIN"))
                throw new InvalidOperationException("Chỉ nhân viên bán hàng được sửa SO nháp.");
            if (await _salesOrderRepository.GetActiveCustomerAsync(dto.CustomerId) == null)
                throw new ArgumentException("Khách hàng không còn hoạt động.");
            if (dto.ExpectedIssueDate < dto.OrderDate)
                throw new ArgumentException("Ngày xuất dự kiến không được trước ngày đặt hàng.");
            if (!dto.SalesOrderId.HasValue || dto.Items == null || dto.Items.Count == 0) return false;
            if (dto.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
                throw new ArgumentException("Mỗi sản phẩm chỉ được xuất hiện một lần trong đơn bán hàng.");

            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var activeProducts = await _salesOrderRepository.GetActiveProductsAsync(productIds);
            if (activeProducts.Count != productIds.Distinct().Count())
                throw new ArgumentException("Một hoặc nhiều sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
            var productsById = activeProducts.ToDictionary(p => p.ProductId);
            foreach (var item in dto.Items)
                QuantityRules.EnsureValid(productsById[item.ProductId], item.OrderedQuantity, "Số lượng đặt");

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await BMWMS.Business.Common.OrderWorkflowLock.AcquireAsync(_context, "SO", dto.SalesOrderId.Value);
                var existing = await _context.SalesOrders.IgnoreQueryFilters()
                    .Include(s => s.SalesOrderDetails)
                    .FirstOrDefaultAsync(s => s.SalesOrderId == dto.SalesOrderId.Value);
                if (existing == null || existing.Status != "DRAFT") return false;
                if (actor.Role.RoleCode == "SALES_STAFF" && existing.CreatedByUserId != dto.CurrentUserId)
                    throw new InvalidOperationException("Bạn chỉ được sửa đơn bán hàng mình phụ trách.");
                if (await _context.OutboundOrders.AnyAsync(o => o.SalesOrderId == existing.SalesOrderId) ||
                    await _context.InboundOrders.AnyAsync(o => o.SalesOrderId == existing.SalesOrderId))
                    throw new InvalidOperationException("Đơn đã có phiếu nhập/xuất; không thể chỉnh sửa.");
                if (dto.RowVersion != Convert.ToBase64String(existing.RowVersion))
                    throw new InvalidOperationException("SO đã thay đổi hoặc thiếu phiên bản. Vui lòng tải lại đơn trước khi sửa.");
                var oldValues = new { existing.Status, existing.CustomerId, existing.RevisionNo,
                    Items = existing.SalesOrderDetails.Where(d => d.IsActive).Select(d => new { d.ProductId, d.OrderedQuantity }).ToList() };

                await ReleaseReservationsAsync(existing.SalesOrderId, dto.CurrentUserId);

                existing.CustomerId = dto.CustomerId;
                existing.OrderDate = dto.OrderDate;
                existing.ExpectedIssueDate = dto.ExpectedIssueDate;
                existing.Notes = dto.Notes;
                existing.AllocationStrategy = dto.AllocationStrategy ?? "FIFO";
                existing.UpdatedAt = DateTime.UtcNow;
                existing.Status = "DRAFT";
                existing.ConfirmedAt = null;
                existing.ConfirmedByUserId = null;
                existing.RevisionNo++;

                foreach (var detail in existing.SalesOrderDetails)
                {
                    var requested = dto.Items.SingleOrDefault(i => i.ProductId == detail.ProductId);
                    detail.IsActive = requested != null;
                    detail.ReservedQuantity = 0;
                    if (requested != null)
                    {
                        detail.OrderedQuantity = requested.OrderedQuantity;
                        detail.Notes = requested.Notes;
                    }
                }
                foreach (var requested in dto.Items.Where(i => !existing.SalesOrderDetails.Any(d => d.ProductId == i.ProductId)))
                    existing.SalesOrderDetails.Add(new SalesOrderDetail { ProductId = requested.ProductId,
                        OrderedQuantity = requested.OrderedQuantity, Notes = requested.Notes, IsActive = true });
                await _context.SaveChangesAsync();

                foreach (var detail in existing.SalesOrderDetails.Where(d => d.IsActive))
                {
                    var product = productsById[detail.ProductId];
                    if (!await _invenRepository.ReserveStockForOrderAsync(
                            detail.ProductId, detail.OrderedQuantity, detail.SalesOrderDetailId,
                            dto.CurrentUserId, product.RotationMethod))
                        throw new InvalidOperationException($"Tồn khả dụng của {product.ProductCode} không đủ để cập nhật đơn nháp.");
                    detail.ReservedQuantity = detail.OrderedQuantity;
                }

                if (_auditLogService != null) await _auditLogService.StageAsync(new AuditEventDto { UserId = dto.CurrentUserId,
                    ActionType = "UPDATE_SALES_ORDER", EntityName = AuditEntities.SalesOrder,
                    EntityId = existing.SalesOrderId.ToString(), OldValues = oldValues,
                    NewValues = new { existing.Status, existing.RevisionNo, dto.Items } });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool IsSuccess, string Message)> ConfirmAndReserveStockAsync(
          long salesOrderId,
          long confirmedByUserId, string? rowVersion = null)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await BMWMS.Business.Common.OrderWorkflowLock.AcquireAsync(_context, "SO", salesOrderId);
            var approver = await _context.Users.Include(user => user.Role)
                .FirstOrDefaultAsync(user => user.UserId == confirmedByUserId && user.Status == "ACTIVE");
            if (approver?.Role?.RoleCode is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN"))
                return (false, "Chỉ Quản lý kho được duyệt SO và giữ tồn.");
            var order = await _salesOrderRepository.GetByIdAsync(salesOrderId);

            if (order == null)
                return (false, "Không tìm thấy đơn bán hàng!");

            if (order.Status != "DRAFT")
                return (false, "Đơn hàng phải ở trạng thái Nháp mới có thể xác nhận!");
            if (rowVersion != null && rowVersion != Convert.ToBase64String(order.RowVersion))
                return (false, "Nội dung SO đã thay đổi. Tải lại và xem xét phiên bản mới trước khi duyệt.");

            foreach (var detail in order.SalesOrderDetails.Where(d => d.IsActive))
            {
                var activeReserved = await _context.InventoryReservations
                    .Where(r => r.SalesOrderDetailId == detail.SalesOrderDetailId &&
                                (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"))
                    .SumAsync(r => (decimal?)(r.ReservedQuantity - r.ConsumedQuantity)) ?? 0;
                var need = detail.OrderedQuantity - activeReserved;
                if (need > 0 && !await _invenRepository.ReserveStockForOrderAsync(
                        detail.ProductId, need, detail.SalesOrderDetailId, confirmedByUserId,
                        detail.Product?.RotationMethod ?? "FIFO"))
                {
                    await transaction.RollbackAsync();
                    return (false, $"Tồn khả dụng của {detail.Product?.ProductCode ?? detail.ProductId.ToString()} không đủ để hoàn tất giữ tồn.");
                }
                detail.ReservedQuantity = detail.OrderedQuantity;
            }

            order.Status = "CONFIRMED";
            order.ConfirmedByUserId = confirmedByUserId;
            order.ConfirmedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            if (_auditLogService != null) await _auditLogService.StageAsync(new AuditEventDto {
                UserId = confirmedByUserId, ActionType = "APPROVE_SALES_ORDER",
                EntityName = AuditEntities.SalesOrder, EntityId = order.SalesOrderId.ToString(),
                OldValues = new { Status = "DRAFT" }, NewValues = new { order.Status, order.RevisionNo } });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (
                true,
                "Đã xác nhận đơn bán hàng; phần giữ tồn từ bản nháp tiếp tục có hiệu lực."
            );
        }


        public async Task<(bool IsSuccess, string Message)> CancelOrderAsync(long salesOrderId, long userId, string reason)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await OrderWorkflowLock.AcquireAsync(_context, "SO", salesOrderId);
            var order = await _salesOrderRepository.GetByIdAsync(salesOrderId);
            if (order == null) return (false, "Không tìm thấy đơn bán hàng!");

            if (order.Status is not ("DRAFT" or "CONFIRMED" or "ALLOCATED"))
                return (false, "Chỉ có thể hủy đơn bán hàng ở trạng thái Nháp hoặc Đã xác nhận!");

            var actor = await _context.Users.Include(user => user.Role)
                .FirstOrDefaultAsync(user => user.UserId == userId && user.Status == "ACTIVE");
            var permittedRoles = new[] { "SALES_STAFF", "WAREHOUSE_MANAGER", "SYSTEM_ADMIN" };
            if (actor?.Role == null || !permittedRoles.Contains(actor.Role.RoleCode))
                return (false, "Bạn không có quyền hủy đơn bán hàng.");
            if (actor.Role.RoleCode == "SALES_STAFF" && order.CreatedByUserId != userId)
                return (false, "Bạn chỉ được hủy SO mình phụ trách.");
            if (actor.Role.RoleCode == "SALES_STAFF" && order.Status != "DRAFT")
                return (false, "SO đã được Manager duyệt; nhân viên bán hàng không được hủy.");
            if (actor.Role.RoleCode == "WAREHOUSE_MANAGER" && order.Status == "DRAFT")
                return (false, "SO đang nháp; hãy dùng chức năng từ chối đơn hàng.");
            reason = reason?.Trim() ?? string.Empty;
            if (reason.Length is < 5 or > 500)
                return (false, "Lý do hủy phải từ 5 đến 500 ký tự.");
            if (order.SalesOrderDetails.Any(detail => detail.FulfilledQuantity > 0) ||
                await _context.OutboundOrders.AnyAsync(outbound => outbound.SalesOrderId == salesOrderId) ||
                await _context.InboundOrders.AnyAsync(inbound => inbound.SalesOrderId == salesOrderId))
                return (false, "SO đã có phiếu nhập/xuất; không được hủy.");

            await ReleaseReservationsAsync(salesOrderId, userId);
            var oldStatus = order.Status;
            order.Status = "CANCELLED";
            order.Notes = OrderWorkflowNotes.AppendIfFits(order.Notes, $"Lý do hủy: {reason.Trim()}");
            order.UpdatedAt = DateTime.UtcNow;
            if (_auditLogService != null) await _auditLogService.StageAsync(new AuditEventDto {
                UserId = userId, ActionType = "CANCEL_SALES_ORDER", EntityName = AuditEntities.SalesOrder,
                EntityId = order.SalesOrderId.ToString(), OldValues = new { Status = oldStatus },
                NewValues = new { order.Status, Reason = reason.Trim() } });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, "Hủy đơn bán hàng và giải phóng giữ tồn thành công!");
        }

        private async Task ReleaseReservationsAsync(long salesOrderId, long userId)
        {
            var reservations = await _context.InventoryReservations
                .Include(r => r.SalesOrderDetail)
                .Where(r => r.SalesOrderDetail != null && r.SalesOrderDetail.SalesOrderId == salesOrderId &&
                            (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"))
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                var releasable = reservation.ReservedQuantity - reservation.ConsumedQuantity;
                if (releasable > 0)
                {
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "RELEASE_RESERVATION",
                        ProductId = reservation.ProductId,
                        StorageLocationId = reservation.StorageLocationId,
                        ProductLotId = reservation.ProductLotId,
                        OnHandDelta = 0,
                        ReservedDelta = -releasable,
                        InventoryReservationId = reservation.InventoryReservationId,
                        PerformedByUserId = userId,
                        TransactionAt = DateTime.UtcNow,
                        Notes = "Giải phóng giữ tồn của Sales Order nháp."
                    });
                }
                reservation.Status = "RELEASED";
                reservation.ReleasedAt = DateTime.UtcNow;
                reservation.SalesOrderDetail!.ReservedQuantity = 0;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<(bool IsSuccess, string Message)> RejectDraftAsync(long salesOrderId, long userId, string reason, string? rowVersion = null)
        {
            reason = reason?.Trim() ?? "";
            if (reason.Length is < 10 or > 500) return (false, "Lý do từ chối phải từ 10 đến 500 ký tự.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await BMWMS.Business.Common.OrderWorkflowLock.AcquireAsync(_context, "SO", salesOrderId);
            var actor = await _context.Users.Include(u => u.Role).AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Status == "ACTIVE");
            if (actor?.Role?.RoleCode is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN"))
                return (false, "Chỉ Quản lý kho được từ chối SO.");
            var order = await _salesOrderRepository.GetByIdAsync(salesOrderId);
            if (order?.Status != "DRAFT") return (false, "Chỉ được từ chối SO đang nháp.");
            if (rowVersion != null && rowVersion != Convert.ToBase64String(order.RowVersion))
                return (false, "SO đã thay đổi. Tải lại và xem xét phiên bản mới trước khi từ chối.");
            if (await _context.OutboundOrders.AnyAsync(o => o.SalesOrderId == salesOrderId) ||
                await _context.InboundOrders.AnyAsync(o => o.SalesOrderId == salesOrderId))
                return (false, "SO đã có phiếu nhập/xuất; không được từ chối.");
            await ReleaseReservationsAsync(salesOrderId, userId);
            order.Status = "REJECTED";
            order.Notes = OrderWorkflowNotes.AppendIfFits(order.Notes, $"Quản lý từ chối: {reason}");
            order.UpdatedAt = DateTime.UtcNow;
            if (_auditLogService != null) await _auditLogService.StageAsync(new AuditEventDto {
                UserId = userId, ActionType = "REJECT_SALES_ORDER", EntityName = AuditEntities.SalesOrder,
                EntityId = order.SalesOrderId.ToString(), OldValues = new { Status = "DRAFT" },
                NewValues = new { order.Status, Reason = reason } });
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, "Đã từ chối SO và giải phóng giữ tồn.");
        }
    }
}



