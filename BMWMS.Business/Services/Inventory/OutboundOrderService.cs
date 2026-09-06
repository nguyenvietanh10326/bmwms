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
                    i.ProductId == line.ProductId).SumAsync(i => i.RequestedQuantity);
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
        if (request.Items == null || request.Items.Count == 0 || request.Items.Any(i => i.RequestedQuantity <= 0))
            throw new ArgumentException("Phiếu xuất phải có ít nhất một mặt hàng với số lượng lớn hơn 0.");
        if (request.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
            throw new ArgumentException("Mỗi sản phẩm chỉ được xuất hiện một lần trong phiếu xuất.");

        if (request.IsSubmit && !request.AssignedToUserId.HasValue)
            throw new ArgumentException("Phải phân công nhân viên kho trước khi chuyển phiếu sang trạng thái Sẵn sàng.");

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
            await EnsureWarehouseStaffAssigneeAsync(request.AssignedToUserId);
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
                        .SumAsync(i => i.RequestedQuantity);
                    maximumByProduct[detail.ProductId] = Math.Max(0, received - plannedReturns);
                }
            }

            foreach (var requested in request.Items)
                if (!maximumByProduct.TryGetValue(requested.ProductId, out var maximum) || requested.RequestedQuantity > maximum)
                    throw new ArgumentException($"Sản phẩm ID {requested.ProductId} vượt số lượng có thể xuất ({maximum}).");

            var status = request.AssignedToUserId.HasValue ? "ASSIGNED" : "DRAFT";
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
                Status = status,
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
                    outbound.AssignedToUserId,
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

    public async Task<bool> UpdateStatusAsync(long outboundOrderId, string newStatus)
    {
        var order = await _context.OutboundOrders.FirstOrDefaultAsync(o => o.OutboundOrderId == outboundOrderId);
        if (order == null) return false;
        var normalized = newStatus.Trim().ToUpperInvariant();
        if (order.Status == "DRAFT" && normalized is "READY" or "ASSIGNED")
        {
            if (!order.AssignedToUserId.HasValue)
                throw new InvalidOperationException("Phải phân công nhân viên kho trước khi chuyển phiếu sang trạng thái Sẵn sàng.");
            await EnsureWarehouseStaffAssigneeAsync(order.AssignedToUserId, excludeOutboundOrderId: order.OutboundOrderId);
            order.Status = "ASSIGNED";
            await _context.SaveChangesAsync();
            return true;
        }
        throw new InvalidOperationException("Trạng thái xuất kho chỉ thay đổi qua đúng thao tác xác nhận/lấy hàng.");
    }

    public async Task<bool> CancelOutboundOrderAsync(long outboundOrderId)
    {
        var order = await _context.OutboundOrders
            .Include(o => o.OutboundOrderItems).ThenInclude(i => i.OutboundOrderDetails)
            .FirstOrDefaultAsync(o => o.OutboundOrderId == outboundOrderId);
        if (order == null) return false;
        if (order.Status is not ("DRAFT" or "ASSIGNED") || order.OutboundOrderItems.Any(i => i.OutboundOrderDetails.Count > 0))
            throw new InvalidOperationException("Chỉ được hủy phiếu Nháp/Sẵn sàng chưa phát sinh xuất hàng.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Giữ tồn thuộc vòng đời SO. Hủy phiếu xuất chỉ hủy tác nghiệp lấy hàng;
            // SO vẫn giữ hàng để có thể lập lại một phiếu xuất khác.
            order.Status = "CANCELLED";
            order.CancelledAt = DateTime.UtcNow;
            order.CancelledByUserId = order.CreatedByUserId;
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
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

    public async Task<(bool Success, string Message)> ExecutePickAsync(ExecutePickItemRequest request, long userId)
        => await ExecutePickBatchAsync(new[] { request }, userId);

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
            if (order.Status is not ("ASSIGNED" or "IN_PROGRESS"))
                return (false, "Phiếu không ở trạng thái cho phép lấy/xuất hàng.");
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user?.Status != "ACTIVE" || user.Role?.RoleCode != "WAREHOUSE_STAFF")
                return (false, "Chỉ nhân viên kho đang hoạt động mới được thực hiện lấy/xuất hàng.");
            if (order.AssignedToUserId != userId)
                return (false, "Bạn không được phân công thực hiện phiếu xuất này.");

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

                InventoryReservation? reservation = null;
                var reservedDelta = 0m;
                if (order.SourceType == "SALES_ORDER")
                {
                    if (!request.InventoryReservationId.HasValue || request.InventoryReservationId <= 0)
                        return (false, "Xuất bán phải tham chiếu bản ghi giữ tồn của SO.");
                    reservation = await _context.InventoryReservations.Include(r => r.SalesOrderDetail)
                        .FirstOrDefaultAsync(r => r.InventoryReservationId == request.InventoryReservationId.Value);
                    if (reservation == null || reservation.SalesOrderDetail.SalesOrderId != order.SalesOrderId ||
                        reservation.ProductId != item.ProductId || reservation.StorageLocationId != request.StorageLocationId ||
                        reservation.ProductLotId != request.ProductLotId ||
                        reservation.Status is not ("ACTIVE" or "PARTIALLY_CONSUMED") ||
                        request.PickQuantity > reservation.ReservedQuantity - reservation.ConsumedQuantity ||
                        inventory.ReservedQuantity < request.PickQuantity)
                        return (false, "Giữ tồn không hợp lệ hoặc không còn đủ số lượng.");
                    reservedDelta = -request.PickQuantity;
                }
                else if ((inventory.AvailableQuantity ?? inventory.OnHandQuantity - inventory.ReservedQuantity) < request.PickQuantity)
                    return (false, "Số lượng khả dụng không đủ để trả nhà cung cấp.");

                var detail = new OutboundOrderDetail
                {
                    OutboundOrderId = order.OutboundOrderId,
                    OutboundOrderItemId = item.OutboundOrderItemId,
                    ProductId = item.ProductId,
                    StorageLocationId = request.StorageLocationId,
                    ProductLotId = request.ProductLotId,
                    InventoryReservationId = reservation?.InventoryReservationId,
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
                    InventoryReservationId = reservation?.InventoryReservationId,
                    PerformedByUserId = userId,
                    TransactionAt = DateTime.UtcNow,
                    Notes = $"Xuất kho theo {order.OutboundOrderNumber}."
                };
                detail.InventoryTransaction = ledger;
                _context.OutboundOrderDetails.Add(detail);
                _context.InventoryTransactions.Add(ledger);
                item.IssuedQuantity += request.PickQuantity;
                if (reservation != null)
                {
                    reservation.ConsumedQuantity += request.PickQuantity;
                    reservation.Status = reservation.ConsumedQuantity >= reservation.ReservedQuantity ? "CONSUMED" : "PARTIALLY_CONSUMED";
                    var salesDetail = order.SalesOrder!.SalesOrderDetails.Single(d => d.SalesOrderDetailId == reservation.SalesOrderDetailId);
                    salesDetail.FulfilledQuantity += request.PickQuantity;
                }

                // Persist each route inside the same DB transaction so triggers and
                // subsequent route checks see the latest on-hand/reserved balance.
                await _context.SaveChangesAsync();
            }

            var completed = order.OutboundOrderItems.All(i => i.IssuedQuantity >= i.RequestedQuantity);
            order.Status = completed ? "COMPLETED" : "IN_PROGRESS";
            if (completed) { order.ConfirmedByUserId = userId; order.ConfirmedAt = DateTime.UtcNow; }
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
                EntityId = order.OutboundOrderId.ToString(),
                NewValues = new
                {
                    order.OutboundOrderNumber,
                    Status = completed ? "COMPLETED" : "IN_PROGRESS",
                    Rows = requests.Select(request => new
                    {
                        request.OutboundOrderItemId,
                        request.StorageLocationId,
                        request.ProductLotId,
                        request.PickQuantity
                    })
                }
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (true, completed ? "Đã xuất đủ hàng và hoàn tất phiếu." : "Đã ghi nhận đợt xuất; phiếu còn số lượng phải xử lý.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> CompleteSalesDeliveryEarlyAsync(long outboundOrderId, long userId, string reason)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 500)
            return (false, "Lý do kết thúc sớm phải có từ 10 đến 500 ký tự.");
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var order = await _context.OutboundOrders
                .Include(o => o.OutboundOrderItems)
                .Include(o => o.SalesOrder).ThenInclude(s => s!.SalesOrderDetails)
                .FirstOrDefaultAsync(o => o.OutboundOrderId == outboundOrderId);
            if (order == null) return (false, "Không tìm thấy phiếu xuất.");
            if (order.SourceType != "SALES_ORDER" || order.SalesOrder == null)
                return (false, "Chỉ áp dụng kết thúc sớm cho đợt giao hàng theo SO.");
            if (order.Status != "IN_PROGRESS" || !order.OutboundOrderItems.Any(item => item.IssuedQuantity > 0))
                return (false, "Chỉ được kết thúc sớm sau khi đã ghi nhận ít nhất một số lượng thực giao.");
            if (order.AssignedToUserId != userId)
                return (false, "Bạn không phải nhân viên chịu trách nhiệm cho đợt giao này.");

            var userIsWarehouseStaff = await _context.Users.AnyAsync(user => user.UserId == userId &&
                user.Status == "ACTIVE" && user.Role.RoleCode == "WAREHOUSE_STAFF");
            if (!userIsWarehouseStaff)
                return (false, "Chỉ nhân viên kho đang hoạt động mới được kết thúc đợt giao.");

            var reservations = await _context.InventoryReservations
                .Where(reservation => reservation.SalesOrderDetail.SalesOrderId == order.SalesOrderId &&
                    (reservation.Status == "ACTIVE" || reservation.Status == "PARTIALLY_CONSUMED"))
                .ToListAsync();
            foreach (var reservation in reservations)
            {
                var remaining = reservation.ReservedQuantity - reservation.ConsumedQuantity;
                if (remaining > 0)
                {
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "RELEASE",
                        ProductId = reservation.ProductId,
                        StorageLocationId = reservation.StorageLocationId,
                        ProductLotId = reservation.ProductLotId,
                        OnHandDelta = 0,
                        ReservedDelta = -remaining,
                        InventoryReservationId = reservation.InventoryReservationId,
                        PerformedByUserId = userId,
                        TransactionAt = DateTime.UtcNow,
                        Notes = $"Giải phóng tồn do kết thúc SO {order.SalesOrder.SalesOrderNumber} trước khi giao đủ."
                    });
                }
                reservation.Status = "RELEASED";
                reservation.ReleasedAt = DateTime.UtcNow;
            }

            foreach (var detail in order.SalesOrder.SalesOrderDetails)
                detail.ReservedQuantity = 0;
            order.Status = "COMPLETED";
            order.ConfirmedByUserId = userId;
            order.ConfirmedAt = DateTime.UtcNow;
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? $"Kết thúc theo số thực giao: {reason}"
                : $"{order.Notes}\nKết thúc theo số thực giao: {reason}";
            order.SalesOrder.Status = "CLOSED";
            order.SalesOrder.UpdatedAt = DateTime.UtcNow;
            order.SalesOrder.Notes = string.IsNullOrWhiteSpace(order.SalesOrder.Notes)
                ? $"Kết thúc khi chưa giao đủ: {reason}"
                : $"{order.SalesOrder.Notes}\nKết thúc khi chưa giao đủ: {reason}";

            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = userId,
                ActionType = "CLOSE_PARTIAL_SALES_DELIVERY",
                EntityName = AuditEntities.OutboundOrder,
                EntityId = order.OutboundOrderId.ToString(),
                OldValues = new { OutboundStatus = "IN_PROGRESS", SalesOrderStatus = "PARTIALLY_FULFILLED" },
                NewValues = new
                {
                    OutboundStatus = "COMPLETED",
                    SalesOrderStatus = "CLOSED",
                    Reason = reason,
                    ReleasedReservationCount = reservations.Count,
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
            return (true, "Đã kết thúc SO theo số lượng thực giao và giải phóng phần tồn không còn nhu cầu giao.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    private async Task<List<Allocation>> BuildSalesAllocationsAsync(long salesOrderId, long productId, decimal quantity)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new ArgumentException("Không tìm thấy sản phẩm.");
        var query = _context.InventoryReservations
            .Where(r => r.SalesOrderDetail.SalesOrderId == salesOrderId && r.ProductId == productId &&
                        (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"));
        query = string.Equals(product.RotationMethod, "FEFO", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(r => r.ProductLot.ExpiryDate == null)
                .ThenBy(r => r.ProductLot.ExpiryDate)
                .ThenBy(r => r.ProductLot.FirstReceivedDate)
                .ThenBy(r => r.StorageLocation.LocationCode)
            : query.OrderBy(r => r.ProductLot.FirstReceivedDate)
                .ThenBy(r => r.StorageLocation.LocationCode);
        var rows = await query
            .Select(r => new Allocation(r.StorageLocationId, r.ProductLotId, r.ReservedQuantity - r.ConsumedQuantity))
            .ToListAsync();
        return TakeAllocations(rows, quantity);
    }

    private async Task<List<Allocation>> BuildInventoryAllocationsAsync(long warehouseId, long productId, decimal quantity)
    {
        var product = await _context.Products.FindAsync(productId) ?? throw new ArgumentException("Không tìm thấy sản phẩm.");
        if (!IsActive(product.Status)) throw new InvalidOperationException("Sản phẩm đang ngừng hoạt động.");
        var query = _context.Inventories.Where(i => i.ProductId == productId && i.StorageLocation.WarehouseId == warehouseId &&
            i.StorageLocation.IsPickable &&
            (i.StorageLocation.Status == "ACTIVE" || i.StorageLocation.Status == "AVAILABLE" || i.StorageLocation.Status == "OCCUPIED") &&
            i.ProductLot.Status == "AVAILABLE" && i.OnHandQuantity - i.ReservedQuantity > 0);
        var rows = product.RotationMethod == "FEFO"
            ? await query.OrderBy(i => i.ProductLot.ExpiryDate == null).ThenBy(i => i.ProductLot.ExpiryDate)
                .ThenBy(i => i.ProductLot.FirstReceivedDate).ThenBy(i => i.StorageLocation.LocationCode)
                .Select(i => new Allocation(i.StorageLocationId, i.ProductLotId, i.OnHandQuantity - i.ReservedQuantity)).ToListAsync()
            : await query.OrderBy(i => i.ProductLot.FirstReceivedDate).ThenBy(i => i.StorageLocation.LocationCode)
                .Select(i => new Allocation(i.StorageLocationId, i.ProductLotId, i.OnHandQuantity - i.ReservedQuantity)).ToListAsync();
        return TakeAllocations(rows, quantity);
    }

    private static List<Allocation> TakeAllocations(IEnumerable<Allocation> rows, decimal quantity)
    {
        var availableRows = rows.Where(r => r.Quantity > 0).ToList();
        if (availableRows.Sum(r => r.Quantity) < quantity)
            throw new InvalidOperationException("Tồn khả dụng/giữ tồn không đủ để tiếp tục phiếu xuất.");

        return availableRows;
    }

    private async Task<List<AvailableStockLocationDto>> GetAvailableAllocationsAsync(OutboundOrder order, OutboundOrderItem item)
    {
        var remaining = Math.Max(0, item.RequestedQuantity - item.IssuedQuantity);
        if (remaining <= 0) return new List<AvailableStockLocationDto>();

        var routes = order.SourceType == "SALES_ORDER"
            ? await BuildSalesAllocationsAsync(order.SalesOrderId!.Value, item.ProductId, remaining)
            : await BuildInventoryAllocationsAsync(order.WarehouseId, item.ProductId, remaining);
        var result = new List<AvailableStockLocationDto>();
        foreach (var route in routes)
        {
            long? reservationId = null;
            var available = route.Quantity;
            if (order.SourceType == "SALES_ORDER")
            {
                var reservation = await _context.InventoryReservations.FirstOrDefaultAsync(r =>
                    r.SalesOrderDetail.SalesOrderId == order.SalesOrderId && r.ProductId == item.ProductId &&
                    r.StorageLocationId == route.StorageLocationId && r.ProductLotId == route.ProductLotId &&
                    (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"));
                reservationId = reservation?.InventoryReservationId;
                if (reservation != null) available = Math.Min(available, reservation.ReservedQuantity - reservation.ConsumedQuantity);
            }
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
    private sealed record Allocation(long StorageLocationId, long ProductLotId, decimal Quantity);
}
