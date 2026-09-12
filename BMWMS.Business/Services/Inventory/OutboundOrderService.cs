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
                        p.Status == "COMPLETED" || p.Status == "CLOSED")
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
            var remaining = Math.Max(0, received - returned);
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
                RemainingQuantity = remaining
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
            var requiredRole = request.SourceType == "SALES_ORDER" ? "SALES_STAFF" : "PURCHASING_STAFF";
            if (creator.Role?.RoleCode != requiredRole)
                throw new InvalidOperationException(request.SourceType == "SALES_ORDER"
                    ? "Chỉ nhân viên bán hàng được tạo phiếu xuất theo SO."
                    : "Chỉ nhân viên mua hàng được tạo phiếu trả nhà cung cấp theo PO.");

            // Phiếu mới luôn là Nháp. Warehouse Manager là người duyệt và phân công ở bước sau.
            request.AssignedToUserId = null;
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

                    var missingReservation = remainingToIssue - activeReserved;
                    if (missingReservation > 0)
                    {
                        var reserved = await _inventoryRepo.ReserveStockForOrderAsync(
                            detail.ProductId,
                            missingReservation,
                            detail.SalesOrderDetailId,
                            createdByUserId,
                            detail.Product.RotationMethod);
                        if (!reserved)
                            throw new InvalidOperationException(
                                $"SO {salesOrder.SalesOrderNumber} chưa đủ tồn khả dụng để giữ cho sản phẩm {detail.Product.ProductCode}.");
                        activeReserved += missingReservation;
                    }

                    detail.ReservedQuantity = activeReserved;
                    maximumByProduct[detail.ProductId] = Math.Max(0, remainingToIssue - activePlanned);
                }

                // Cần ghi các reservation bù trước khi dựng phân bổ lot/bin cho phiếu xuất.
                await _context.SaveChangesAsync();
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
                AssignedToUserId = null,
                Notes = request.Notes,
                Status = "DRAFT",
                CreatedByUserId = createdByUserId,
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
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = createdByUserId,
                ActionType = "CREATE_OUTBOUND",
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
        if (order.Status is not ("DRAFT" or "ASSIGNED") || order.OutboundOrderItems.Any(i => i.OutboundOrderDetails.Count > 0))
            return (false, "Chỉ được hủy phiếu Nháp/Sẵn sàng chưa phát sinh xuất hàng.");

        var actor = await _context.Users.Include(value => value.Role).FirstOrDefaultAsync(value => value.UserId == userId);
        var role = actor?.Role?.RoleCode;
        var canCancelDraft = order.Status == "DRAFT" && order.CreatedByUserId == userId &&
            ((order.SourceType == "SALES_ORDER" && role == "SALES_STAFF") ||
             (order.SourceType == "PURCHASE_RETURN" && role == "PURCHASING_STAFF"));
        var canCancelAssigned = order.Status == "ASSIGNED" && role is "WAREHOUSE_MANAGER" or "SYSTEM_ADMIN";
        if (!canCancelDraft && !canCancelAssigned)
            return (false, order.Status == "DRAFT"
                ? "Chỉ nhân viên phụ trách đúng luồng được hủy phiếu Nháp."
                : "Chỉ Quản lý kho được hủy phiếu đã duyệt nhưng chưa bắt đầu.");

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
                var routes = await GetAvailableAllocationsAsync(order, item);
                var expected = BuildExpectedAllocationMap(routes, requestedTotal);
                var actual = itemRequests
                    .GroupBy(value => (value.StorageLocationId, value.ProductLotId))
                    .ToDictionary(group => group.Key, group => group.Sum(value => value.PickQuantity));
                var followsSuggestion = expected.Count == actual.Count && expected.All(pair =>
                    actual.TryGetValue(pair.Key, out var actualQuantity) && actualQuantity == pair.Value);
                if (!followsSuggestion && !itemRequests.Any(value => (value.DeviationReason?.Trim().Length ?? 0) is >= 10 and <= 500))
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

                var location = await _context.StorageLocations.FindAsync(request.StorageLocationId);
                if (location == null || location.WarehouseId != order.WarehouseId || !location.IsPickable || !IsActive(location.Status))
                    return (false, "Vị trí nguồn không hoạt động hoặc không cho phép lấy hàng.");
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i =>
                    i.ProductId == item.ProductId && i.StorageLocationId == request.StorageLocationId &&
                    i.ProductLotId == request.ProductLotId);
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
            order.Status = completed ? "COMPLETED" : "IN_PROGRESS";
            if (completed)
            {
                order.ConfirmedByUserId = userId;
                order.ConfirmedAt = DateTime.UtcNow;
                order.CompletionType = "FULL";
                order.CompletionReason = null;
            }
            if (order.SalesOrder != null)
            {
                order.SalesOrder.Status = order.SalesOrder.SalesOrderDetails.All(d => d.FulfilledQuantity >= d.OrderedQuantity)
                    ? "FULFILLED" : "PARTIALLY_FULFILLED";
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
                    Status = completed ? "COMPLETED" : "IN_PROGRESS",
                    Rows = requests.Select(request => new
                    {
                        request.OutboundOrderItemId,
                        request.StorageLocationId,
                        request.ProductLotId,
                        request.PickQuantity,
                        request.DeviationReason
                    })
                }
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (true, completed ? "Đã xuất đủ hàng và hoàn tất đợt giao." : "Đã ghi nhận số lượng thực xuất; phiếu còn hàng chưa giao.");
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

            order.Status = "COMPLETED";
            order.ConfirmedByUserId = userId;
            order.ConfirmedAt = DateTime.UtcNow;
            order.CompletionType = order.OutboundOrderItems.All(item => item.IssuedQuantity >= item.RequestedQuantity)
                ? "FULL" : "PARTIAL";
            order.CompletionReason = order.CompletionType == "PARTIAL" ? reason : null;
            if (order.SourceType == "PURCHASE_RETURN")
                await ReleaseReturnReservationsAsync(order, userId, "Giải phóng phần không trả trong đợt xuất đã hoàn tất.");

            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = userId,
                ActionType = "COMPLETE_OUTBOUND",
                EntityName = AuditEntities.OutboundOrder,
                EntityId = order.OutboundOrderNumber,
                OldValues = new { Status = "IN_PROGRESS" },
                NewValues = new
                {
                    Status = "COMPLETED",
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
                ? "Đã hoàn tất đợt xuất đủ hàng."
                : "Đã hoàn tất đợt xuất theo số lượng thực giao. Phần còn lại của đơn tham chiếu chưa bị đóng.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, GetSafeErrorMessage(ex, "Không thể hoàn tất phiếu xuất."));
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
            if (actor?.Status != "ACTIVE" || actor.Role?.RoleCode != "SALES_STAFF")
                return (false, "Chỉ nhân viên bán hàng được đóng phần nhu cầu còn lại của SO.");
            var order = await _context.OutboundOrders
                .Include(value => value.OutboundOrderItems)
                .Include(value => value.SalesOrder).ThenInclude(value => value!.SalesOrderDetails)
                .FirstOrDefaultAsync(value => value.OutboundOrderId == outboundOrderId);
            if (order?.SalesOrder == null || order.SourceType != "SALES_ORDER")
                return (false, "Phiếu không tham chiếu SO.");
            if (order.Status != "COMPLETED" || order.CompletionType != "PARTIAL")
                return (false, "Chỉ được đóng nhu cầu sau khi một đợt giao thiếu đã hoàn tất.");
            if (order.SalesOrder.Status == "CLOSED")
                return (false, "SO đã được đóng trước đó.");
            var hasOtherActiveOutbound = await _context.OutboundOrders.AnyAsync(value =>
                value.OutboundOrderId != order.OutboundOrderId && value.SalesOrderId == order.SalesOrderId &&
                (value.Status == "DRAFT" || value.Status == "ASSIGNED" || value.Status == "IN_PROGRESS"));
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
                        Notes = $"Giải phóng tồn do Sales đóng phần còn lại của {order.SalesOrder.SalesOrderNumber}."
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

    private async Task<List<Allocation>> BuildSalesAllocationsAsync(long salesOrderId, long productId, decimal quantity)
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
        return TakeAllocations(OrderAllocations(rows, product.RotationMethod), quantity);
    }

    private async Task<List<Allocation>> BuildPurchaseReturnSourceAllocationsAsync(
        long purchaseOrderId,
        long warehouseId,
        long productId,
        decimal quantity)
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
        return TakeAllocations(OrderAllocations(rows, product.RotationMethod), quantity);
    }

    private static List<Allocation> TakeAllocations(IEnumerable<Allocation> rows, decimal quantity)
    {
        var availableRows = rows.Where(r => r.Quantity > 0).ToList();
        if (availableRows.Sum(r => r.Quantity) < quantity)
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
        var remaining = Math.Max(0, item.RequestedQuantity - item.IssuedQuantity);
        if (remaining <= 0) return new List<AvailableStockLocationDto>();

        var routes = order.SourceType == "SALES_ORDER"
            ? await BuildSalesAllocationsAsync(order.SalesOrderId!.Value, item.ProductId, remaining)
            : await BuildReturnReservationAllocationsAsync(item.OutboundOrderItemId, remaining);
        var result = new List<AvailableStockLocationDto>();
        foreach (var route in routes)
        {
            long? reservationId = null;
            var available = route.Quantity;
            var reservation = await _context.InventoryReservations.FirstOrDefaultAsync(r =>
                r.ProductId == item.ProductId && r.StorageLocationId == route.StorageLocationId &&
                r.ProductLotId == route.ProductLotId &&
                (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED") &&
                (order.SourceType == "SALES_ORDER"
                    ? r.SalesOrderDetail != null && r.SalesOrderDetail.SalesOrderId == order.SalesOrderId
                    : r.OutboundOrderItemId == item.OutboundOrderItemId));
            reservationId = reservation?.InventoryReservationId;
            if (reservation != null) available = Math.Min(available, reservation.ReservedQuantity - reservation.ConsumedQuantity);
            var location = await _context.StorageLocations
                .Include(value => value.StorageRack)
                    .ThenInclude(rack => rack!.WarehouseZone)
                .FirstOrDefaultAsync(value => value.StorageLocationId == route.StorageLocationId);
            var productLot = await _context.ProductLots.FindAsync(route.ProductLotId);
            result.Add(new AvailableStockLocationDto
            {
                StorageLocationId = route.StorageLocationId,
                LocationCode = location?.LocationCode ?? string.Empty,
                LocationPath = FormatLocationPath(location),
                ProductLotId = route.ProductLotId,
                LotNumber = productLot?.LotNumber ?? string.Empty,
                FirstReceivedDate = productLot?.FirstReceivedDate ?? default,
                ExpiryDate = productLot?.ExpiryDate,
                InventoryReservationId = reservationId,
                AvailableQuantity = available
            });
        }
        return result;
    }

    private async Task<List<Allocation>> BuildReturnReservationAllocationsAsync(long outboundOrderItemId, decimal quantity)
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
        return TakeAllocations(OrderAllocations(rows, rotationMethod), quantity);
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
        CompletionReason = o.CompletionReason,
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
    { "PARTIALLYRECEIVED" => "PARTIALLY_RECEIVED", "COMPLETED" or "CLOSED" => "RECEIVED", var value => value };
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
