using System.Data;
using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.DTOs.Inbound;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Business.Services;

public class CustomerReturnRequestService(BmwmsContext context, IAuditLogService audit)
{
    private async Task<long> RequestCustomerIdAsync(long id)
    {
        if (id <= 0) throw new ArgumentException("Thiếu mã yêu cầu trả hàng. Vui lòng mở lại phiếu từ danh sách.");
        return await context.CustomerReturnRequests.AsNoTracking()
            .Where(r => r.CustomerReturnRequestId == id).Select(r => (long?)r.CustomerId).SingleOrDefaultAsync()
            ?? throw new KeyNotFoundException("Yêu cầu trả hàng không tồn tại. Vui lòng kiểm tra lại danh sách.");
    }
    private async Task<User> ActorAsync(long id, params string[] roles)
    {
        var u = await context.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.UserId == id && x.Status == "ACTIVE");
        if (u?.Role == null || !roles.Contains(BusinessRoleCodes.Normalize(u.Role.RoleCode))) throw new UnauthorizedAccessException();
        return u;
    }

    private async Task<List<(SalesOrderDetail Line, decimal Delivered, decimal Available)>> SourcesAsync(long customerId, long? excludeRequestId = null)
    {
        var lines = await context.SalesOrderDetails.Include(x => x.SalesOrder).Include(x => x.Product).ThenInclude(x => x.UnitOfMeasure)
            .Where(x => x.SalesOrder.CustomerId == customerId).OrderByDescending(x => x.SalesOrder.OrderDate)
            .ThenByDescending(x => x.SalesOrderDetailId).ToListAsync();
        var allocations = await context.CustomerReturnSourceAllocations.Include(x => x.RequestItem).ThenInclude(x => x.Request)
            .Where(x => x.RequestItem.Request.CustomerId == customerId &&
                (!excludeRequestId.HasValue || x.RequestItem.CustomerReturnRequestId != excludeRequestId.Value)).ToListAsync();
        var legacy = await context.InboundOrderItems.Include(x => x.InboundOrder).Include(x => x.InboundOrderDetails)
            .Where(x => x.InboundOrder.SourceType == "SALES_RETURN" && x.InboundOrder.ReturnRequestId == null &&
                x.InboundOrder.SalesOrder != null && x.InboundOrder.SalesOrder.CustomerId == customerId).ToListAsync();
        var issued = await context.InventoryTransactions.Where(t => t.TransactionType == "OUTBOUND" && t.OutboundOrderDetail != null &&
            t.OutboundOrderDetail.OutboundOrderItem.OutboundOrder.SourceType == "SALES_ORDER" &&
            t.OutboundOrderDetail.OutboundOrderItem.OutboundOrder.Status == "COMPLETED" &&
            t.OutboundOrderDetail.OutboundOrderItem.OutboundOrder.SalesOrder!.CustomerId == customerId)
            .GroupBy(t => new { t.OutboundOrderDetail!.OutboundOrderItem.OutboundOrder.SalesOrderId, t.ProductId })
            .Select(g => new { g.Key.SalesOrderId, g.Key.ProductId, Quantity = -g.Sum(t => t.OnHandDelta) }).ToListAsync();
        return lines.Select(line =>
        {
            var delivered = issued.Where(x => x.SalesOrderId == line.SalesOrderId && x.ProductId == line.ProductId).Sum(x => x.Quantity);
            var old = SalesOrderReturnRules.CalculateLine(delivered, legacy.Where(x => x.InboundOrder.SalesOrderId == line.SalesOrderId && x.ProductId == line.ProductId));
            var used = allocations.Where(a => a.SalesOrderDetailId == line.SalesOrderDetailId).Sum(a =>
                a.RequestItem.Request.Status is "SUBMITTED" or "APPROVED" or "PARTIALLY_RECEIVED" or "PENDING_REMAINDER_REVIEW"
                    ? a.AllocatedQuantity
                    : a.ReceivedQuantity);
            return (line, delivered, old.AvailableToPlanQuantity - used);
        }).ToList();
    }

    public async Task<List<CustomerReturnSalesOrderDto>> GetSalesOrdersAsync()
    {
        var ids = await context.InventoryTransactions.Where(t => t.TransactionType == "OUTBOUND" && t.OnHandDelta < 0 &&
            t.OutboundOrderDetail != null && t.OutboundOrderDetail.OutboundOrderItem.OutboundOrder.SourceType == "SALES_ORDER" &&
            t.OutboundOrderDetail.OutboundOrderItem.OutboundOrder.Status == "COMPLETED")
            .Select(t => t.OutboundOrderDetail!.OutboundOrderItem.OutboundOrder.SalesOrderId!.Value).Distinct().ToListAsync();
        return await context.SalesOrders.AsNoTracking().Where(s => ids.Contains(s.SalesOrderId) &&
                (s.Status == "PARTIALLY_ISSUED" || s.Status == "PARTIALLY_FULFILLED" ||
                 s.Status == "ISSUED" || s.Status == "FULFILLED" ||
                 s.Status == "COMPLETED" || s.Status == "CLOSED"))
            .OrderByDescending(s => s.OrderDate).ThenByDescending(s => s.SalesOrderId)
            .Select(s => new CustomerReturnSalesOrderDto { SalesOrderId = s.SalesOrderId, SalesOrderNumber = s.SalesOrderNumber,
                CustomerId = s.CustomerId, CustomerName = s.Customer.CustomerName }).ToListAsync();
    }

    public async Task<List<CustomerReturnSourceDto>> GetSalesOrderSourcesAsync(long salesOrderId, long? editingRequestId = null, long? userId = null)
    {
        var so = await context.SalesOrders.AsNoTracking().SingleOrDefaultAsync(s => s.SalesOrderId == salesOrderId)
            ?? throw new KeyNotFoundException("SO tham chiếu không tồn tại.");
        if (!IsReturnableSalesOrderStatus(so.Status))
            throw new InvalidOperationException("Chỉ được nhận hàng khách trả từ SO đã giao một phần hoặc đã giao xong.");
        if (editingRequestId.HasValue)
        {
            if (!userId.HasValue) throw new UnauthorizedAccessException();
            var actor = await ActorAsync(userId.Value, "SALES_STAFF", "SYSTEM_ADMIN");
            var r = await GetAsync(editingRequestId.Value) ?? throw new KeyNotFoundException("Phiếu trả không tồn tại.");
            if (!r.CanEdit || r.SalesOrderId != salesOrderId ||
                (BusinessRoleCodes.Normalize(actor.Role.RoleCode) != "SYSTEM_ADMIN" && r.CreatedByUserId != userId)) throw new UnauthorizedAccessException();
        }
        return MapSources((await SourcesAsync(so.CustomerId, editingRequestId)).Where(s => s.Line.SalesOrderId == salesOrderId));
    }

    private static List<CustomerReturnSourceDto> MapSources(IEnumerable<(SalesOrderDetail Line, decimal Delivered, decimal Available)> sources)
    {
        return sources.Where(x => x.Delivered > 0).GroupBy(x => x.Line.ProductId).Select(g =>
        {
            var p = g.First().Line.Product;
            return new CustomerReturnSourceDto { ProductId = p.ProductId, ProductCode = p.ProductCode, ProductName = p.ProductName,
                UnitName = p.UnitOfMeasure.UnitName, QuantityScale = p.UnitOfMeasure.QuantityScale,
                DeliveredQuantity = g.Sum(x => x.Delivered), AvailableToReturn = Math.Max(0, g.Sum(x => x.Available)) };
        }).OrderBy(x => x.ProductCode).ToList();
    }

    public async Task<long> CreateAsync(CreateCustomerReturnRequestDto dto, long userId)
    {
        await ActorAsync(userId, "SALES_STAFF", "SYSTEM_ADMIN");
        if ((dto.Reason?.Trim().Length ?? 0) is < 10 or > 500 || dto.Items == null || dto.Items.Count == 0 ||
            dto.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1)) throw new ArgumentException("Chọn mặt hàng không trùng và ghi lý do trả từ 10 đến 500 ký tự.");
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var so = await context.SalesOrders.AsNoTracking().SingleOrDefaultAsync(s => s.SalesOrderId == dto.SalesOrderId)
            ?? throw new ArgumentException("Chọn SO tham chiếu hợp lệ.");
        if (!IsReturnableSalesOrderStatus(so.Status))
            throw new ArgumentException("Chỉ được nhận hàng khách trả từ SO đã giao một phần hoặc đã giao xong.");
        if (dto.CustomerId != 0 && dto.CustomerId != so.CustomerId) throw new ArgumentException("Khách hàng không khớp SO tham chiếu.");
        await OrderWorkflowLock.AcquireAsync(context, "CUSTOMER_RETURN", so.CustomerId);
        if (!await context.Customers.AnyAsync(c => c.CustomerId == so.CustomerId && c.Status == "ACTIVE")) throw new ArgumentException("Khách hàng không còn hoạt động.");
        var sources = (await SourcesAsync(so.CustomerId)).Where(s => s.Line.SalesOrderId == so.SalesOrderId).ToList();
        var request = new CustomerReturnRequest { CustomerId = so.CustomerId, Reason = dto.Reason!.Trim(), CreatedByUserId = userId,
            RequestNumber = $"RET-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}" };
        PopulateItems(request, dto, sources);
        context.CustomerReturnRequests.Add(request);
        await audit.StageAsync(new AuditEventDto { UserId = userId, ActionType = "CREATE_CUSTOMER_RETURN_REQUEST", EntityName = "CustomerReturnRequest",
            EntityId = request.RequestNumber, NewValues = dto });
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return request.CustomerReturnRequestId;
    }

    private static bool IsReturnableSalesOrderStatus(string? status) =>
        (status ?? string.Empty).Trim().ToUpperInvariant() is
            "PARTIALLY_ISSUED" or "PARTIALLY_FULFILLED" or
            "ISSUED" or "FULFILLED" or "COMPLETED" or "CLOSED";

    private static void PopulateItems(CustomerReturnRequest request, CreateCustomerReturnRequestDto dto,
        List<(SalesOrderDetail Line, decimal Delivered, decimal Available)> sources)
    {
        if ((dto.Reason?.Trim().Length ?? 0) is < 10 or > 500 || dto.Items == null || dto.Items.Count == 0 ||
            dto.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1)) throw new ArgumentException("Chọn mặt hàng không trùng và ghi lý do trả từ 10 đến 500 ký tự.");
        foreach (var row in dto.Items)
        {
            var candidates = sources.Where(x => x.Line.ProductId == row.ProductId).ToList();
            var product = candidates.FirstOrDefault().Line?.Product ?? throw new ArgumentException("Khách chưa được giao mặt hàng này.");
            if (product.Status != "ACTIVE") throw new ArgumentException("Sản phẩm đã ngừng hoạt động; cần xác minh cấu hình trước khi nhận trả.");
            QuantityRules.EnsureValid(product, row.Quantity, "Số khách yêu cầu trả");
            if (row.Quantity > candidates.Sum(x => x.Available)) throw new ArgumentException($"{product.ProductCode}: lượng trả vượt phần thực giao còn được trả.");
            var item = new CustomerReturnRequestItem { ProductId = row.ProductId, RequestedQuantity = row.Quantity };
            var remaining = row.Quantity;
            foreach (var source in candidates.Where(x => x.Available > 0))
            {
                var take = Math.Min(remaining, source.Available);
                if (take <= 0) break;
                item.Allocations.Add(new CustomerReturnSourceAllocation { SalesOrderDetailId = source.Line.SalesOrderDetailId, AllocatedQuantity = take });
                remaining -= take;
            }
            request.Items.Add(item);
        }
    }

    public async Task UpdateAsync(long id, CreateCustomerReturnRequestDto dto, long userId)
    {
        var actor = await ActorAsync(userId, "SALES_STAFF", "SYSTEM_ADMIN");
        var customerId = await RequestCustomerIdAsync(id);
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await OrderWorkflowLock.AcquireAsync(context, "CUSTOMER_RETURN", customerId);
        var r = await context.CustomerReturnRequests.Include(x => x.Items).ThenInclude(x => x.Allocations)
            .ThenInclude(x => x.SalesOrderDetail).SingleOrDefaultAsync(x => x.CustomerReturnRequestId == id)
            ?? throw new KeyNotFoundException("Phiếu trả không tồn tại.");
        if (BusinessRoleCodes.Normalize(actor.Role.RoleCode) != "SYSTEM_ADMIN" && r.CreatedByUserId != userId) throw new UnauthorizedAccessException();
        if (r.Status != "SUBMITTED" || await context.InboundOrders.AnyAsync(o => o.ReturnRequestId == id) ||
            r.Items.SelectMany(i => i.Allocations).Any(a => a.ReceivedQuantity > 0))
            throw new InvalidOperationException("Chỉ sửa phiếu chưa duyệt và chưa có đợt nhận hàng.");
        if (dto.RowVersion != Convert.ToBase64String(r.RowVersion)) throw new InvalidOperationException("Phiếu đã thay đổi. Vui lòng tải lại.");
        var sourceIds = r.Items.SelectMany(i => i.Allocations).Select(a => a.SalesOrderDetail.SalesOrderId).Distinct().ToList();
        if (sourceIds.Count != 1 || sourceIds[0] != dto.SalesOrderId || (dto.CustomerId != 0 && dto.CustomerId != customerId))
            throw new ArgumentException("Không đổi SO/khách hàng của phiếu đã tạo. Phiếu lịch sử nhiều SO chỉ được xem.");
        var replacement = new CustomerReturnRequest();
        PopulateItems(replacement, dto, (await SourcesAsync(customerId, id)).Where(s => s.Line.SalesOrderId == dto.SalesOrderId).ToList());
        var old = new { r.Reason, Items = r.Items.Select(i => new { i.ProductId, i.RequestedQuantity }).ToList() };
        // No receipts exist: remove only this unapproved request's allocations/items,
        // never SO detail IDs, reservations, receipts or ledger history.
        context.CustomerReturnSourceAllocations.RemoveRange(r.Items.SelectMany(i => i.Allocations));
        context.CustomerReturnRequestItems.RemoveRange(r.Items);
        await context.SaveChangesAsync();
        r.Items.Clear();
        foreach (var item in replacement.Items) r.Items.Add(item);
        r.Reason = dto.Reason.Trim();
        await audit.StageAsync(new AuditEventDto { UserId = userId, ActionType = "UPDATE_CUSTOMER_RETURN_REQUEST", EntityName = "CustomerReturnRequest",
            EntityId = r.RequestNumber, OldValues = old, NewValues = dto });
        // Ensure rowversion advances even when only child quantities changed.
        context.Entry(r).Property(x => x.Reason).IsModified = true;
        await context.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task<List<CustomerReturnRequestDto>> GetListAsync() =>
        await context.CustomerReturnRequests.AsNoTracking().OrderBy(x =>
                x.Status == "SUBMITTED" || x.Status == "PENDING_REMAINDER_REVIEW" ? 0 :
                x.Status == "APPROVED" || x.Status == "PARTIALLY_RECEIVED" ? 1 : 2)
            .ThenByDescending(x => x.CreatedAt).Take(200).Select(x => new CustomerReturnRequestDto { Id = x.CustomerReturnRequestId,
                SalesOrderId = x.Items.SelectMany(i => i.Allocations).Select(a => a.SalesOrderDetail.SalesOrderId).Distinct().Count() == 1
                    ? x.Items.SelectMany(i => i.Allocations).Select(a => (long?)a.SalesOrderDetail.SalesOrderId).FirstOrDefault() : null,
                SalesOrderNumber = x.Items.SelectMany(i => i.Allocations).Select(a => a.SalesOrderDetail.SalesOrder.SalesOrderNumber).Distinct().Count() == 1
                    ? x.Items.SelectMany(i => i.Allocations).Select(a => a.SalesOrderDetail.SalesOrder.SalesOrderNumber).FirstOrDefault() : null,
                RequestNumber = x.RequestNumber, CustomerId = x.CustomerId, CustomerName = x.Customer.CustomerName,
                Status = x.Status, Reason = x.Reason, CreatedAt = x.CreatedAt }).ToListAsync();

    public async Task<CustomerReturnRequestDto?> GetAsync(long id)
    {
        var r = await context.CustomerReturnRequests.AsNoTracking().Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x.UnitOfMeasure)
            .Include(x => x.Items).ThenInclude(x => x.Allocations).ThenInclude(x => x.SalesOrderDetail).ThenInclude(x => x.SalesOrder)
            .SingleOrDefaultAsync(x => x.CustomerReturnRequestId == id);
        if (r == null) return null;
        var sourceOrders = r.Items.SelectMany(i => i.Allocations).Select(a => a.SalesOrderDetail.SalesOrder).DistinctBy(s => s.SalesOrderId).ToList();
        var inboundIds = await context.InboundOrders.Where(o => o.ReturnRequestId == id).OrderByDescending(o => o.InboundOrderId).Select(o => o.InboundOrderId).ToListAsync();
        return new CustomerReturnRequestDto { Id = id, CreatedByUserId = r.CreatedByUserId, RequestNumber = r.RequestNumber, CustomerId = r.CustomerId, CustomerName = r.Customer.CustomerName,
            SalesOrderId = sourceOrders.Count == 1 ? sourceOrders[0].SalesOrderId : null,
            SalesOrderNumber = sourceOrders.Count == 1 ? sourceOrders[0].SalesOrderNumber : null,
            CanEdit = sourceOrders.Count == 1 && r.Status == "SUBMITTED" && inboundIds.Count == 0,
            Status = r.Status, Reason = r.Reason, DecisionReason = r.DecisionReason, CreatedAt = r.CreatedAt, RowVersion = Convert.ToBase64String(r.RowVersion),
            InboundIds = inboundIds,
            Items = r.Items.Select(i => new CustomerReturnRequestLineDto { ProductId = i.ProductId, ProductCode = i.Product.ProductCode,
                ProductName = i.Product.ProductName, UnitName = i.Product.UnitOfMeasure.UnitName, QuantityScale = i.Product.UnitOfMeasure.QuantityScale,
                RequestedQuantity = i.RequestedQuantity, ReceivedQuantity = i.Allocations.Sum(a => a.ReceivedQuantity),
                RemainingQuantity = i.RequestedQuantity - i.Allocations.Sum(a => a.ReceivedQuantity),
                Sources = i.Allocations.Select(a => new CustomerReturnProofDto { SalesOrderId = a.SalesOrderDetail.SalesOrderId, SalesOrderNumber = a.SalesOrderDetail.SalesOrder.SalesOrderNumber,
                    Quantity = a.AllocatedQuantity, ReceivedQuantity = a.ReceivedQuantity }).ToList() }).ToList() };
    }

    public async Task DecideAsync(long id, CustomerReturnDecisionDto dto, long userId)
    {
        var actor = await ActorAsync(userId, "WAREHOUSE_MANAGER", "SYSTEM_ADMIN", "SALES_STAFF");
        var customerId = await RequestCustomerIdAsync(id);
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await OrderWorkflowLock.AcquireAsync(context, "CUSTOMER_RETURN", customerId);
        var r = await context.CustomerReturnRequests.Include(x => x.Items).ThenInclude(x => x.Allocations).SingleOrDefaultAsync(x => x.CustomerReturnRequestId == id)
            ?? throw new KeyNotFoundException("Yêu cầu trả hàng không còn tồn tại. Vui lòng kiểm tra lại danh sách.");
        if (dto.RowVersion != Convert.ToBase64String(r.RowVersion)) throw new InvalidOperationException("Yêu cầu đã thay đổi. Vui lòng tải lại.");
        var action = dto.Action?.Trim().ToUpperInvariant() ?? "";
        var actorRole = BusinessRoleCodes.Normalize(actor.Role.RoleCode);
        if (actorRole == "SALES_STAFF" && (action != "CANCEL" || r.CreatedByUserId != userId)) throw new UnauthorizedAccessException();
        if (action == "APPROVE")
        {
            if (r.Status != "SUBMITTED") throw new InvalidOperationException("Chỉ duyệt yêu cầu đang chờ duyệt.");
            if (await context.InboundOrders.AnyAsync(o => o.ReturnRequestId == id))
                throw new InvalidOperationException("Phiếu chưa duyệt đã có đợt nhập; cần đối soát trước khi duyệt.");
            if (r.Items.Count == 0 || r.Items.Any(i => i.RequestedQuantity <= 0 || i.Allocations.Count == 0 ||
                i.Allocations.Any(a => a.AllocatedQuantity <= 0) || i.Allocations.Sum(a => a.AllocatedQuantity) != i.RequestedQuantity))
                throw new InvalidOperationException("Yêu cầu thiếu mặt hàng hoặc nguồn giao không đủ. Cần đối soát lại trước khi duyệt.");
            // Entitlements were reserved at submission. Recheck gross ledger proof
            // against all active/legacy requests, without treating own budget as free.
            var sources = await SourcesAsync(r.CustomerId);
            if (sources.Any(s => s.Available < 0)) throw new InvalidOperationException("Ngân sách trả hàng đã vượt nguồn giao; cần đối soát trước khi duyệt.");
            foreach (var i in r.Items)
                foreach (var a in i.Allocations)
                    if (!sources.Any(s => s.Line.SalesOrderDetailId == a.SalesOrderDetailId && s.Delivered >= a.AllocatedQuantity))
                        throw new InvalidOperationException("Nguồn giao không còn hợp lệ; không thể duyệt.");
            r.Status = "APPROVED"; r.ApprovedByUserId = userId; r.ApprovedAt = DateTime.UtcNow;
        }
        else if (action is "REJECT" or "CANCEL")
        {
            if (action == "CANCEL" && actorRole == "WAREHOUSE_MANAGER")
                throw new InvalidOperationException("Phiếu đang chờ duyệt; Manager dùng chức năng từ chối.");
            if (r.Status != "SUBMITTED" || await context.InboundOrders.AnyAsync(o => o.ReturnRequestId == id)) throw new InvalidOperationException("Chỉ từ chối/hủy yêu cầu chưa duyệt và chưa có phiếu nhập.");
            if ((dto.Reason?.Trim().Length ?? 0) is < 5 or > 500) throw new ArgumentException("Nhập lý do từ 5 đến 500 ký tự.");
            r.Status = action == "REJECT" ? "REJECTED" : "CANCELLED"; r.DecisionReason = dto.Reason!.Trim();
        }
        else if (action is "CONTINUE" or "CLOSE")
        {
            if (actorRole is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN")) throw new UnauthorizedAccessException();
            if (r.Status != "PENDING_REMAINDER_REVIEW")
                throw new InvalidOperationException("Chỉ quyết định phần còn lại sau khi một đợt nhận trả đã hoàn tất nhưng còn thiếu.");
            if ((dto.Reason?.Trim().Length ?? 0) is < 10 or > 500)
                throw new ArgumentException("Căn cứ quyết định phải từ 10 đến 500 ký tự.");
            if (await context.InboundOrders.AnyAsync(o => o.ReturnRequestId == id && o.Status != "CANCELLED" &&
                (o.Status != "COMPLETED" || o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails).Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null))))
                throw new InvalidOperationException("Còn phiếu nhận trả đang xử lý/chờ cất.");
            r.Status = action == "CONTINUE" ? "PARTIALLY_RECEIVED" : "COMPLETED";
            r.DecisionReason = dto.Reason!.Trim();
        }
        else throw new ArgumentException("Thao tác không hợp lệ.");
        await audit.StageAsync(new AuditEventDto { UserId = userId, ActionType = "DECIDE_CUSTOMER_RETURN_REQUEST", EntityName = "CustomerReturnRequest",
            EntityId = r.RequestNumber, NewValues = new { action, r.Status, r.DecisionReason } });
        await context.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task<long> CreateReceiptAsync(long id, DateOnly receiptDate, long userId)
    {
        await ActorAsync(userId, "WAREHOUSE_STAFF");
        if (receiptDate == default || receiptDate > DateOnly.FromDateTime(DateTime.Today)) throw new ArgumentException("Ngày nhận phải hợp lệ và không ở tương lai.");
        var customerId = await RequestCustomerIdAsync(id);
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await OrderWorkflowLock.AcquireAsync(context, "CUSTOMER_RETURN", customerId);
        var r = await context.CustomerReturnRequests.Include(x => x.Items).ThenInclude(x => x.Allocations).SingleOrDefaultAsync(x => x.CustomerReturnRequestId == id)
            ?? throw new KeyNotFoundException("Yêu cầu trả hàng không còn tồn tại. Vui lòng kiểm tra lại danh sách.");
        if (r.Status is not ("APPROVED" or "PARTIALLY_RECEIVED")) throw new InvalidOperationException("Yêu cầu chưa được Manager duyệt hoặc đã đóng.");
        if (await context.InboundOrders.AnyAsync(o => o.ReturnRequestId == id && o.Status != "CANCELLED" &&
            (o.Status != "COMPLETED" || o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails).Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null))))
            throw new InvalidOperationException("Phải hoàn tất/cất đợt hiện tại trước khi nhận tiếp.");
        var warehouses = await context.Warehouses.Where(w => w.Status == "ACTIVE").Take(2).ToListAsync();
        if (warehouses.Count != 1) throw new InvalidOperationException("Phải có đúng một kho hoạt động.");
        var order = new InboundOrder { ReturnRequestId = id, SourceType = "SALES_RETURN", WarehouseId = warehouses[0].WarehouseId,
            InboundOrderNumber = $"IN-RET-{Guid.NewGuid():N}", Status = "ASSIGNED", ExpectedReceiptDate = receiptDate,
            CreatedByUserId = userId, AssignedToUserId = userId, CreatedAt = DateTime.UtcNow,
            ConfirmedByUserId = r.ApprovedByUserId, ConfirmedAt = r.ApprovedAt, Notes = r.Reason };
        foreach (var i in r.Items)
        {
            var remaining = i.RequestedQuantity - i.Allocations.Sum(a => a.ReceivedQuantity);
            if (remaining > 0) order.InboundOrderItems.Add(new InboundOrderItem { ReturnRequestItemId = i.CustomerReturnRequestItemId,
                ProductId = i.ProductId, ExpectedQuantity = remaining });
        }
        if (!order.InboundOrderItems.Any()) throw new InvalidOperationException("Không còn lượng được nhận trả.");
        context.InboundOrders.Add(order);
        await audit.StageAsync(new AuditEventDto { UserId = userId, ActionType = "CREATE_CUSTOMER_RETURN_RECEIPT", EntityName = AuditEntities.InboundOrder,
            EntityId = order.InboundOrderNumber, NewValues = new { id, receiptDate, AssignedToUserId = userId } });
        await context.SaveChangesAsync(); await tx.CommitAsync(); return order.InboundOrderId;
    }
}
