using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.Interfaces;
using BMWMS.Business.Interfaces.Inventory;
using QuantityRules = BMWMS.Business.Common.QuantityRules;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BMWMS.Business.Services.Inventory;

public class OutboundOrderService : IOutboundOrderService
{
    private readonly IOutboundOrderRepository _outboundOrderRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly BmwmsContext _context;
    private readonly IAuditLogService _auditLogService;

    public OutboundOrderService(
        IOutboundOrderRepository outboundOrderRepo,
        IInventoryRepository inventoryRepo,
        BmwmsContext context,
        IAuditLogService auditLogService)
    {
        _outboundOrderRepo = outboundOrderRepo;
        _inventoryRepo = inventoryRepo;
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResultDto<OutboundOrderListDto>> GetOutboundOrdersAsync(OutboundOrderQueryFilter filter)
    {
        var pageIndex = Math.Max(1, filter.PageIndex);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var rawOrders = (await _outboundOrderRepo.GetAllAsync(filter.Search, filter.Status, filter.WarehouseId)).ToList();
        if (filter.AssignedToUserId.HasValue)
            rawOrders = rawOrders.Where(o => o.AssignedToUserId == filter.AssignedToUserId.Value).ToList();
        if (!string.IsNullOrWhiteSpace(filter.SourceType))
            rawOrders = rawOrders.Where(o => o.SourceType == filter.SourceType).ToList();
        rawOrders = filter.SortOrder == "oldest"
            ? rawOrders.OrderBy(o => o.CreatedAt).ThenBy(o => o.OutboundOrderId).ToList()
            : rawOrders.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.OutboundOrderId).ToList();
        return new PagedResultDto<OutboundOrderListDto>
        {
            TotalCount = rawOrders.Count,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = rawOrders.Skip((pageIndex - 1) * pageSize).Take(pageSize).Select(MapList).ToList()
        };
    }

    public async Task<OutboundOrderDetailDto?> GetOutboundOrderByIdAsync(long id)
    {
        var order = await _outboundOrderRepo.GetByIdAsync(id);
        return order == null ? null : MapDetail(order);
    }

    public async Task<List<UserSelectDto>> GetWarehouseStaffAsync()
    {
        return await _context.Users
            .Where(u => u.Status == "ACTIVE" && u.Role.RoleCode == "WAREHOUSE_STAFF" &&
                        !u.InboundOrderAssignedToUsers.Any(o =>
                            o.Status != "CANCELLED" && o.Status != "PUTAWAY_COMPLETED" &&
                            (o.Status != "COMPLETED" ||
                             !o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                                 .Any(d => d.ConditionStatus == "GOOD") ||
                             o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                                 .Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null))) &&
                        !u.OutboundOrderAssignedToUsers.Any(o =>
                            o.Status != "CANCELLED" && o.Status != "COMPLETED"))
            .OrderBy(u => u.FullName).ThenBy(u => u.Username)
            .Select(u => new UserSelectDto
            {
                UserId = u.UserId,
                FullName = u.FullName ?? u.Username
            })
            .ToListAsync();
    }

    public async Task<List<PurchaseOrderReturnOptionDto>> GetReturnablePurchaseOrdersAsync()
    {
        var orders = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Where(p => p.Status == "PARTIALLY_RECEIVED" || p.Status == "RECEIVED" ||
                        p.Status == "COMPLETED" || p.Status == "CLOSED" ||
                        p.Status == "PENDING_RECEIPT_REVIEW" || p.Status == "PENDING_REMAINDER_CONFIRMATION")
            .OrderByDescending(p => p.PurchaseOrderId)
            .ToListAsync();
        var result = new List<PurchaseOrderReturnOptionDto>();
        foreach (var order in orders)
        {
            var detail = await GetPurchaseOrderForReturnAsync(order.PurchaseOrderId);
            if (detail?.Items.Any(i => i.RemainingQuantity > 0) == true)
                result.Add(new PurchaseOrderReturnOptionDto
                {
                    PurchaseOrderId = order.PurchaseOrderId,
                    PurchaseOrderNumber = order.PurchaseOrderNumber,
                    SupplierName = order.Supplier?.SupplierName ?? string.Empty
                });
        }
        return result;
    }

    public async Task<PurchaseOrderForReturnDto?> GetPurchaseOrderForReturnAsync(long purchaseOrderId)
    {
        if (await _context.OutboundOrders.AnyAsync(o => o.PurchaseOrderId == purchaseOrderId && o.SourceType == "PURCHASE_RETURN" && o.Status == "PENDING_APPROVAL")) return null;
        var warehouseIds = await _context.Warehouses.Where(warehouse => warehouse.Status == "ACTIVE")
            .Select(warehouse => warehouse.WarehouseId).Take(2).ToListAsync();
        if (warehouseIds.Count != 1) return null;
        var warehouseId = warehouseIds[0];
        var order = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderDetails).ThenInclude(d => d.Product).ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId);
        if (order == null || NormalizePurchaseStatus(order.Status) is not ("PARTIALLY_RECEIVED" or "RECEIVED"))
            return null;
        var result = new PurchaseOrderForReturnDto
        {
            PurchaseOrderId = order.PurchaseOrderId,
            PurchaseOrderNumber = order.PurchaseOrderNumber,
            SupplierName = order.Supplier?.SupplierName ?? string.Empty
        };
        foreach (var line in order.PurchaseOrderDetails)
        {
            var received = await _context.InventoryTransactions.Where(t => t.TransactionType == "INBOUND" &&
                    t.InboundOrderDetail != null &&
                    t.InboundOrderDetail.InboundOrderItem.InboundOrder.PurchaseOrderId == order.PurchaseOrderId &&
                    t.ProductId == line.ProductId).SumAsync(t => t.OnHandDelta);
            var returned = await _context.OutboundOrderItems.Where(i => i.OutboundOrder.SourceType == "PURCHASE_RETURN" &&
                    i.OutboundOrder.PurchaseOrderId == order.PurchaseOrderId && i.OutboundOrder.Status != "CANCELLED" &&
                    i.ProductId == line.ProductId)
                .SumAsync(i => i.OutboundOrder.Status == "COMPLETED" ? i.IssuedQuantity : i.RequestedQuantity);
            var physicalAvailable = (await GetPurchaseReturnSourceRowsAsync(
                order.PurchaseOrderId, warehouseId, line.ProductId)).Sum(row => row.Quantity);
            var remaining = Math.Max(0, Math.Min(received - returned, physicalAvailable));
            if (remaining <= 0) continue;
            result.Items.Add(new PurchaseOrderReturnItemDto
            {
                ProductId = line.ProductId,
                ProductCode = line.Product.ProductCode,
                ProductName = line.Product.ProductName,
                UnitName = line.Product.UnitOfMeasure?.UnitName ?? string.Empty,
                QuantityScale = line.Product.UnitOfMeasure?.QuantityScale ?? 0,
                TrackLot = line.Product.TrackLot,
                ReceivedQuantity = received,
                ReturnedQuantity = returned,
                RemainingQuantity = remaining,
                SourceLocations = await GetReturnLocationDetailsAsync(order.PurchaseOrderId, warehouseId, line.ProductId)
            });
        }
        return result;
    }

    public async Task<OutboundOrderDetailDto> CreateOutboundOrderAsync(CreateOutboundOrderRequest request, long createdByUserId)
    {
        request.SourceType = (request.SourceType ?? string.Empty).Trim().ToUpperInvariant();
        if (request.SourceType is not ("SALES_ORDER" or "PURCHASE_RETURN"))
            throw new ArgumentException("Phiếu xuất chỉ hỗ trợ bán hàng từ SO hoặc trả hàng nhà cung cấp từ PO.");
        if (request.ExpectedIssueDate < DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Ngày dự kiến xuất không được trước ngày hiện tại.");
        if (request.SourceType == "PURCHASE_RETURN" && (request.Notes?.Trim().Length ?? 0) < 10)
            throw new ArgumentException("Phiếu trả nhà cung cấp phải ghi rõ lý do (ít nhất 10 ký tự).");
        if (request.Items == null || request.Items.Count == 0 || request.Items.Any(i => i.RequestedQuantity <= 0))
            throw new ArgumentException("Phiếu xuất phải có ít nhất một mặt hàng với số lượng lớn hơn 0.");
        if (request.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
            throw new ArgumentException("Mỗi sản phẩm chỉ được xuất hiện một lần trong phiếu xuất.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.UnitOfMeasure)
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);
        foreach (var requested in request.Items)
        {
            if (!products.TryGetValue(requested.ProductId, out var product) || !IsActive(product.Status))
                throw new ArgumentException($"Sản phẩm ID {requested.ProductId} không tồn tại hoặc đã ngừng hoạt động.");
            QuantityRules.EnsureValid(product, requested.RequestedQuantity, "Số lượng yêu cầu");
        }

        var activeWarehouses = await _context.Warehouses
            .Where(w => w.Status == "ACTIVE")
            .OrderBy(w => w.WarehouseId)
            .Take(2)
            .ToListAsync();
        if (activeWarehouses.Count != 1)
            throw new InvalidOperationException("Hệ thống một kho phải có đúng một kho đang hoạt động.");
        request.WarehouseId = activeWarehouses[0].WarehouseId;

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var creator = await _context.Users.Include(user => user.Role)
                .FirstOrDefaultAsync(user => user.UserId == createdByUserId && user.Status == "ACTIVE")
                ?? throw new InvalidOperationException("Tài khoản tạo phiếu không tồn tại hoặc đã ngừng hoạt động.");
            var creatorRole = creator.Role?.RoleCode;
            if (request.SourceType == "SALES_ORDER" && creatorRole != "WAREHOUSE_STAFF")
                throw new InvalidOperationException("Chỉ Nhân viên kho được tự nhận và tạo đợt xuất bán từ SO đã được Quản lý kho xác nhận.");
            if (request.SourceType == "PURCHASE_RETURN" && creatorRole is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN"))
                throw new InvalidOperationException("Chỉ Quản lý kho được quyết định tạo đợt trả hàng nhà cung cấp từ PO.");

            // Outbound là chứng từ tác nghiệp vật lý, không có bước Nháp riêng.
            // Xuất bán: nhân viên kho tự nhận đợt. Trả NCC: quản lý chọn nhân viên thực hiện.
            request.AssignedToUserId = request.SourceType == "SALES_ORDER"
                ? createdByUserId
                : request.AssignedToUserId;
            if (!request.AssignedToUserId.HasValue)
                throw new ArgumentException("Phải chọn Nhân viên kho thực hiện đợt trả nhà cung cấp.");
            await EnsureWarehouseStaffAssigneeAsync(request.AssignedToUserId);
            if (await _context.OutboundOrders.AnyAsync(o => o.Status == "PENDING_APPROVAL" &&
                (request.SourceType == "SALES_ORDER" ? o.SalesOrderId == request.SalesOrderId :
                    o.PurchaseOrderId == request.PurchaseOrderId && o.SourceType == "PURCHASE_RETURN")))
                throw new InvalidOperationException("Đợt xuất trước đang chờ Quản lý kho duyệt chốt. Chưa thể tạo đợt mới.");
            Dictionary<long, decimal> maximumByProduct;
            if (request.SourceType == "SALES_ORDER")
            {
                if (!request.SalesOrderId.HasValue)
                    throw new ArgumentException("Phải chọn SO tham chiếu cho luồng bán hàng.");
                request.PurchaseOrderId = null;
                var salesOrder = await _context.SalesOrders
                    .Include(s => s.SalesOrderDetails).ThenInclude(d => d.Product)
                    .Include(s => s.OutboundOrders)
                    .FirstOrDefaultAsync(s => s.SalesOrderId == request.SalesOrderId.Value)
                    ?? throw new ArgumentException("Không tìm thấy SO tham chiếu.");
                if (salesOrder.Status is not ("CONFIRMED" or "APPROVED" or "ALLOCATED" or "PARTIALLY_FULFILLED"))
                    throw new InvalidOperationException("SO chưa xác nhận/giữ tồn hoặc đã kết thúc.");

                maximumByProduct = new Dictionary<long, decimal>();
                foreach (var detail in salesOrder.SalesOrderDetails)
                {
                    var remainingToIssue = Math.Max(0, detail.OrderedQuantity - detail.FulfilledQuantity);
                    var activePlanned = await _context.OutboundOrderItems
                        .Where(item => item.OutboundOrder.SalesOrderId == salesOrder.SalesOrderId &&
                                       (item.OutboundOrder.Status == "DRAFT" ||
                                        item.OutboundOrder.Status == "ASSIGNED" ||
                                        item.OutboundOrder.Status == "IN_PROGRESS") &&
                                       item.ProductId == detail.ProductId)
                        .SumAsync(item => item.RequestedQuantity - item.IssuedQuantity);
                    var activeReserved = await _context.InventoryReservations
                        .Where(r => r.SalesOrderDetailId == detail.SalesOrderDetailId &&
                                    (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"))
                        .SumAsync(r => r.ReservedQuantity - r.ConsumedQuantity);

                    // Việc giữ tồn thuộc bước Quản lý kho xác nhận SO. Outbound không được
                    // tự ý giữ thêm hàng vì sẽ vượt thẩm quyền của Nhân viên kho.
                    detail.ReservedQuantity = activeReserved;
                    maximumByProduct[detail.ProductId] = Math.Max(0, Math.Min(
                        remainingToIssue - activePlanned,
                        activeReserved - activePlanned));
                }
            }
            else
            {
                if (!request.PurchaseOrderId.HasValue)
                    throw new ArgumentException("Phải chọn PO tham chiếu cho luồng trả hàng nhà cung cấp.");
                request.SalesOrderId = null;
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderDetails)
                    .FirstOrDefaultAsync(p => p.PurchaseOrderId == request.PurchaseOrderId.Value)
                    ?? throw new ArgumentException("Không tìm thấy PO tham chiếu.");
                if (NormalizePurchaseStatus(purchaseOrder.Status) is not ("PARTIALLY_RECEIVED" or "RECEIVED"))
                    throw new InvalidOperationException("PO chưa phát sinh hàng đã nhận để trả nhà cung cấp.");

                maximumByProduct = new Dictionary<long, decimal>();
                foreach (var detail in purchaseOrder.PurchaseOrderDetails)
                {
                    var received = await _context.InventoryTransactions
                        .Where(t => t.TransactionType == "INBOUND" && t.InboundOrderDetail != null &&
                                    t.InboundOrderDetail.InboundOrderItem.InboundOrder.PurchaseOrderId == purchaseOrder.PurchaseOrderId &&
                                    t.ProductId == detail.ProductId)
                        .SumAsync(t => t.OnHandDelta);
                    var plannedReturns = await _context.OutboundOrderItems
                        .Where(i => i.OutboundOrder.SourceType == "PURCHASE_RETURN" &&
                                    i.OutboundOrder.PurchaseOrderId == purchaseOrder.PurchaseOrderId &&
                                    i.OutboundOrder.Status != "CANCELLED" && i.ProductId == detail.ProductId)
                        .SumAsync(i => i.OutboundOrder.Status == "COMPLETED" ? i.IssuedQuantity : i.RequestedQuantity);
                    maximumByProduct[detail.ProductId] = Math.Max(0, received - plannedReturns);
                }
            }

            foreach (var requested in request.Items)
                if (!maximumByProduct.TryGetValue(requested.ProductId, out var maximum) || requested.RequestedQuantity > maximum)
                    throw new ArgumentException($"Sản phẩm ID {requested.ProductId} vượt số lượng có thể xuất ({maximum}).");

            var orderNumber = $"OUT-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            var outbound = new OutboundOrder
            {
                OutboundOrderNumber = orderNumber,
                WarehouseId = request.WarehouseId,
                SourceType = request.SourceType,
                SalesOrderId = request.SalesOrderId,
                PurchaseOrderId = request.PurchaseOrderId,
                ExpectedIssueDate = request.ExpectedIssueDate,
                DueDate = request.ExpectedIssueDate,
                AssignedToUserId = request.AssignedToUserId,
                Notes = request.Notes,
                Status = "ASSIGNED",
                CreatedByUserId = createdByUserId,
                ApprovedByUserId = request.SourceType == "PURCHASE_RETURN" ? createdByUserId : null,
                ApprovedAt = request.SourceType == "PURCHASE_RETURN" ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var requested in request.Items)
            {
                outbound.OutboundOrderItems.Add(new OutboundOrderItem
                {
                    ProductId = requested.ProductId,
                    RequestedQuantity = requested.RequestedQuantity,
                    IssuedQuantity = 0,
                    Notes = requested.Notes
                });
            }

            _context.OutboundOrders.Add(outbound);
            await _context.SaveChangesAsync();

            // Phiếu trả NCC giữ đúng lượng hàng từng được nhập từ PO. Việc giữ hàng
            // diễn ra ngay khi Quản lý kho tạo lệnh, trước khi nhân viên bắt đầu lấy.
            if (outbound.SourceType == "PURCHASE_RETURN")
            {
                foreach (var item in outbound.OutboundOrderItems)
                {
                    var allocations = await BuildPurchaseReturnSourceAllocationsAsync(
                        outbound.PurchaseOrderId!.Value,
                        outbound.WarehouseId,
                        item.ProductId,
                        item.RequestedQuantity);
                    var selectedLocations = request.Items.Single(r => r.ProductId == item.ProductId).RequestedLocations;
                    if (selectedLocations.Count > 0)
                    {
                        if (selectedLocations.Any(r => r.Quantity <= 0) || selectedLocations.Sum(r => r.Quantity) != item.RequestedQuantity ||
                            selectedLocations.GroupBy(r => (r.StorageLocationId, r.ProductLotId)).Any(g => g.Count() > 1))
                            throw new ArgumentException("Số lượng chọn theo vị trí phải lớn hơn 0, không trùng và bằng tổng số muốn trả.");
                        var productForReturn = await _context.Products.Include(p => p.UnitOfMeasure).SingleAsync(p => p.ProductId == item.ProductId);
                        var sourceRows = await GetPurchaseReturnSourceRowsAsync(outbound.PurchaseOrderId.Value, outbound.WarehouseId, item.ProductId);
                        allocations = selectedLocations.Select(r =>
                        {
                            QuantityRules.EnsureValid(productForReturn, r.Quantity, "Số muốn trả theo vị trí");
                            var source = sourceRows.SingleOrDefault(s => s.StorageLocationId == r.StorageLocationId && s.ProductLotId == r.ProductLotId);
                            if (source == null || r.Quantity > source.Quantity)
                                throw new ArgumentException("Vị trí không còn đủ hàng khả dụng thuộc đúng PO này.");
                            return source with { Quantity = r.Quantity };
                        }).ToList();
                    }
                    var remaining = item.RequestedQuantity;
                    foreach (var allocation in allocations)
                    {
                        if (remaining <= 0) break;
                        var quantity = Math.Min(remaining, allocation.Quantity);
                        var reservation = new InventoryReservation
                        {
                            OutboundOrderItemId = item.OutboundOrderItemId,
                            ProductId = item.ProductId,
                            StorageLocationId = allocation.StorageLocationId,
                            ProductLotId = allocation.ProductLotId,
                            ReservedQuantity = quantity,
                            ConsumedQuantity = 0,
                            Status = "ACTIVE",
                            ReservedByUserId = createdByUserId,
                            ReservedAt = DateTime.UtcNow
                        };
                        _context.InventoryReservations.Add(reservation);
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionType = "RESERVE",
                            ProductId = item.ProductId,
                            StorageLocationId = allocation.StorageLocationId,
                            ProductLotId = allocation.ProductLotId,
                            OnHandDelta = 0,
                            ReservedDelta = quantity,
                            InventoryReservation = reservation,
                            PerformedByUserId = createdByUserId,
                            TransactionAt = DateTime.UtcNow,
                            Notes = $"Giữ hàng trả NCC theo {outbound.OutboundOrderNumber}."
                        });
                        remaining -= quantity;
                    }
                    if (remaining > 0)
                        throw new InvalidOperationException($"Tồn đúng nguồn PO không đủ cho sản phẩm ID {item.ProductId}.");
                }
            }

            _context.Notifications.Add(new Notification
            {
                UserId = request.AssignedToUserId.Value,
                NotificationType = "WORK_ASSIGNMENT",
                Title = request.SourceType == "SALES_ORDER" ? "Bạn đã nhận một đợt xuất bán" : "Bạn được giao một đợt trả nhà cung cấp",
                Message = $"Phiếu {outbound.OutboundOrderNumber} đã sẵn sàng. Hãy đối chiếu hàng vật lý trước khi bắt đầu.",
                ReferenceType = "OUTBOUND_ORDER",
                ReferenceId = outbound.OutboundOrderId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = createdByUserId,
                ActionType = request.SourceType == "SALES_ORDER" ? "CREATE_CLAIM_OUTBOUND" : "CREATE_ASSIGN_RETURN_OUTBOUND",
                EntityName = AuditEntities.OutboundOrder,
                EntityId = outbound.OutboundOrderNumber,
                NewValues = new
                {
                    outbound.SourceType,
                    outbound.SalesOrderId,
                    outbound.PurchaseOrderId,
                    outbound.Status,
                    Items = request.Items.Select(item => new { item.ProductId, item.RequestedQuantity })
                }
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (await GetOutboundOrderByIdAsync(outbound.OutboundOrderId))!;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool Success, string Message)> UpdateDraftAsync(
        long outboundOrderId,
        UpdateOutboundOrderRequest request,
        long userId)
    {
        if (request.ExpectedIssueDate < DateOnly.FromDateTime(DateTime.Today))
            return (false, "Ngày dự kiến xuất không được trước ngày hiện tại.");
        if (request.Items == null || request.Items.Count == 0 || request.Items.Any(item => item.RequestedQuantity <= 0))
            return (false, "Phiếu xuất phải có ít nhất một mặt hàng với số lượng lớn hơn 0.");
        if (request.Items.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
            return (false, "Mỗi sản phẩm chỉ được xuất hiện một lần trong phiếu xuất.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var order = await _context.OutboundOrders
                .Include(value => value.OutboundOrderItems).ThenInclude(value => value.Product).ThenInclude(value => value.UnitOfMeasure)
                .Include(value => value.SalesOrder).ThenInclude(value => value!.SalesOrderDetails)
                .Include(value => value.PurchaseOrder).ThenInclude(value => value!.PurchaseOrderDetails)
                .FirstOrDefaultAsync(value => value.OutboundOrderId == outboundOrderId);
            if (order == null) return (false, "Không tìm thấy phiếu xuất.");
            if (order.Status != "DRAFT") return (false, "Chỉ phiếu Nháp mới được chỉnh sửa.");
            if (order.CreatedByUserId != userId) return (false, "Chỉ người tạo phiếu được chỉnh sửa phiếu Nháp.");
            if (order.SourceType == "PURCHASE_RETURN" && (request.Notes?.Trim().Length ?? 0) < 10)
                return (false, "Phiếu trả nhà cung cấp phải ghi rõ lý do (ít nhất 10 ký tự).");

            var originalProducts = order.OutboundOrderItems.Select(item => item.ProductId).OrderBy(value => value).ToArray();
            var requestedProducts = request.Items.Select(item => item.ProductId).OrderBy(value => value).ToArray();
            if (!originalProducts.SequenceEqual(requestedProducts))
                return (false, "Không được thay đổi danh sách sản phẩm của phiếu. Hãy hủy phiếu Nháp và tạo lại nếu chọn sai mặt hàng.");

            var oldSnapshot = new
            {
                order.ExpectedIssueDate,
                order.Notes,
                Items = order.OutboundOrderItems.Select(item => new { item.ProductId, item.RequestedQuantity }).ToList()
            };
            foreach (var requested in request.Items)
            {
                var item = order.OutboundOrderItems.Single(value => value.ProductId == requested.ProductId);
                QuantityRules.EnsureValid(item.Product, requested.RequestedQuantity, "Số lượng đợt xuất");
                decimal maximum;
                if (order.SourceType == "SALES_ORDER")
                {
                    var sourceLine = order.SalesOrder!.SalesOrderDetails.SingleOrDefault(value => value.ProductId == item.ProductId);
                    if (sourceLine == null) return (false, "Sản phẩm không còn thuộc SO tham chiếu.");
                    var otherPlanned = await _context.OutboundOrderItems
                        .Where(value => value.OutboundOrderId != order.OutboundOrderId &&
                                        value.OutboundOrder.SalesOrderId == order.SalesOrderId &&
                                        (value.OutboundOrder.Status == "DRAFT" || value.OutboundOrder.Status == "ASSIGNED" || value.OutboundOrder.Status == "IN_PROGRESS") &&
                                        value.ProductId == item.ProductId)
                        .SumAsync(value => value.RequestedQuantity - value.IssuedQuantity);
                    maximum = Math.Max(0, sourceLine.OrderedQuantity - sourceLine.FulfilledQuantity - otherPlanned);
                }
                else
                {
                    var received = await _context.InventoryTransactions
                        .Where(value => value.TransactionType == "INBOUND" && value.InboundOrderDetail != null &&
                                        value.InboundOrderDetail.InboundOrderItem.InboundOrder.PurchaseOrderId == order.PurchaseOrderId &&
                                        value.ProductId == item.ProductId)
                        .SumAsync(value => value.OnHandDelta);
                    var otherReturns = await _context.OutboundOrderItems
                        .Where(value => value.OutboundOrderId != order.OutboundOrderId &&
                                        value.OutboundOrder.SourceType == "PURCHASE_RETURN" &&
                                        value.OutboundOrder.PurchaseOrderId == order.PurchaseOrderId &&
                                        value.OutboundOrder.Status != "CANCELLED" && value.ProductId == item.ProductId)
                        .SumAsync(value => value.OutboundOrder.Status == "COMPLETED" ? value.IssuedQuantity : value.RequestedQuantity);
                    maximum = Math.Max(0, received - otherReturns);
                }
                if (requested.RequestedQuantity > maximum)
                    return (false, $"Số lượng của {item.Product.ProductCode} vượt mức còn có thể xuất ({maximum}).");
                item.RequestedQuantity = requested.RequestedQuantity;
                item.Notes = requested.Notes;
            }

            order.ExpectedIssueDate = request.ExpectedIssueDate;
            order.DueDate = request.ExpectedIssueDate;
            order.Notes = request.Notes?.Trim();
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = userId,
                ActionType = "UPDATE_OUTBOUND_DRAFT",
                EntityName = AuditEntities.OutboundOrder,
                EntityId = order.OutboundOrderNumber,
                OldValues = oldSnapshot,
                NewValues = new
                {
                    order.ExpectedIssueDate,
                    order.Notes,
                    Items = order.OutboundOrderItems.Select(item => new { item.ProductId, item.RequestedQuantity }).ToList()
                }
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (true, "Đã cập nhật phiếu xuất Nháp.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, GetSafeErrorMessage(ex, "Không thể cập nhật phiếu xuất."));
        }
    }

    public async Task<(bool Success, string Message)> ApproveAndAssignAsync(
        long outboundOrderId,
        long assignedToUserId,
        long approvedByUserId)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var approver = await _context.Users.Include(user => user.Role)
                .FirstOrDefaultAsync(user => user.UserId == approvedByUserId && user.Status == "ACTIVE");
            if (approver?.Role?.RoleCode is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN"))
                return (false, "Chỉ Quản lý kho được duyệt và phân công phiếu xuất.");

            var order = await _context.OutboundOrders
                .Include(value => value.OutboundOrderItems)
                .Include(value => value.SalesOrder)
                .FirstOrDefaultAsync(value => value.OutboundOrderId == outboundOrderId);
            if (order == null) return (false, "Không tìm thấy phiếu xuất.");
            if (order.Status != "DRAFT")
                return (false, "Chỉ phiếu Nháp mới được duyệt và phân công.");

            await EnsureWarehouseStaffAssigneeAsync(assignedToUserId, excludeOutboundOrderId: order.OutboundOrderId);

            if (order.SourceType == "SALES_ORDER")
            {
                foreach (var item in order.OutboundOrderItems)
                {
                    var reserved = await _context.InventoryReservations
                        .Where(value => value.SalesOrderDetail != null &&
                                        value.SalesOrderDetail.SalesOrderId == order.SalesOrderId &&
                                        value.ProductId == item.ProductId &&
                                        (value.Status == "ACTIVE" || value.Status == "PARTIALLY_CONSUMED"))
                        .SumAsync(value => value.ReservedQuantity - value.ConsumedQuantity);
                    if (reserved < item.RequestedQuantity - item.IssuedQuantity)
                        return (false, $"Giữ tồn của SO không còn đủ cho sản phẩm ID {item.ProductId}. Vui lòng kiểm tra lại SO.");
                }
            }
            else
            {
                foreach (var item in order.OutboundOrderItems)
                {
                    var allocations = await BuildPurchaseReturnSourceAllocationsAsync(
                        order.PurchaseOrderId!.Value,
                        order.WarehouseId,
                        item.ProductId,
                        item.RequestedQuantity);
                    var remaining = item.RequestedQuantity;
                    foreach (var allocation in allocations)
                    {
                        if (remaining <= 0) break;
                        var quantity = Math.Min(remaining, allocation.Quantity);
                        var reservation = new InventoryReservation
                        {
                            SalesOrderDetailId = null,
                            OutboundOrderItemId = item.OutboundOrderItemId,
                            ProductId = item.ProductId,
                            StorageLocationId = allocation.StorageLocationId,
                            ProductLotId = allocation.ProductLotId,
                            ReservedQuantity = quantity,
                            ConsumedQuantity = 0,
                            Status = "ACTIVE",
                            ReservedByUserId = approvedByUserId,
                            ReservedAt = DateTime.UtcNow
                        };
                        _context.InventoryReservations.Add(reservation);
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionType = "RESERVE",
                            ProductId = item.ProductId,
                            StorageLocationId = allocation.StorageLocationId,
                            ProductLotId = allocation.ProductLotId,
                            OnHandDelta = 0,
                            ReservedDelta = quantity,
                            InventoryReservation = reservation,
                            PerformedByUserId = approvedByUserId,
                            TransactionAt = DateTime.UtcNow,
                            Notes = $"Giữ hàng trả NCC theo {order.OutboundOrderNumber}."
                        });
                        remaining -= quantity;
                    }
                    if (remaining > 0)
                        return (false, $"Tồn đúng nguồn PO không đủ cho sản phẩm ID {item.ProductId}.");
                }
            }

            order.AssignedToUserId = assignedToUserId;
            order.ApprovedByUserId = approvedByUserId;
            order.ApprovedAt = DateTime.UtcNow;
            order.Status = "ASSIGNED";
            _context.Notifications.Add(new Notification
            {
                UserId = assignedToUserId,
                NotificationType = "WORK_ASSIGNMENT",
                Title = "Bạn được giao một phiếu xuất kho mới",
                Message = $"Phiếu {order.OutboundOrderNumber} đã được duyệt và giao cho bạn. Vui lòng kiểm tra trước khi bắt đầu.",
                ReferenceType = "OUTBOUND_ORDER",
                ReferenceId = order.OutboundOrderId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = approvedByUserId,
                ActionType = "APPROVE_ASSIGN_OUTBOUND",
                EntityName = AuditEntities.OutboundOrder,
                EntityId = order.OutboundOrderNumber,
                OldValues = new { Status = "DRAFT", AssignedToUserId = (long?)null },
                NewValues = new { Status = "ASSIGNED", AssignedToUserId = assignedToUserId }
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (true, "Đã duyệt phiếu và phân công nhân viên kho.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, GetSafeErrorMessage(ex, "Không thể duyệt phiếu xuất."));
        }
    }

    public async Task<(bool Success, string Message)> StartProcessingAsync(long outboundOrderId, long userId)
    {
        var order = await _context.OutboundOrders.FirstOrDefaultAsync(value => value.OutboundOrderId == outboundOrderId);
        if (order == null) return (false, "Không tìm thấy phiếu xuất.");
        if (order.Status != "ASSIGNED") return (false, "Chỉ phiếu Sẵn sàng mới được bắt đầu xử lý.");
        if (order.AssignedToUserId != userId) return (false, "Bạn không được phân công thực hiện phiếu xuất này.");
        var validStaff = await _context.Users.AnyAsync(user => user.UserId == userId && user.Status == "ACTIVE" &&
            user.Role.RoleCode == "WAREHOUSE_STAFF");
        if (!validStaff) return (false, "Chỉ nhân viên kho đang hoạt động mới được bắt đầu phiếu.");

        order.Status = "IN_PROGRESS";
        await _auditLogService.StageAsync(new AuditEventDto
        {
            UserId = userId,
            ActionType = "START_OUTBOUND",
            EntityName = AuditEntities.OutboundOrder,
            EntityId = order.OutboundOrderNumber,
            OldValues = new { Status = "ASSIGNED" },
            NewValues = new { Status = "IN_PROGRESS" }
        });
        await _context.SaveChangesAsync();
        return (true, "Đã bắt đầu tác nghiệp xuất hàng.");
    }

    public async Task<(bool Success, string Message)> CancelOutboundOrderAsync(long outboundOrderId, long userId, string reason)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 5 or > 500)
            return (false, "Lý do hủy phải có từ 5 đến 500 ký tự.");
        var order = await _context.OutboundOrders
            .Include(o => o.OutboundOrderItems).ThenInclude(i => i.OutboundOrderDetails)
            .Include(o => o.OutboundOrderItems).ThenInclude(i => i.InventoryReservations)
            .FirstOrDefaultAsync(o => o.OutboundOrderId == outboundOrderId);
        if (order == null) return (false, "Không tìm thấy phiếu xuất.");
        if (order.Status is not ("DRAFT" or "ASSIGNED" or "IN_PROGRESS") || order.OutboundOrderItems.Any(i => i.OutboundOrderDetails.Count > 0))
            return (false, "Chỉ được hủy phiếu chưa phát sinh bất kỳ số lượng thực xuất nào.");

        var actor = await _context.Users.Include(value => value.Role).FirstOrDefaultAsync(value => value.UserId == userId);
        var role = actor?.Role?.RoleCode;
        var canCancelDraft = order.Status == "DRAFT" && role is "WAREHOUSE_MANAGER" or "SYSTEM_ADMIN";
        var canCancelAssigned = order.Status is "ASSIGNED" or "IN_PROGRESS" && role is "WAREHOUSE_MANAGER" or "SYSTEM_ADMIN";
        canCancelAssigned |= order.Status is "ASSIGNED" or "IN_PROGRESS" && role == "WAREHOUSE_STAFF" &&
                             order.AssignedToUserId == userId;
        if (!canCancelDraft && !canCancelAssigned)
            return (false, order.Status == "DRAFT"
                ? "Phiếu Nháp cũ chỉ được Quản lý kho xử lý hoặc hủy."
                : "Chỉ Quản lý kho hoặc Nhân viên kho phụ trách được dừng phiếu khi chưa có số thực xuất.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Giữ tồn thuộc vòng đời SO. Hủy phiếu xuất chỉ hủy tác nghiệp lấy hàng;
            // SO vẫn giữ hàng để có thể lập lại một phiếu xuất khác.
            var oldStatus = order.Status;
            order.Status = "CANCELLED";
            order.CancelledAt = DateTime.UtcNow;
            order.CancelledByUserId = userId;
            order.CancellationReason = reason;
            if (order.SourceType == "PURCHASE_RETURN")
                await ReleaseReturnReservationsAsync(order, userId, "Hủy phiếu trả nhà cung cấp.");
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = userId,
                ActionType = "CANCEL_OUTBOUND",
                EntityName = AuditEntities.OutboundOrder,
                EntityId = order.OutboundOrderNumber,
                OldValues = new { Status = oldStatus },
                NewValues = new { Status = "CANCELLED", Reason = reason }
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (true, "Đã hủy phiếu xuất.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, GetSafeErrorMessage(ex, "Không thể hủy phiếu xuất."));
        }
    }

    public async Task<OutboundProcessViewDto?> GetOutboundProcessDetailAsync(long outboundOrderId)
    {
        var order = await _outboundOrderRepo.GetByIdAsync(outboundOrderId);
        if (order == null) return null;
        var result = new OutboundProcessViewDto
        {
            OutboundOrderId = order.OutboundOrderId,
            OutboundOrderNumber = order.OutboundOrderNumber,
            SourceType = order.SourceType,
            SalesOrderNumber = order.SalesOrder?.SalesOrderNumber,
            PurchaseOrderNumber = order.PurchaseOrder?.PurchaseOrderNumber,
            SourceReference = GetSourceReference(order),
            PartnerName = GetPartnerName(order),
            CustomerName = GetPartnerName(order),
            WarehouseId = order.WarehouseId,
            WarehouseName = order.Warehouse?.WarehouseName ?? string.Empty,
            AssignedToUserId = order.AssignedToUserId,
            Status = NormalizeOutboundStatus(order.Status),
            Notes = order.Notes,
            ExpectedIssueDate = order.ExpectedIssueDate
        };
        foreach (var item in order.OutboundOrderItems)
        {
            result.Items.Add(new OutboundProcessItemDto
            {
                OutboundOrderItemId = item.OutboundOrderItemId,
                ProductId = item.ProductId,
                ProductCode = item.Product?.ProductCode ?? string.Empty,
                ProductName = item.Product?.ProductName ?? string.Empty,
                UnitName = item.Product?.UnitOfMeasure?.UnitName ?? "Đơn vị",
                QuantityScale = item.Product?.UnitOfMeasure?.QuantityScale ?? 0,
                TrackLot = item.Product?.TrackLot ?? false,
                RotationMethod = string.Equals(item.Product?.RotationMethod, "FEFO", StringComparison.OrdinalIgnoreCase)
                    ? "FEFO" : "FIFO",
                RequestedQuantity = item.RequestedQuantity,
                IssuedQuantity = item.IssuedQuantity,
                PickedDetails = item.OutboundOrderDetails.Select(d => new OutboundPickedDetailDto
                {
                    OutboundOrderDetailId = d.OutboundOrderDetailId,
                    LocationCode = d.StorageLocation?.LocationCode ?? string.Empty,
                    LotNumber = d.ProductLot?.LotNumber ?? string.Empty,
                    FirstReceivedDate = d.ProductLot?.FirstReceivedDate ?? default,
                    ExpiryDate = d.ProductLot?.ExpiryDate,
                    IssuedQuantity = d.IssuedQuantity,
                    RecordedByUserName = d.RecordedByUser?.FullName ?? d.RecordedByUser?.Username ?? string.Empty,
                    RecordedAt = d.RecordedAt
                }).ToList(),
                AvailableLocations = await GetAvailableAllocationsAsync(order, item)
            });
        }
        return result;
    }

    public async Task<(bool Success, string Message)> ExecutePickBatchAsync(
        IReadOnlyCollection<ExecutePickItemRequest> requests,
        long userId)
    {
        if (requests == null || requests.Count == 0)
            return (false, "Phải chọn ít nhất một dòng hàng để xuất.");
        if (requests.Any(request => request.PickQuantity <= 0))
            return (false, "Mọi số lượng lấy phải lớn hơn 0.");
        if (requests.Any(request => !request.PhysicalCheckConfirmed || !request.PackingConfirmed))
            return (false, "Phải xác nhận đã kiểm tra hàng vật lý và hoàn tất đóng gói trước khi bàn giao/trừ tồn.");
        if (requests.Select(request => request.OutboundOrderId).Distinct().Count() != 1)
            return (false, "Các dòng lấy hàng phải thuộc cùng một phiếu xuất.");
        if (requests.GroupBy(request => new
            {
                request.OutboundOrderItemId,
                request.StorageLocationId,
                request.ProductLotId
            }).Any(group => group.Count() > 1))
            return (false, "Một vị trí/lớp hàng chỉ được chọn một lần cho mỗi mặt hàng.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var outboundOrderId = requests.First().OutboundOrderId;
            var order = await _context.OutboundOrders
                .Include(o => o.OutboundOrderItems).ThenInclude(i => i.OutboundOrderDetails)
                .Include(o => o.OutboundOrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.UnitOfMeasure)
                .Include(o => o.SalesOrder).ThenInclude(s => s!.SalesOrderDetails)
                .FirstOrDefaultAsync(o => o.OutboundOrderId == outboundOrderId);
            if (order == null) return (false, "Không tìm thấy phiếu xuất kho.");
            if (order.Status != "IN_PROGRESS")
                return (false, "Phải bấm Bắt đầu xử lý trước khi ghi nhận xuất hàng.");
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user?.Status != "ACTIVE" || user.Role?.RoleCode != "WAREHOUSE_STAFF")
                return (false, "Chỉ nhân viên kho đang hoạt động mới được thực hiện lấy/xuất hàng.");
            if (order.AssignedToUserId != userId)
                return (false, "Bạn không được phân công thực hiện phiếu xuất này.");

            foreach (var itemRequests in requests.GroupBy(request => request.OutboundOrderItemId))
            {
                var item = order.OutboundOrderItems.SingleOrDefault(value => value.OutboundOrderItemId == itemRequests.Key);
                if (item == null) return (false, "Dòng hàng không thuộc phiếu xuất.");
                var requestedTotal = itemRequests.Sum(value => value.PickQuantity);
                var itemRemaining = item.RequestedQuantity - item.IssuedQuantity;
                if (requestedTotal > itemRemaining)
                    return (false, $"Tổng số lượng lấy của {item.Product.ProductCode} vượt số lượng còn phải xuất ({itemRemaining}).");
                var routes = await GetAvailableAllocationsAsync(order, item);
                var methods = itemRequests.Select(r => r.PickingMethod?.ToUpperInvariant() ?? "DEFAULT").Distinct().ToList();
                if (methods.Count != 1 || methods[0] is not ("DEFAULT" or "FIFO" or "FEFO" or "CUSTOM"))
                    return (false, "Chọn một phương pháp FIFO, FEFO hoặc tự chọn cho mỗi mặt hàng.");
                var method = methods[0] == "DEFAULT" ? item.Product.RotationMethod : methods[0];
                if (method == "FEFO")
                {
                    if (itemRequests.Any(r => !routes.Any(route => route.StorageLocationId == r.StorageLocationId && route.ProductLotId == r.ProductLotId && route.ExpiryDate.HasValue)))
                        return (false, "FEFO yêu cầu HSD của đợt nhận. Hàng chưa có HSD phải được xác minh trước hoặc chọn cách lấy khác có lý do.");
                    routes = routes.Where(r => r.ExpiryDate.HasValue).ToList();
                }
                routes = method == "FEFO" ? routes.OrderBy(r => r.ExpiryDate == null).ThenBy(r => r.ExpiryDate)
                    .ThenBy(r => r.FirstReceivedDate).ThenBy(r => r.LocationCode, NaturalLocationCodeComparer.Instance).ToList()
                    : routes.OrderBy(r => r.FirstReceivedDate).ThenBy(r => r.LocationCode, NaturalLocationCodeComparer.Instance).ToList();
                var expected = BuildExpectedAllocationMap(routes, requestedTotal);
                var actual = itemRequests
                    .GroupBy(value => (value.StorageLocationId, value.ProductLotId))
                    .ToDictionary(group => group.Key, group => group.Sum(value => value.PickQuantity));
                var followsSuggestion = expected.Count == actual.Count && expected.All(pair =>
                    actual.TryGetValue(pair.Key, out var actualQuantity) && actualQuantity == pair.Value);
                if ((method == "CUSTOM" || !followsSuggestion) && !itemRequests.Any(value => (value.DeviationReason?.Trim().Length ?? 0) is >= 10 and <= 500))
                    return (false, $"Bạn đang lấy {item.Product.ProductCode} khác thứ tự FIFO/FEFO được đề xuất. Hãy nhập lý do (10-500 ký tự).");
            }

            foreach (var request in requests)
            {
                var item = order.OutboundOrderItems.SingleOrDefault(i => i.OutboundOrderItemId == request.OutboundOrderItemId);
                if (item == null) return (false, "Dòng hàng không thuộc phiếu xuất.");
                try
                {
                    QuantityRules.EnsureValid(item.Product, request.PickQuantity, "Số lượng lấy");
                }
                catch (ArgumentException ex)
                {
                    return (false, ex.Message);
                }
                if (request.PickQuantity > item.RequestedQuantity - item.IssuedQuantity)
                    return (false, $"Số lượng lấy của {item.Product.ProductCode} vượt số lượng còn phải xuất.");
                var route = (await GetAvailableAllocationsAsync(order, item)).FirstOrDefault(d =>
                    d.ProductLotId == request.ProductLotId && d.StorageLocationId == request.StorageLocationId);
                if (route == null || request.PickQuantity > route.AvailableQuantity)
                    return (false, $"Vị trí/lớp hàng của {item.Product.ProductCode} không còn đủ số lượng được phép lấy.");

                request.InventoryReservationId = await EnsurePickReservationAsync(order, item, request, userId);

                var location = await _context.StorageLocations
                    .Include(value => value.StorageRack).ThenInclude(value => value!.WarehouseZone)
                    .FirstOrDefaultAsync(value => value.StorageLocationId == request.StorageLocationId);
                if (location == null || location.WarehouseId != order.WarehouseId || !location.IsPickable || !IsActive(location.Status))
                    return (false, "Vị trí nguồn không hoạt động hoặc không cho phép lấy hàng.");
                var configuredGroupId = location.StorageRack?.WarehouseZone?.ProductGroupId;
                if (configuredGroupId.HasValue && configuredGroupId.Value != item.Product.ProductGroupId)
                    return (false, $"Vị trí {location.LocationCode} không được cấu hình cho nhóm của {item.Product.ProductCode}.");
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i =>
                    i.ProductId == item.ProductId && i.StorageLocationId == request.StorageLocationId &&
                    i.ProductLotId == request.ProductLotId);
                if (inventory != null) await _context.Entry(inventory).ReloadAsync();
                if (inventory == null || inventory.OnHandQuantity < request.PickQuantity)
                    return (false, $"Tồn thực tế của {item.Product.ProductCode} tại vị trí đã chọn không đủ.");

                if (!request.InventoryReservationId.HasValue || request.InventoryReservationId <= 0)
                    return (false, "Dòng lấy hàng phải tham chiếu bản ghi giữ tồn hợp lệ.");
                var reservation = await _context.InventoryReservations
                    .Include(r => r.SalesOrderDetail)
                    .FirstOrDefaultAsync(r => r.InventoryReservationId == request.InventoryReservationId.Value);
                var correctSource = order.SourceType == "SALES_ORDER"
                    ? reservation?.SalesOrderDetail?.SalesOrderId == order.SalesOrderId
                    : reservation?.OutboundOrderItemId == item.OutboundOrderItemId;
                if (reservation == null || !correctSource ||
                    reservation.ProductId != item.ProductId || reservation.StorageLocationId != request.StorageLocationId ||
                    reservation.ProductLotId != request.ProductLotId ||
                    reservation.Status is not ("ACTIVE" or "PARTIALLY_CONSUMED") ||
                    request.PickQuantity > reservation.ReservedQuantity - reservation.ConsumedQuantity ||
                    inventory.ReservedQuantity < request.PickQuantity)
                    return (false, "Giữ tồn không hợp lệ, sai nguồn đơn hoặc không còn đủ số lượng.");
                var reservedDelta = -request.PickQuantity;

                var detail = new OutboundOrderDetail
                {
                    OutboundOrderId = order.OutboundOrderId,
                    OutboundOrderItemId = item.OutboundOrderItemId,
                    ProductId = item.ProductId,
                    StorageLocationId = request.StorageLocationId,
                    ProductLotId = request.ProductLotId,
                    InventoryReservationId = reservation.InventoryReservationId,
                    IssuedQuantity = request.PickQuantity,
                    RecordedByUserId = userId,
                    RecordedAt = DateTime.UtcNow,
                    Notes = request.Notes
                };
                var ledger = new InventoryTransaction
                {
                    TransactionType = "OUTBOUND",
                    ProductId = item.ProductId,
                    StorageLocationId = request.StorageLocationId,
                    ProductLotId = request.ProductLotId,
                    OnHandDelta = -request.PickQuantity,
                    ReservedDelta = reservedDelta,
                    OutboundOrderDetail = detail,
                    InventoryReservationId = reservation.InventoryReservationId,
                    PerformedByUserId = userId,
                    TransactionAt = DateTime.UtcNow,
                    Notes = $"Xuất kho theo {order.OutboundOrderNumber}."
                };
                detail.InventoryTransaction = ledger;
                _context.OutboundOrderDetails.Add(detail);
                _context.InventoryTransactions.Add(ledger);
                item.IssuedQuantity += request.PickQuantity;
                reservation.ConsumedQuantity += request.PickQuantity;
                reservation.Status = reservation.ConsumedQuantity >= reservation.ReservedQuantity ? "CONSUMED" : "PARTIALLY_CONSUMED";
                if (order.SourceType == "SALES_ORDER")
                {
                    var salesDetail = order.SalesOrder!.SalesOrderDetails.Single(d => d.SalesOrderDetailId == reservation.SalesOrderDetailId);
                    salesDetail.FulfilledQuantity += request.PickQuantity;
                }

                // Persist each route inside the same DB transaction so triggers and
                // subsequent route checks see the latest on-hand/reserved balance.
                await _context.SaveChangesAsync();
            }

            var completed = order.OutboundOrderItems.All(i => i.IssuedQuantity >= i.RequestedQuantity);
            order.Status = completed ? "PENDING_APPROVAL" : "IN_PROGRESS";
            if (completed)
            {
                order.CompletionType = "FULL";
                order.CompletionReason = null;
            }
            if (order.SalesOrder != null)
            {
                // The physical delivered quantity is current; final SO fulfilment is manager-owned.
                order.SalesOrder.Status = "PARTIALLY_FULFILLED";
                order.SalesOrder.UpdatedAt = DateTime.UtcNow;
            }
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = userId,
                ActionType = "ISSUE_OUTBOUND_BATCH",
                EntityName = AuditEntities.OutboundOrder,
                EntityId = order.OutboundOrderNumber,
                NewValues = new
                {
                    order.OutboundOrderNumber,
                    Status = completed ? "PENDING_APPROVAL" : "IN_PROGRESS",
                    Rows = requests.Select(request => new
                    {
                        request.OutboundOrderItemId,
                        request.StorageLocationId,
                        request.ProductLotId,
                        request.PickQuantity,
                        request.PickingMethod,
                        request.DeviationReason,
                        request.PhysicalCheckConfirmed,
                        request.PackingConfirmed
                    })
                }
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (true, completed ? "Đã ghi đủ thực xuất và gửi Quản lý kho duyệt chốt đợt." : "Đã ghi nhận số lượng thực xuất; có thể lấy tiếp hoặc gửi chốt đợt giao thiếu.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, GetSafeErrorMessage(ex, "Không thể ghi nhận xuất hàng."));
        }
    }

    public async Task<(bool Success, string Message)> CompleteOutboundAsync(long outboundOrderId, long userId, string reason)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 500)
            return (false, "Lý do hoàn tất khi còn thiếu phải có từ 10 đến 500 ký tự.");
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var order = await _context.OutboundOrders
                .Include(o => o.OutboundOrderItems)
                .FirstOrDefaultAsync(o => o.OutboundOrderId == outboundOrderId);
            if (order == null) return (false, "Không tìm thấy phiếu xuất.");
            if (order.Status != "IN_PROGRESS" || !order.OutboundOrderItems.Any(item => item.IssuedQuantity > 0))
                return (false, "Chỉ được hoàn tất sau khi đã ghi nhận ít nhất một số lượng thực xuất.");
            if (order.AssignedToUserId != userId)
                return (false, "Bạn không phải nhân viên chịu trách nhiệm cho đợt giao này.");

            var userIsWarehouseStaff = await _context.Users.AnyAsync(user => user.UserId == userId &&
                user.Status == "ACTIVE" && user.Role.RoleCode == "WAREHOUSE_STAFF");
            if (!userIsWarehouseStaff)
                return (false, "Chỉ nhân viên kho đang hoạt động mới được hoàn tất đợt giao.");

            order.Status = "PENDING_APPROVAL";
            order.CompletionType = order.OutboundOrderItems.All(item => item.IssuedQuantity >= item.RequestedQuantity)
                ? "FULL" : "PARTIAL";
            order.CompletionReason = order.CompletionType == "PARTIAL" ? reason : null;

            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = userId,
                ActionType = "SUBMIT_OUTBOUND_COMPLETION",
                EntityName = AuditEntities.OutboundOrder,
                EntityId = order.OutboundOrderNumber,
                OldValues = new { Status = "IN_PROGRESS" },
                NewValues = new
                {
                    Status = "PENDING_APPROVAL",
                    order.CompletionType,
                    Reason = reason,
                    Items = order.OutboundOrderItems.Select(item => new
                    {
                        item.ProductId,
                        item.RequestedQuantity,
                        item.IssuedQuantity
                    })
                }
            });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (true, order.CompletionType == "FULL"
                ? "Đã gửi chốt đợt xuất đủ hàng để Quản lý kho duyệt."
                : "Đã gửi số thực giao để Quản lý kho duyệt và quyết định phần còn lại.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, GetSafeErrorMessage(ex, "Không thể hoàn tất phiếu xuất."));
        }
    }

    public async Task<(bool Success, string Message)> ReviewCompletionAsync(long outboundOrderId, long userId, ReviewOutboundCompletionRequest request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (actor?.Status != "ACTIVE" || actor.Role?.RoleCode is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN"))
                return (false, "Chỉ Quản lý kho được duyệt chốt đợt xuất.");
            var order = await _context.OutboundOrders.Include(o => o.OutboundOrderItems)
                .Include(o => o.SalesOrder).ThenInclude(s => s!.SalesOrderDetails)
                .FirstOrDefaultAsync(o => o.OutboundOrderId == outboundOrderId);
            if (order == null || order.Status != "PENDING_APPROVAL")
                return (false, "Phiếu không ở trạng thái chờ duyệt chốt đợt.");
            if (!order.OutboundOrderItems.Any(i => i.IssuedQuantity > 0))
                return (false, "Phiếu chưa có số lượng thực xuất.");
            var remaining = order.SourceType == "SALES_ORDER"
                ? order.SalesOrder!.SalesOrderDetails.Any(d => d.FulfilledQuantity < d.OrderedQuantity)
                : order.OutboundOrderItems.Any(i => i.IssuedQuantity < i.RequestedQuantity);
            var action = request.RemainderAction?.Trim().ToUpperInvariant();
            var reason = request.Reason?.Trim();
            if (!string.IsNullOrEmpty(action) && action is not ("CONTINUE" or "CLOSE"))
                return (false, "Quyết định phần còn lại không hợp lệ.");
            if (reason?.Length > 500) return (false, "Căn cứ quyết định không được vượt quá 500 ký tự.");
            if (remaining && (action is not ("CONTINUE" or "CLOSE") || reason?.Length is not (>= 10 and <= 500)))
                return (false, "Chọn giao tiếp hoặc kết thúc phần còn lại và ghi căn cứ từ 10 đến 500 ký tự.");
            if (remaining && action == "CLOSE" && order.SourceType == "SALES_ORDER")
            {
                if (await _context.OutboundOrders.AnyAsync(o => o.OutboundOrderId != outboundOrderId &&
                    o.SalesOrderId == order.SalesOrderId && o.Status != "COMPLETED" && o.Status != "CANCELLED"))
                    return (false, "SO còn đợt xuất khác đang xử lý. Hãy chốt các đợt đó trước khi đóng SO.");
                var reservations = await _context.InventoryReservations.Where(r => r.SalesOrderDetail != null &&
                    r.SalesOrderDetail.SalesOrderId == order.SalesOrderId &&
                    (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED")).ToListAsync();
                foreach (var r in reservations)
                {
                    var quantity = r.ReservedQuantity - r.ConsumedQuantity;
                    if (quantity > 0) _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "RELEASE_RESERVATION", ProductId = r.ProductId,
                        StorageLocationId = r.StorageLocationId, ProductLotId = r.ProductLotId,
                        ReservedDelta = -quantity, OnHandDelta = 0, InventoryReservationId = r.InventoryReservationId,
                        PerformedByUserId = userId, TransactionAt = DateTime.UtcNow,
                        Notes = $"Quản lý đóng phần chưa giao của {order.SalesOrder!.SalesOrderNumber}: {reason}"
                    });
                    r.Status = "RELEASED"; r.ReleasedAt = DateTime.UtcNow;
                }
                foreach (var d in order.SalesOrder!.SalesOrderDetails) d.ReservedQuantity = 0;
            }
            if (order.SalesOrder != null)
            {
                order.SalesOrder.Status = remaining ? (action == "CLOSE" ? "CLOSED" : "PARTIALLY_FULFILLED") : "FULFILLED";
                order.SalesOrder.UpdatedAt = DateTime.UtcNow;
            }
            // Physical quantities were posted by staff. Approval must never post OUTBOUND again.
            if (order.SourceType == "PURCHASE_RETURN")
                await ReleaseReturnReservationsAsync(order, userId, "Quản lý duyệt kết thúc đợt trả nhà cung cấp.");
            order.Status = "COMPLETED";
            order.ConfirmedByUserId = userId; order.ConfirmedAt = DateTime.UtcNow;
            if (remaining) order.Notes = $"{order.Notes}\nQuản lý chốt đợt — {(action == "CONTINUE" ? "Tiếp tục phần còn lại" : "Kết thúc phần còn lại")}: {reason}".Trim();
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = userId, ActionType = "APPROVE_OUTBOUND_COMPLETION", EntityName = AuditEntities.OutboundOrder,
                EntityId = order.OutboundOrderNumber, OldValues = new { Status = "PENDING_APPROVAL" },
                NewValues = new { Status = "COMPLETED", RemainderAction = remaining ? action : "FULL", Reason = reason,
                    SalesOrderStatus = order.SalesOrder?.Status }
            });
            await _context.SaveChangesAsync(); await transaction.CommitAsync();
            return (true, remaining && action == "CONTINUE"
                ? "Đã duyệt chốt đợt xuất. Có thể lập đợt mới cho phần còn lại."
                : "Đã duyệt chốt đợt xuất.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, GetSafeErrorMessage(ex, "Không thể duyệt chốt đợt xuất."));
        }
    }

    public async Task<(bool Success, string Message)> CloseRemainingSalesDemandAsync(long outboundOrderId, long userId, string reason)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 500)
            return (false, "Lý do đóng phần nhu cầu còn lại phải có từ 10 đến 500 ký tự.");
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var actor = await _context.Users.Include(value => value.Role).FirstOrDefaultAsync(value => value.UserId == userId);
            if (actor?.Status != "ACTIVE" || actor.Role?.RoleCode is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN"))
                return (false, "Chỉ Quản lý kho được đóng phần nhu cầu còn lại của SO.");
            var order = await _context.OutboundOrders
                .Include(value => value.OutboundOrderItems)
                .Include(value => value.SalesOrder).ThenInclude(value => value!.SalesOrderDetails)
                .FirstOrDefaultAsync(value => value.OutboundOrderId == outboundOrderId);
            if (order?.SalesOrder == null || order.SourceType != "SALES_ORDER")
                return (false, "Phiếu không tham chiếu SO.");
            if (order.Status != "COMPLETED" ||
                !order.SalesOrder.SalesOrderDetails.Any(detail => detail.FulfilledQuantity < detail.OrderedQuantity))
                return (false, "Chỉ được đóng phần chưa giao của SO sau khi một đợt xuất đã hoàn tất.");
            if (order.SalesOrder.Status == "CLOSED")
                return (false, "SO đã được đóng trước đó.");
            var hasOtherActiveOutbound = await _context.OutboundOrders.AnyAsync(value =>
                value.OutboundOrderId != order.OutboundOrderId && value.SalesOrderId == order.SalesOrderId &&
                (value.Status == "DRAFT" || value.Status == "ASSIGNED" || value.Status == "IN_PROGRESS" || value.Status == "PENDING_APPROVAL"));
            if (hasOtherActiveOutbound)
                return (false, "SO còn phiếu xuất khác đang hoạt động. Hãy hoàn tất hoặc hủy các phiếu đó trước khi đóng nhu cầu còn lại.");

            var reservations = await _context.InventoryReservations
                .Where(value => value.SalesOrderDetail != null &&
                                value.SalesOrderDetail.SalesOrderId == order.SalesOrderId &&
                                (value.Status == "ACTIVE" || value.Status == "PARTIALLY_CONSUMED"))
                .ToListAsync();
            foreach (var reservation in reservations)
            {
                var remaining = reservation.ReservedQuantity - reservation.ConsumedQuantity;
                if (remaining > 0)
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "RELEASE_RESERVATION",
                        ProductId = reservation.ProductId,
                        StorageLocationId = reservation.StorageLocationId,
                        ProductLotId = reservation.ProductLotId,
                        OnHandDelta = 0,
                        ReservedDelta = -remaining,
                        InventoryReservationId = reservation.InventoryReservationId,
                        PerformedByUserId = userId,
                        TransactionAt = DateTime.UtcNow,
                        Notes = $"Giải phóng tồn do Quản lý kho đóng phần còn lại của {order.SalesOrder.SalesOrderNumber}."
                    });
                reservation.Status = "RELEASED";
                reservation.ReleasedAt = DateTime.UtcNow;
            }
            foreach (var detail in order.SalesOrder.SalesOrderDetails) detail.ReservedQuantity = 0;
            order.SalesOrder.Status = "CLOSED";
            order.SalesOrder.UpdatedAt = DateTime.UtcNow;
            order.SalesOrder.Notes = string.IsNullOrWhiteSpace(order.SalesOrder.Notes)
                ? $"Đóng phần chưa giao: {reason}"
                : $"{order.SalesOrder.Notes}\nĐóng phần chưa giao: {reason}";
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = userId,
                ActionType = "CLOSE_SALES_REMAINDER",
                EntityName = AuditEntities.OutboundOrder,
                EntityId = order.OutboundOrderNumber,
                NewValues = new { SalesOrderStatus = "CLOSED", Reason = reason, ReleasedReservationCount = reservations.Count }
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (true, "Đã đóng phần nhu cầu còn lại của SO và giải phóng tồn giữ tương ứng.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, GetSafeErrorMessage(ex, "Không thể đóng phần nhu cầu còn lại của SO."));
        }
    }

    private async Task<List<Allocation>> BuildSalesAllocationsAsync(long salesOrderId, long productId, decimal quantity, bool requireEnough = true)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new ArgumentException("Không tìm thấy sản phẩm.");
        var query = _context.InventoryReservations
            .Where(r => r.SalesOrderDetail != null && r.SalesOrderDetail.SalesOrderId == salesOrderId && r.ProductId == productId &&
                        (r.ProductLot.ExpiryDate == null || r.ProductLot.ExpiryDate >= DateOnly.FromDateTime(DateTime.UtcNow)) &&
                        (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"));
        var rows = await query
            .Select(r => new Allocation(
                r.StorageLocationId,
                r.ProductLotId,
                r.ReservedQuantity - r.ConsumedQuantity,
                r.ProductLot.ExpiryDate,
                r.ProductLot.FirstReceivedDate,
                r.StorageLocation.LocationCode))
            .ToListAsync();
        return TakeAllocations(OrderAllocations(rows, product.RotationMethod), quantity, requireEnough);
    }

    private async Task<List<PurchaseReturnSourceLocationDto>> GetReturnLocationDetailsAsync(long poId, long warehouseId, long productId)
    {
        var eligible = await GetPurchaseReturnSourceRowsAsync(poId, warehouseId, productId);
        var lotIds = eligible.Select(r => r.ProductLotId).Distinct().ToList();
        var locations = await _context.Inventories.AsNoTracking().Include(i => i.ProductLot)
            .Include(i => i.StorageLocation).ThenInclude(l => l.StorageRack).ThenInclude(r => r!.WarehouseZone)
            .Where(i => i.ProductId == productId && lotIds.Contains(i.ProductLotId) && i.StorageLocation.WarehouseId == warehouseId).ToListAsync();
        return eligible.Select(row =>
        {
            var stock = locations.Single(i => i.StorageLocationId == row.StorageLocationId && i.ProductLotId == row.ProductLotId);
            return new PurchaseReturnSourceLocationDto
            {
                StorageLocationId = row.StorageLocationId, ProductLotId = row.ProductLotId,
                LocationPath = FormatLocationPath(stock.StorageLocation), FirstReceivedDate = row.FirstReceivedDate,
                ExpiryDate = row.ExpiryDate, StoredQuantity = stock.OnHandQuantity, HeldQuantity = stock.ReservedQuantity,
                ReturnableQuantity = row.Quantity
            };
        }).ToList();
    }
    private async Task<List<Allocation>> BuildPurchaseReturnSourceAllocationsAsync(
        long purchaseOrderId,
        long warehouseId,
        long productId,
        decimal quantity)
    {
        var rows = await GetPurchaseReturnSourceRowsAsync(purchaseOrderId, warehouseId, productId);
        var product = await _context.Products.FindAsync(productId) ?? throw new ArgumentException("Không tìm thấy sản phẩm.");
        return TakeAllocations(OrderAllocations(rows, product.RotationMethod), quantity);
    }

    private async Task<List<Allocation>> GetPurchaseReturnSourceRowsAsync(
        long purchaseOrderId,
        long warehouseId,
        long productId)
    {
        var product = await _context.Products.FindAsync(productId) ?? throw new ArgumentException("Không tìm thấy sản phẩm.");
        if (!IsActive(product.Status)) throw new InvalidOperationException("Sản phẩm đang ngừng hoạt động.");
        var sourceLotIds = _context.InventoryTransactions
            .Where(transaction => transaction.TransactionType == "INBOUND" &&
                                  transaction.ProductId == productId &&
                                  transaction.InboundOrderDetail != null &&
                                  transaction.InboundOrderDetail.InboundOrderItem.InboundOrder.PurchaseOrderId == purchaseOrderId)
            .Select(transaction => transaction.ProductLotId)
            .Distinct();
        var query = _context.Inventories.Where(i => i.ProductId == productId && i.StorageLocation.WarehouseId == warehouseId &&
            sourceLotIds.Contains(i.ProductLotId) &&
            i.StorageLocation.IsPickable &&
            (i.StorageLocation.StorageRack == null ||
             (i.StorageLocation.StorageRack.Status == "ACTIVE" &&
              i.StorageLocation.StorageRack.WarehouseZone.Status == "ACTIVE" &&
              (i.StorageLocation.StorageRack.WarehouseZone.ProductGroupId == null ||
               i.StorageLocation.StorageRack.WarehouseZone.ProductGroupId == product.ProductGroupId))) &&
            (i.StorageLocation.Status == "ACTIVE" || i.StorageLocation.Status == "AVAILABLE" || i.StorageLocation.Status == "OCCUPIED") &&
            i.ProductLot.Status == "AVAILABLE" &&
            (i.ProductLot.ExpiryDate == null || i.ProductLot.ExpiryDate >= DateOnly.FromDateTime(DateTime.UtcNow)) &&
            i.OnHandQuantity - i.ReservedQuantity > 0);
        var rows = await query.Select(i => new Allocation(
            i.StorageLocationId,
            i.ProductLotId,
            i.OnHandQuantity - i.ReservedQuantity,
            i.ProductLot.ExpiryDate,
            i.ProductLot.FirstReceivedDate,
            i.StorageLocation.LocationCode)).ToListAsync();
        return rows;
    }

    private static List<Allocation> TakeAllocations(IEnumerable<Allocation> rows, decimal quantity, bool requireEnough = true)
    {
        var availableRows = rows.Where(r => r.Quantity > 0).ToList();
        if (requireEnough && availableRows.Sum(r => r.Quantity) < quantity)
            throw new InvalidOperationException("Tồn khả dụng/giữ tồn không đủ để tiếp tục phiếu xuất.");

        return availableRows;
    }

    private static Dictionary<(long StorageLocationId, long ProductLotId), decimal> BuildExpectedAllocationMap(
        IEnumerable<AvailableStockLocationDto> routes,
        decimal quantity)
    {
        var remaining = quantity;
        var result = new Dictionary<(long, long), decimal>();
        foreach (var route in routes)
        {
            if (remaining <= 0) break;
            var take = Math.Min(remaining, route.AvailableQuantity);
            if (take > 0) result[(route.StorageLocationId, route.ProductLotId)] = take;
            remaining -= take;
        }
        return result;
    }

    private async Task<List<AvailableStockLocationDto>> GetAvailableAllocationsAsync(OutboundOrder order, OutboundOrderItem item)
    {
        if (item.RequestedQuantity <= item.IssuedQuantity) return [];
        var ownReservations = await _context.InventoryReservations.AsNoTracking()
            .Where(r => r.ProductId == item.ProductId && (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED") &&
                (order.SourceType == "SALES_ORDER"
                    ? r.SalesOrderDetail != null && r.SalesOrderDetail.SalesOrderId == order.SalesOrderId
                    : r.OutboundOrderItemId == item.OutboundOrderItemId))
            .ToListAsync();
        var ownBudget = ownReservations.Sum(r => r.ReservedQuantity - r.ConsumedQuantity);
        var query = _context.Inventories.AsNoTracking()
            .Include(i => i.StorageLocation).ThenInclude(l => l.StorageRack).ThenInclude(r => r!.WarehouseZone)
            .Include(i => i.ProductLot)
            .Where(i => i.ProductId == item.ProductId && i.StorageLocation.WarehouseId == order.WarehouseId &&
                i.OnHandQuantity > 0 && i.StorageLocation.LocationType == "BIN" && i.StorageLocation.IsPickable &&
                (i.StorageLocation.Status == "AVAILABLE" || i.StorageLocation.Status == "ACTIVE" || i.StorageLocation.Status == "OCCUPIED") &&
                i.StorageLocation.StorageRack != null && i.StorageLocation.StorageRack.Status == "ACTIVE" &&
                i.StorageLocation.StorageRack.WarehouseZone.Status == "ACTIVE" &&
                i.StorageLocation.StorageRack.WarehouseZone.ProductGroupId == item.Product.ProductGroupId &&
                i.ProductLot.Status == "AVAILABLE" &&
                (i.ProductLot.ExpiryDate == null || i.ProductLot.ExpiryDate >= DateOnly.FromDateTime(DateTime.UtcNow)));
        if (order.SourceType == "PURCHASE_RETURN")
        {
            var sourceLots = _context.InventoryTransactions.Where(t => t.TransactionType == "INBOUND" &&
                t.ProductId == item.ProductId && t.InboundOrderDetail != null &&
                t.InboundOrderDetail.InboundOrderItem.InboundOrder.PurchaseOrderId == order.PurchaseOrderId)
                .Select(t => t.ProductLotId).Distinct();
            query = query.Where(i => sourceLots.Contains(i.ProductLotId));
        }
        var rows = await query.ToListAsync();
        var result = rows.Select(i =>
        {
            var owned = ownReservations.Where(r => r.StorageLocationId == i.StorageLocationId && r.ProductLotId == i.ProductLotId).ToList();
            var ownAtLocation = owned.Sum(r => r.ReservedQuantity - r.ConsumedQuantity);
            // Stock held by other SO/Outbound/Transfer is never available to this task.
            var available = Math.Min(ownBudget, Math.Min(i.OnHandQuantity, i.OnHandQuantity - i.ReservedQuantity + ownAtLocation));
            return new AvailableStockLocationDto
            {
                StorageLocationId = i.StorageLocationId, LocationCode = i.StorageLocation.LocationCode,
                LocationPath = FormatLocationPath(i.StorageLocation), ProductLotId = i.ProductLotId,
                LotNumber = i.ProductLot.LotNumber, FirstReceivedDate = i.ProductLot.FirstReceivedDate,
                ExpiryDate = i.ProductLot.ExpiryDate, InventoryReservationId = owned.FirstOrDefault()?.InventoryReservationId,
                AvailableQuantity = Math.Max(0, available), StoredQuantity = i.OnHandQuantity, HeldQuantity = i.ReservedQuantity
            };
        }).Where(r => r.AvailableQuantity > 0);
        return item.Product.RotationMethod == "FEFO"
            ? result.OrderBy(r => r.ExpiryDate == null).ThenBy(r => r.ExpiryDate).ThenBy(r => r.FirstReceivedDate)
                .ThenBy(r => r.LocationCode, NaturalLocationCodeComparer.Instance).ToList()
            : result.OrderBy(r => r.FirstReceivedDate).ThenBy(r => r.LocationCode, NaturalLocationCodeComparer.Instance).ToList();
    }

    private async Task<long> EnsurePickReservationAsync(OutboundOrder order, OutboundOrderItem item, ExecutePickItemRequest request, long userId)
    {
        var owned = await _context.InventoryReservations
            .Where(r => r.ProductId == item.ProductId && (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED") &&
                (order.SourceType == "SALES_ORDER"
                    ? r.SalesOrderDetail != null && r.SalesOrderDetail.SalesOrderId == order.SalesOrderId
                    : r.OutboundOrderItemId == item.OutboundOrderItemId))
            .OrderBy(r => r.InventoryReservationId).ToListAsync();
        var atSource = owned.FirstOrDefault(r => r.StorageLocationId == request.StorageLocationId && r.ProductLotId == request.ProductLotId);
        if (request.InventoryReservationId.HasValue)
        {
            // Another pick in this same batch may have rebalanced this reservation.
            // Validate ownership/source, not the now-stale ACTIVE identifier from the browser.
            var supplied = await _context.InventoryReservations.AsNoTracking().FirstOrDefaultAsync(r =>
                r.InventoryReservationId == request.InventoryReservationId && r.ProductId == item.ProductId &&
                r.StorageLocationId == request.StorageLocationId && r.ProductLotId == request.ProductLotId &&
                (order.SourceType == "SALES_ORDER"
                    ? r.SalesOrderDetail != null && r.SalesOrderDetail.SalesOrderId == order.SalesOrderId
                    : r.OutboundOrderItemId == item.OutboundOrderItemId));
            if (supplied == null) throw new InvalidOperationException("Bản ghi giữ tồn không thuộc đơn và vị trí này; tải lại phiếu.");
        }
        if (atSource != null && atSource.ReservedQuantity - atSource.ConsumedQuantity >= request.PickQuantity)
            return atSource.InventoryReservationId;
        // Rebalance only this order's reserve, within the surrounding Serializable batch transaction.
        // Release the target's partial reserve too, then establish one complete reservation for this pick.
        var needed = request.PickQuantity;
        var released = 0m;
        foreach (var r in owned.OrderBy(r => r == atSource ? 0 : 1))
        {
            var remaining = r.ReservedQuantity - r.ConsumedQuantity;
            var take = Math.Min(needed - released, remaining);
            if (take <= 0) continue;
            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                TransactionType = "RELEASE_RESERVATION", ProductId = item.ProductId,
                StorageLocationId = r.StorageLocationId, ProductLotId = r.ProductLotId,
                OnHandDelta = 0, ReservedDelta = -remaining, InventoryReservationId = r.InventoryReservationId,
                PerformedByUserId = userId, TransactionAt = DateTime.UtcNow,
                Notes = $"Đổi vị trí lấy hàng trong {order.OutboundOrderNumber}."
            });
            // Close the old reservation without inventing consumed stock or editing its historical quantity.
            r.Status = "RELEASED";
            r.ReleasedAt = DateTime.UtcNow;
            if (remaining > take)
            {
                var retained = new InventoryReservation
                {
                    SalesOrderDetailId = r.SalesOrderDetailId, OutboundOrderItemId = r.OutboundOrderItemId,
                    ProductId = r.ProductId, ProductLotId = r.ProductLotId, StorageLocationId = r.StorageLocationId,
                    ReservedQuantity = remaining - take, ConsumedQuantity = 0, Status = "ACTIVE",
                    ReservedByUserId = userId, ReservedAt = DateTime.UtcNow
                };
                _context.InventoryReservations.Add(retained);
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    TransactionType = "RESERVE", ProductId = r.ProductId, ProductLotId = r.ProductLotId,
                    StorageLocationId = r.StorageLocationId, OnHandDelta = 0, ReservedDelta = remaining - take,
                    InventoryReservation = retained, PerformedByUserId = userId, TransactionAt = DateTime.UtcNow,
                    Notes = $"Giữ phần chưa lấy của {order.OutboundOrderNumber}."
                });
            }
            released += take;
            if (released >= needed) break;
        }
        if (released < needed) throw new InvalidOperationException("Giữ tồn của đơn không đủ để đổi vị trí lấy.");
        await _context.SaveChangesAsync();
        var inventory = await _context.Inventories.AsNoTracking().SingleOrDefaultAsync(i => i.ProductId == item.ProductId &&
            i.StorageLocationId == request.StorageLocationId && i.ProductLotId == request.ProductLotId);
        if (inventory == null || inventory.OnHandQuantity - inventory.ReservedQuantity < needed)
            throw new InvalidOperationException("Vị trí tự chọn không còn đủ tồn khả dụng; hàng đang được giữ bởi đơn khác.");
        var reservation = new InventoryReservation
        {
            SalesOrderDetailId = order.SourceType == "SALES_ORDER" ? order.SalesOrder!.SalesOrderDetails.Single(d => d.ProductId == item.ProductId).SalesOrderDetailId : null,
            OutboundOrderItemId = order.SourceType == "PURCHASE_RETURN" ? item.OutboundOrderItemId : null,
            ProductId = item.ProductId, ProductLotId = request.ProductLotId, StorageLocationId = request.StorageLocationId,
            ReservedQuantity = needed, ConsumedQuantity = 0, Status = "ACTIVE", ReservedByUserId = userId, ReservedAt = DateTime.UtcNow
        };
        _context.InventoryReservations.Add(reservation);
        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            TransactionType = "RESERVE", ProductId = item.ProductId, ProductLotId = request.ProductLotId,
            StorageLocationId = request.StorageLocationId, OnHandDelta = 0, ReservedDelta = needed,
            InventoryReservation = reservation, PerformedByUserId = userId, TransactionAt = DateTime.UtcNow,
            Notes = $"Giữ tồn tại vị trí lấy mới trong {order.OutboundOrderNumber}."
        });
        await _context.SaveChangesAsync();
        return reservation.InventoryReservationId;
    }

    private async Task<List<Allocation>> BuildReturnReservationAllocationsAsync(long outboundOrderItemId, decimal quantity, bool requireEnough = true)
    {
        var rows = await _context.InventoryReservations
            .Where(value => value.OutboundOrderItemId == outboundOrderItemId &&
                            (value.Status == "ACTIVE" || value.Status == "PARTIALLY_CONSUMED") &&
                            (value.ProductLot.ExpiryDate == null || value.ProductLot.ExpiryDate >= DateOnly.FromDateTime(DateTime.UtcNow)))
            .Select(value => new Allocation(
                value.StorageLocationId,
                value.ProductLotId,
                value.ReservedQuantity - value.ConsumedQuantity,
                value.ProductLot.ExpiryDate,
                value.ProductLot.FirstReceivedDate,
                value.StorageLocation.LocationCode))
            .ToListAsync();
        var rotationMethod = await _context.OutboundOrderItems
            .Where(value => value.OutboundOrderItemId == outboundOrderItemId)
            .Select(value => value.Product.RotationMethod)
            .SingleAsync();
        return TakeAllocations(OrderAllocations(rows, rotationMethod), quantity, requireEnough);
    }

    private async Task ReleaseReturnReservationsAsync(OutboundOrder order, long userId, string note)
    {
        var itemIds = order.OutboundOrderItems.Select(value => value.OutboundOrderItemId).ToList();
        var reservations = await _context.InventoryReservations
            .Where(value => value.OutboundOrderItemId.HasValue && itemIds.Contains(value.OutboundOrderItemId.Value) &&
                            (value.Status == "ACTIVE" || value.Status == "PARTIALLY_CONSUMED"))
            .ToListAsync();
        foreach (var reservation in reservations)
        {
            var remaining = reservation.ReservedQuantity - reservation.ConsumedQuantity;
            if (remaining > 0)
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    TransactionType = "RELEASE_RESERVATION",
                    ProductId = reservation.ProductId,
                    StorageLocationId = reservation.StorageLocationId,
                    ProductLotId = reservation.ProductLotId,
                    OnHandDelta = 0,
                    ReservedDelta = -remaining,
                    InventoryReservationId = reservation.InventoryReservationId,
                    PerformedByUserId = userId,
                    TransactionAt = DateTime.UtcNow,
                    Notes = note
                });
            reservation.Status = "RELEASED";
            reservation.ReleasedAt = DateTime.UtcNow;
        }
    }

    private static OutboundOrderListDto MapList(OutboundOrder o) => new()
    {
        OutboundOrderId = o.OutboundOrderId, OutboundOrderNumber = o.OutboundOrderNumber, SourceType = o.SourceType,
        SalesOrderId = o.SalesOrderId, PurchaseOrderId = o.PurchaseOrderId,
        SalesOrderNumber = o.SalesOrder?.SalesOrderNumber, PurchaseOrderNumber = o.PurchaseOrder?.PurchaseOrderNumber,
        SourceReference = GetSourceReference(o), PartnerName = GetPartnerName(o), CustomerName = GetPartnerName(o),
        WarehouseId = o.WarehouseId, WarehouseName = o.Warehouse?.WarehouseName ?? string.Empty,
        Status = NormalizeOutboundStatus(o.Status), LineCount = o.OutboundOrderItems.Count, TotalRequestedQuantity = o.OutboundOrderItems.Sum(i => i.RequestedQuantity),
        TotalIssuedQuantity = o.OutboundOrderItems.Sum(i => i.IssuedQuantity), ExpectedIssueDate = o.ExpectedIssueDate, CreatedAt = o.CreatedAt
    };

    private static OutboundOrderDetailDto MapDetail(OutboundOrder o) => new()
    {
        OutboundOrderId = o.OutboundOrderId, OutboundOrderNumber = o.OutboundOrderNumber, SourceType = o.SourceType,
        SalesOrderId = o.SalesOrderId, PurchaseOrderId = o.PurchaseOrderId,
        SalesOrderNumber = o.SalesOrder?.SalesOrderNumber, PurchaseOrderNumber = o.PurchaseOrder?.PurchaseOrderNumber,
        SourceReference = GetSourceReference(o), PartnerName = GetPartnerName(o), CustomerName = GetPartnerName(o),
        WarehouseId = o.WarehouseId, WarehouseName = o.Warehouse?.WarehouseName ?? string.Empty,
        ExpectedIssueDate = o.ExpectedIssueDate, AssignedToUserId = o.AssignedToUserId,
        AssignedToUserName = o.AssignedToUser?.FullName ?? o.AssignedToUser?.Username ?? "Chưa phân công",
        CreatedByUserId = o.CreatedByUserId,
        CreatedByUserName = o.CreatedByUser?.FullName ?? o.CreatedByUser?.Username,
        ApprovedByUserId = o.ApprovedByUserId,
        ApprovedByUserName = o.ApprovedByUser?.FullName ?? o.ApprovedByUser?.Username,
        ApprovedAt = o.ApprovedAt,
        CompletionType = o.CompletionType,
        CanCloseSalesRemainder = o.SourceType == "SALES_ORDER" && o.Status == "COMPLETED" &&
            o.SalesOrder != null && o.SalesOrder.Status != "CLOSED" &&
            o.SalesOrder.SalesOrderDetails.Any(detail => detail.FulfilledQuantity < detail.OrderedQuantity),
        CompletionReason = o.CompletionReason,
        HasReferenceRemainder = o.SourceType == "SALES_ORDER"
            ? o.SalesOrder?.SalesOrderDetails.Any(d => d.FulfilledQuantity < d.OrderedQuantity) == true
            : o.OutboundOrderItems.Any(i => i.IssuedQuantity < i.RequestedQuantity),
        CompletionReviewedByUserId = o.ConfirmedByUserId,
        CompletionReviewedByUserName = o.ConfirmedByUser?.FullName ?? o.ConfirmedByUser?.Username,
        Remainders = o.SourceType == "SALES_ORDER" && o.SalesOrder != null
            ? o.SalesOrder.SalesOrderDetails.Where(d => d.OrderedQuantity > d.FulfilledQuantity).Select(d => new OutboundRemainderDto
            {
                ProductName = d.Product?.ProductName ?? $"Vật tư #{d.ProductId}", UnitName = d.Product?.UnitOfMeasure?.UnitCode ?? "",
                QuantityScale = d.Product?.UnitOfMeasure?.QuantityScale ?? 0, PlannedQuantity = d.OrderedQuantity,
                DeliveredQuantity = d.FulfilledQuantity, RemainingQuantity = d.OrderedQuantity - d.FulfilledQuantity
            }).ToList()
            : o.OutboundOrderItems.Where(i => i.RequestedQuantity > i.IssuedQuantity).Select(i => new OutboundRemainderDto
            {
                ProductName = i.Product?.ProductName ?? "", UnitName = i.Product?.UnitOfMeasure?.UnitCode ?? "",
                QuantityScale = i.Product?.UnitOfMeasure?.QuantityScale ?? 0, PlannedQuantity = i.RequestedQuantity,
                DeliveredQuantity = i.IssuedQuantity, RemainingQuantity = i.RequestedQuantity - i.IssuedQuantity
            }).ToList(),
        CompletedAt = o.ConfirmedAt,
        CancellationReason = o.CancellationReason,
        Status = NormalizeOutboundStatus(o.Status), Notes = o.Notes, CreatedAt = o.CreatedAt,
        Items = o.OutboundOrderItems.Select(i => new OutboundOrderItemDto
        {
            OutboundOrderItemId = i.OutboundOrderItemId, ProductId = i.ProductId,
            ProductCode = i.Product?.ProductCode ?? string.Empty, ProductName = i.Product?.ProductName ?? string.Empty,
            UnitOfMeasure = i.Product?.UnitOfMeasure?.UnitCode ?? string.Empty,
            QuantityScale = i.Product?.UnitOfMeasure?.QuantityScale ?? 0,
            TrackLot = i.Product?.TrackLot ?? false,
            RequestedQuantity = i.RequestedQuantity, IssuedQuantity = i.IssuedQuantity, Notes = i.Notes,
            PickedDetails = i.OutboundOrderDetails.OrderBy(d => d.RecordedAt).Select(d => new OutboundPickedDetailDto
            {
                OutboundOrderDetailId = d.OutboundOrderDetailId,
                LocationCode = FormatLocationPath(d.StorageLocation),
                LotNumber = d.ProductLot?.LotNumber ?? string.Empty,
                FirstReceivedDate = d.ProductLot?.FirstReceivedDate ?? default,
                ExpiryDate = d.ProductLot?.ExpiryDate,
                IssuedQuantity = d.IssuedQuantity,
                RecordedByUserName = d.RecordedByUser?.FullName ?? d.RecordedByUser?.Username ?? string.Empty,
                RecordedAt = d.RecordedAt
            }).ToList()
        }).ToList()
    };

    private static string FormatLocationPath(StorageLocation? location)
    {
        if (location == null) return string.Empty;
        return string.Join(" / ", new[]
        {
            location.StorageRack?.WarehouseZone?.ZoneCode,
            location.StorageRack?.RackCode,
            location.LocationCode
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string GetSourceReference(OutboundOrder order) => order.SourceType == "PURCHASE_RETURN"
        ? order.PurchaseOrder?.PurchaseOrderNumber ?? string.Empty : order.SalesOrder?.SalesOrderNumber ?? string.Empty;
    private static string GetPartnerName(OutboundOrder order) => order.SourceType == "PURCHASE_RETURN"
        ? order.PurchaseOrder?.Supplier?.SupplierName ?? string.Empty : order.SalesOrder?.Customer?.CustomerName ?? string.Empty;
    private static string NormalizeOutboundStatus(string status) => status switch
    { "ASSIGNED" => "READY", "IN_PROGRESS" => "ISSUING", "COMPLETED" => "ISSUED", _ => status };
    private static string NormalizePurchaseStatus(string? status) => (status ?? string.Empty).Trim().ToUpperInvariant() switch
    { "PARTIALLYRECEIVED" or "PENDING_RECEIPT_REVIEW" or "PENDING_REMAINDER_CONFIRMATION" => "PARTIALLY_RECEIVED",
      "COMPLETED" or "CLOSED" => "RECEIVED", var value => value };
    private static bool IsActive(string? status) => status is "ACTIVE" or "AVAILABLE" or "OCCUPIED";

    private static string GetSafeErrorMessage(Exception exception, string fallback)
    {
        if (exception is ArgumentException or InvalidOperationException)
            return exception.Message;
        return fallback;
    }

    private async Task EnsureWarehouseStaffAssigneeAsync(long? assignedToUserId, long? excludeOutboundOrderId = null)
    {
        if (!assignedToUserId.HasValue) return;
        var valid = await _context.Users.AnyAsync(u => u.UserId == assignedToUserId.Value &&
            u.Status == "ACTIVE" && u.Role.RoleCode == "WAREHOUSE_STAFF" &&
            !u.InboundOrderAssignedToUsers.Any(o =>
                o.Status != "CANCELLED" && o.Status != "PUTAWAY_COMPLETED" &&
                (o.Status != "COMPLETED" ||
                 !o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                     .Any(d => d.ConditionStatus == "GOOD") ||
                 o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                     .Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null))) &&
            !u.OutboundOrderAssignedToUsers.Any(o =>
                o.OutboundOrderId != excludeOutboundOrderId &&
                o.Status != "CANCELLED" && o.Status != "COMPLETED"));
        if (!valid)
            throw new ArgumentException("Nhân viên không thuộc vai trò Nhân viên kho, đang bị khóa hoặc đang phụ trách một phiếu nhập/xuất khác.");
    }
    private static IEnumerable<Allocation> OrderAllocations(IEnumerable<Allocation> rows, string? rotationMethod)
    {
        var ordered = string.Equals(rotationMethod, "FEFO", StringComparison.OrdinalIgnoreCase)
            ? rows.OrderBy(value => value.ExpiryDate == null)
                .ThenBy(value => value.ExpiryDate)
                .ThenBy(value => value.FirstReceivedDate)
            : rows.OrderBy(value => value.FirstReceivedDate);
        return ordered.ThenBy(value => value.LocationCode, NaturalLocationCodeComparer.Instance);
    }

    private sealed class NaturalLocationCodeComparer : IComparer<string>
    {
        public static NaturalLocationCodeComparer Instance { get; } = new();

        public int Compare(string? left, string? right)
        {
            var leftParts = System.Text.RegularExpressions.Regex.Split(left ?? string.Empty, "(\\d+)");
            var rightParts = System.Text.RegularExpressions.Regex.Split(right ?? string.Empty, "(\\d+)");
            for (var index = 0; index < Math.Min(leftParts.Length, rightParts.Length); index++)
            {
                int comparison;
                if (long.TryParse(leftParts[index], out var leftNumber) && long.TryParse(rightParts[index], out var rightNumber))
                    comparison = leftNumber.CompareTo(rightNumber);
                else
                    comparison = string.Compare(leftParts[index], rightParts[index], StringComparison.OrdinalIgnoreCase);
                if (comparison != 0) return comparison;
            }
            return leftParts.Length.CompareTo(rightParts.Length);
        }
    }

    private sealed record Allocation(
        long StorageLocationId,
        long ProductLotId,
        decimal Quantity,
        DateOnly? ExpiryDate = null,
        DateOnly FirstReceivedDate = default,
        string LocationCode = "");
}
