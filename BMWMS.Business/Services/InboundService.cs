using System;
using System.Linq;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.Inbound;
using BMWMS.Business.Interfaces;
using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.Configuration;
using BMWMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BMWMS.Business.Services;

public class InboundService : IInboundService
{
    private readonly IInboundRepository _inboundRepository;
    private readonly BMWMS.Repository.Models.BmwmsContext _context;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICapacityEvaluationService _capacityEvaluationService;
    private readonly WarehouseCapacityOptions _capacityOptions;

    public InboundService(
        IInboundRepository inboundRepository,
        BMWMS.Repository.Models.BmwmsContext context,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ICapacityEvaluationService capacityEvaluationService,
        WarehouseCapacityOptions capacityOptions)
    {
        _inboundRepository = inboundRepository;
        _context = context;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _capacityEvaluationService = capacityEvaluationService;
        _capacityOptions = capacityOptions;
    }

    public async Task<InboundOrderPageDto> GetInboundOrdersPageAsync(InboundOrderFilterDto filter)
    {
        var result = await _inboundRepository.GetInboundOrdersPageAsync(
            filter.Keyword,
            filter.Status,
            filter.SourceType,
            filter.FromDate,
            filter.ToDate,
            filter.AssignedToUserId,
            filter.PageIndex,
            filter.PageSize);

        return new InboundOrderPageDto
        {
            TotalCount = result.TotalCount,
            Items = result.Items.Select(x => new InboundOrderListDto
            {
                InboundOrderId = x.InboundOrderId,
                InboundOrderNumber = x.InboundOrderNumber,
                SupplierName = x.SourceType == "SALES_RETURN"
                    ? x.SalesOrder?.Customer?.CustomerName ?? "Không xác định"
                    : x.PurchaseOrder?.Supplier?.SupplierName ?? "Không xác định",
                SourceType = x.SourceType,
                SourceReference = x.SourceType == "SALES_RETURN"
                    ? x.SalesOrder?.SalesOrderNumber ?? string.Empty
                    : x.PurchaseOrder?.PurchaseOrderNumber ?? string.Empty,
                ExpectedReceiptDate = x.ExpectedReceiptDate,
                Status = GetInboundDisplayStatus(x),
                TotalExpectedQuantity = x.InboundOrderItems.Sum(i => i.ExpectedQuantity),
                TotalReceivedQuantity = x.InboundOrderItems.Sum(i => i.ReceivedQuantity),
                LineCount = x.InboundOrderItems.Count,
                IsSupplemental = x.ParentInboundOrderId.HasValue,
                CreatedAt = x.CreatedAt
            }).ToList()
        };
    }

    public async Task<InboundOrderDetailDto?> GetInboundOrderByIdAsync(long id)
    {
        var order = await _inboundRepository.GetByIdAsync(id);
        if (order == null) return null;

        var dto = new InboundOrderDetailDto
        {
            InboundOrderId = order.InboundOrderId,
            InboundOrderNumber = order.InboundOrderNumber,
            PurchaseOrderNumber = order.PurchaseOrder?.PurchaseOrderNumber,
            PurchaseOrderId = order.PurchaseOrderId,
            SalesOrderNumber = order.SalesOrder?.SalesOrderNumber,
            SalesOrderId = order.SalesOrderId,
            SourceType = order.SourceType,
            SourceReference = order.SourceType == "SALES_RETURN"
                ? order.SalesOrder?.SalesOrderNumber ?? string.Empty
                : order.PurchaseOrder?.PurchaseOrderNumber ?? string.Empty,
            PartnerName = order.SourceType == "SALES_RETURN"
                ? order.SalesOrder?.Customer?.CustomerName ?? string.Empty
                : order.PurchaseOrder?.Supplier?.SupplierName ?? string.Empty,
            SupplierName = order.SourceType == "SALES_RETURN"
                ? order.SalesOrder?.Customer?.CustomerName ?? string.Empty
                : order.PurchaseOrder?.Supplier?.SupplierName ?? string.Empty,
            WarehouseName = order.Warehouse?.WarehouseName ?? "",
            WarehouseId = order.WarehouseId,
            ExpectedReceiptDate = order.ExpectedReceiptDate,
            Status = GetInboundDisplayStatus(order),
            AssignedToUserId = order.AssignedToUserId,
            AssignedToUserName = order.AssignedToUser?.FullName ?? "",
            CreatedByUserName = order.CreatedByUser?.FullName ?? "",
            Notes = order.Notes,
            CancellationReason = order.CancellationReason,
            ParentInboundOrderNumber = order.ParentInboundOrder?.InboundOrderNumber,
            ParentInboundOrderId = order.ParentInboundOrderId,
            Items = order.InboundOrderItems.OrderBy(i => i.InboundOrderItemId).Select(i => new InboundOrderItemDto
            {
                InboundOrderItemId = i.InboundOrderItemId,
                ProductId = i.ProductId,
                ProductCode = i.Product.ProductCode,
                ProductName = i.Product.ProductName,
                UnitName = i.Product.UnitOfMeasure?.UnitName ?? string.Empty,
                QuantityScale = i.Product.UnitOfMeasure?.QuantityScale ?? 0,
                TrackLot = i.Product.TrackLot,
                TrackExpiry = i.Product.TrackExpiry,
                RotationMethod = i.Product.RotationMethod?.Trim().ToUpperInvariant() ?? string.Empty,
                ExpectedQuantity = i.ExpectedQuantity,
                ReceivedQuantity = i.ReceivedQuantity,
                PutawayQuantity = i.InboundOrderDetails
                    .Where(d => d.InventoryTransaction != null && d.InventoryTransaction.TransactionType == "INBOUND")
                    .Sum(d => d.ReceivedQuantity),
                DamagedQuantity = i.DamagedQuantity,
                ShortageQuantity = i.ShortageQuantity,
                SupplementalRemainingQuantity = Math.Max(0, i.ExpectedQuantity - i.InboundOrderDetails
                    .Where(detail => detail.ConditionStatus == "GOOD")
                    .Sum(detail => detail.ReceivedQuantity)),
                Notes = i.Notes,
                LotNumber = i.InboundOrderDetails.FirstOrDefault()?.ProductLot?.LotNumber,
                ExpiryDate = i.InboundOrderDetails.FirstOrDefault()?.ProductLot?.ExpiryDate,
                Receipts = i.InboundOrderDetails.Select(d => new BMWMS.Business.DTOs.Inbound.InboundReceiptDto
                {
                    InboundOrderDetailId = d.InboundOrderDetailId,
                    ProductLotId = d.ProductLotId,
                    LotNumber = d.ProductLot?.LotNumber ?? string.Empty,
                    ExpiryDate = d.ProductLot?.ExpiryDate,
                    ManufactureDate = d.ProductLot?.ManufactureDate,
                    ReceivedQuantity = d.ReceivedQuantity,
                    PutawayQuantity = d.InventoryTransaction?.TransactionType == "INBOUND" ? d.ReceivedQuantity : 0,
                    ConditionStatus = d.ConditionStatus,
                    LocationCode = d.StorageLocation?.LocationCode ?? string.Empty,
                    ZoneCode = d.StorageLocation?.StorageRack?.WarehouseZone?.ZoneCode ?? string.Empty,
                    RackCode = d.StorageLocation?.StorageRack?.RackCode ?? string.Empty,
                    RecordedByUserName = d.RecordedByUser?.FullName ?? string.Empty,
                    RecordedAt = d.RecordedAt,
                    Notes = d.Notes,
                    PutawayByUserName = d.InventoryTransaction?.PerformedByUser?.FullName ?? string.Empty,
                    PutawayAt = d.InventoryTransaction?.TransactionAt
                }).ToList()
            }).ToList()
        };

        var timeline = new List<InboundOrderTimelineDto>();
        
        timeline.Add(new InboundOrderTimelineDto
        {
            EventTime = order.CreatedAt,
            EventName = "Tạo lệnh nhập",
            StatusBadge = order.ConfirmedAt.HasValue ? "READY" : "DRAFT",
            StatusBadgeColor = "gy",
            PerformedBy = order.CreatedByUser?.FullName ?? "Hệ thống"
        });

        if (order.ConfirmedAt.HasValue)
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = order.ConfirmedAt.Value,
                EventName = "Xác nhận lệnh nhập",
                StatusBadge = "READY",
                StatusBadgeColor = "b",
                PerformedBy = order.ConfirmedByUser?.FullName ?? "Quản lý"
            });
        }
        
        var receiptDetails = order.InboundOrderItems.SelectMany(i => i.InboundOrderDetails).ToList();
        if (receiptDetails.Count > 0)
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = receiptDetails.Min(d => d.RecordedAt),
                EventName = "Bắt đầu kiểm nhận hàng",
                StatusBadge = "RECEIVING",
                StatusBadgeColor = "b",
                PerformedBy = receiptDetails.OrderBy(d => d.RecordedAt).First().RecordedByUser?.FullName ?? order.AssignedToUser?.FullName ?? ""
            });
        }

        if (order.Status == "COMPLETED")
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = receiptDetails.Count > 0 ? receiptDetails.Max(d => d.RecordedAt) : order.ConfirmedAt ?? order.CreatedAt,
                EventName = "Hoàn tất kiểm nhận",
                StatusBadge = "RECEIVED",
                StatusBadgeColor = "g",
                PerformedBy = order.AssignedToUser?.FullName ?? ""
            });

            var postedTransactions = receiptDetails.Where(d => d.InventoryTransaction != null)
                .Select(d => d.InventoryTransaction!).ToList();
            if (postedTransactions.Count > 0)
            {
                timeline.Add(new InboundOrderTimelineDto
                {
                    EventTime = postedTransactions.Max(t => t.TransactionAt),
                    EventName = postedTransactions.Count == receiptDetails.Count(d => d.ConditionStatus == "GOOD")
                        ? "Hoàn tất xếp hàng vào vị trí kho"
                        : "Xếp một phần hàng vào vị trí kho",
                    StatusBadge = postedTransactions.Count == receiptDetails.Count(d => d.ConditionStatus == "GOOD")
                        ? "PUTAWAY_COMPLETED"
                        : "RECEIVED",
                    StatusBadgeColor = "g",
                    PerformedBy = postedTransactions.OrderByDescending(t => t.TransactionAt).First().PerformedByUser?.FullName ?? order.AssignedToUser?.FullName ?? ""
                });
            }
        }
        
        if (order.CancelledAt.HasValue)
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = order.CancelledAt.Value,
                EventName = "Hủy lệnh nhập",
                StatusBadge = "CANCELLED",
                StatusBadgeColor = "r",
                PerformedBy = order.CancelledByUser?.FullName ?? ""
            });
        }

        // Sắp xếp giảm dần theo thời gian (mới nhất lên trên)
        dto.Timeline = timeline.OrderByDescending(t => t.EventTime).ToList();

        return dto;
    }

    public async Task<long> CreateInboundOrderAsync(CreateInboundOrderDto dto, long currentUserId)
    {
        dto.SourceType = (dto.SourceType ?? string.Empty).Trim().ToUpperInvariant();
        await using var assignmentTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await EnsureWarehouseStaffCreatorAsync(currentUserId);

        var activeWarehouses = await _context.Warehouses
            .Where(w => w.Status == "ACTIVE")
            .OrderBy(w => w.WarehouseId)
            .Take(2)
            .ToListAsync();
        if (activeWarehouses.Count != 1)
            throw new InvalidOperationException("Hệ thống một kho phải có đúng một kho đang hoạt động.");
        dto.WarehouseId = activeWarehouses[0].WarehouseId;

        dto.IsSubmit = true;
        dto.AssignedToUserId = currentUserId;
        await EnsureWarehouseStaffAssigneeAsync(currentUserId);

        var submittedItems = (dto.Items ?? new List<CreateInboundOrderItemDto>()).ToList();
        if (submittedItems.Count == 0 || submittedItems.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
            throw new ArgumentException("Phiếu nhập phải chứa đúng một dòng cho mỗi vật tư của đơn nguồn.");
        var submittedByProduct = submittedItems.ToDictionary(item => item.ProductId);

        if (dto.SourceType == "PURCHASE_ORDER")
        {
            if (!dto.PurchaseOrderId.HasValue)
                throw new ArgumentException("Vui lòng chọn PO cần nhập hàng.");

            var po = await GetPurchaseOrderForInboundAsync(dto.PurchaseOrderId.Value);
            if (po == null)
                throw new ArgumentException("PO không ở trạng thái đủ điều kiện hoặc không còn số lượng để lập phiếu nhập.");

            var sourceItems = po.Items
                .Where(item => item.RemainingQuantity > 0)
                .OrderBy(item => item.ProductId)
                .ToList();
            var sourceSnapshotMatches = submittedItems.Count == sourceItems.Count && sourceItems.All(sourceItem =>
                submittedByProduct.TryGetValue(sourceItem.ProductId, out var submitted) &&
                submitted.ExpectedQuantity == sourceItem.RemainingQuantity);
            if (!sourceSnapshotMatches)
                throw new ArgumentException("Dữ liệu PO đã thay đổi hoặc đã bị chỉnh sửa. Vui lòng tải lại PO trước khi lập phiếu nhập.");

            dto.Items = sourceItems.Select(item => new CreateInboundOrderItemDto
            {
                ProductId = item.ProductId,
                ExpectedQuantity = item.RemainingQuantity,
                ActualReceivedQuantity = submittedByProduct[item.ProductId].ActualReceivedQuantity,
                ManufactureDate = submittedByProduct[item.ProductId].ManufactureDate,
                ExpiryDate = submittedByProduct[item.ProductId].ExpiryDate,
                Notes = submittedByProduct[item.ProductId].Notes
            }).ToList();
            dto.SalesOrderId = null;
        }
        else if (dto.SourceType == "SALES_RETURN")
        {
            if (!dto.SalesOrderId.HasValue)
                throw new ArgumentException("Vui lòng chọn SO có hàng khách trả.");
            if (string.IsNullOrWhiteSpace(dto.Notes) || dto.Notes.Trim().Length < 10)
                throw new ArgumentException("Hàng khách trả phải có lý do và căn cứ chấp thuận rõ ràng, tối thiểu 10 ký tự.");

            var salesOrder = await GetSalesOrderForInboundAsync(dto.SalesOrderId.Value);
            if (salesOrder == null)
                throw new ArgumentException("SO chưa phát sinh xuất hàng hoặc không còn số lượng có thể trả.");

            var sourceItems = salesOrder.Items.Where(item => item.RemainingQuantity > 0).ToList();
            var sourceSnapshotMatches = submittedItems.Count == sourceItems.Count && sourceItems.All(sourceItem =>
                submittedByProduct.TryGetValue(sourceItem.ProductId, out var submitted) &&
                submitted.ExpectedQuantity == sourceItem.RemainingQuantity);
            if (!sourceSnapshotMatches)
                throw new ArgumentException("Dữ liệu SO đã thay đổi hoặc đã bị chỉnh sửa. Vui lòng tải lại SO trước khi lập phiếu nhập.");

            dto.Items = sourceItems.Select(item => new CreateInboundOrderItemDto
            {
                ProductId = item.ProductId,
                ExpectedQuantity = item.RemainingQuantity,
                ActualReceivedQuantity = submittedByProduct[item.ProductId].ActualReceivedQuantity,
                ManufactureDate = submittedByProduct[item.ProductId].ManufactureDate,
                ExpiryDate = submittedByProduct[item.ProductId].ExpiryDate,
                Notes = submittedByProduct[item.ProductId].Notes
            }).ToList();
            dto.PurchaseOrderId = null;
            dto.Notes = dto.Notes.Trim();
        }
        else
        {
            throw new ArgumentException("Loại nguồn phiếu nhập kho không được hỗ trợ.");
        }

        if (dto.ExpectedReceiptDate == default)
            throw new ArgumentException("Vui lòng chọn ngày nhận hàng của đợt này.");
        if (dto.ExpectedReceiptDate > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Ngày nhận thực tế không được nằm trong tương lai.");
        if (dto.Items.Any(i => i.ExpectedQuantity <= 0))
            throw new ArgumentException("Số lượng dự kiến phải lớn hơn 0.");
        if (dto.Items.Any(i => i.ActualReceivedQuantity < 0 || i.ActualReceivedQuantity > i.ExpectedQuantity))
            throw new ArgumentException("Số lượng thực nhận phải từ 0 đến số lượng còn lại của từng vật tư.");
        if (dto.Items.All(i => i.ActualReceivedQuantity == 0))
            throw new ArgumentException("Phải có ít nhất một vật tư có số lượng thực nhận lớn hơn 0.");
        await ValidateQuantitiesAsync(dto.Items.SelectMany(i => new[]
        {
            (i.ProductId, i.ExpectedQuantity, "Số lượng theo đơn"),
            (i.ProductId, i.ActualReceivedQuantity, "Số lượng thực nhận")
        }).Where(value => value.Item2 > 0));
        var productIds = dto.Items.Select(item => item.ProductId).ToList();
        var products = await _context.Products
            .Include(product => product.UnitOfMeasure)
            .Where(product => productIds.Contains(product.ProductId))
            .ToDictionaryAsync(product => product.ProductId);
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var item in dto.Items.Where(item => item.ActualReceivedQuantity > 0))
        {
            var product = products[item.ProductId];
            var requiresExpiry = product.TrackExpiry ||
                string.Equals(product.RotationMethod?.Trim(), "FEFO", StringComparison.OrdinalIgnoreCase);
            if (requiresExpiry && (!item.ExpiryDate.HasValue || item.ExpiryDate.Value <= today))
                throw new ArgumentException($"Vật tư {product.ProductCode} phải có hạn sử dụng trong tương lai.");
            if (item.ManufactureDate.HasValue && item.ManufactureDate.Value > today)
                throw new ArgumentException($"Ngày sản xuất của {product.ProductCode} không được nằm trong tương lai.");
            if (item.ManufactureDate.HasValue && item.ExpiryDate.HasValue && item.ManufactureDate > item.ExpiryDate)
                throw new ArgumentException($"Ngày sản xuất của {product.ProductCode} không được sau hạn sử dụng.");
        }

        var receiving = await _context.StorageLocations.FirstOrDefaultAsync(location =>
            location.WarehouseId == dto.WarehouseId && location.LocationType == "RECEIVING" &&
            location.Status != "BLOCKED" && location.Status != "INACTIVE")
            ?? await _context.StorageLocations.FirstOrDefaultAsync(location =>
                location.WarehouseId == dto.WarehouseId && location.LocationType == "STAGING" &&
                location.Status != "BLOCKED" && location.Status != "INACTIVE")
            ?? throw new InvalidOperationException("Kho chưa cấu hình vị trí tiếp nhận hàng đang hoạt động.");

        var inboundOrder = new BMWMS.Repository.Models.InboundOrder
        {
            InboundOrderNumber = "IN-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
            SourceType = dto.SourceType,
            PurchaseOrderId = dto.PurchaseOrderId,
            SalesOrderId = dto.SalesOrderId,
            WarehouseId = dto.WarehouseId,
            ExpectedReceiptDate = dto.ExpectedReceiptDate,
            Notes = dto.Notes,
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUserId,
            AssignedToUserId = currentUserId,
            ConfirmedAt = DateTime.UtcNow,
            ConfirmedByUserId = currentUserId,
            ParentInboundOrderId = null
        };

        var pendingReceiptRows = new List<(BMWMS.Repository.Models.InboundOrderItem Item, CreateInboundOrderItemDto Request)>();
        foreach (var itemDto in dto.Items)
        {
            var item = new BMWMS.Repository.Models.InboundOrderItem
            {
                ProductId = itemDto.ProductId,
                ExpectedQuantity = itemDto.ExpectedQuantity,
                ReceivedQuantity = itemDto.ActualReceivedQuantity,
                DamagedQuantity = 0,
                ShortageQuantity = itemDto.ExpectedQuantity - itemDto.ActualReceivedQuantity,
                Notes = itemDto.Notes
            };
            inboundOrder.InboundOrderItems.Add(item);
            if (itemDto.ActualReceivedQuantity > 0)
                pendingReceiptRows.Add((item, itemDto));
        }

        await _auditLogService.StageAsync(new AuditEventDto
        {
            UserId = currentUserId,
            ActionType = "CREATE_INBOUND",
            EntityName = AuditEntities.InboundOrder,
            EntityId = inboundOrder.InboundOrderNumber,
            NewValues = new
            {
                inboundOrder.InboundOrderNumber,
                inboundOrder.SourceType,
                inboundOrder.PurchaseOrderId,
                inboundOrder.SalesOrderId,
                inboundOrder.ExpectedReceiptDate,
                inboundOrder.Status,
                inboundOrder.AssignedToUserId,
                inboundOrder.Notes,
                Items = dto.Items.Select(item => new
                {
                    item.ProductId,
                    item.ExpectedQuantity,
                    item.ActualReceivedQuantity,
                    item.ManufactureDate,
                    item.ExpiryDate
                })
            }
        });

        await _inboundRepository.AddAsync(inboundOrder);

        // InboundOrderDetails only exposes scalar composite FK values
        // (InboundOrderItemId, InboundOrderId, ProductId), not a navigation to
        // InboundOrderItem. Flush the header/items first so their identity values
        // are available before constructing receipt details. Both saves remain in
        // the same serializable transaction and are rolled back together on error.
        await _inboundRepository.SaveChangesAsync();

        foreach (var (item, request) in pendingReceiptRows)
        {
            var layer = new BMWMS.Repository.Models.ProductLot
            {
                ProductId = request.ProductId,
                LotNumber = $"RCV-{inboundOrder.InboundOrderId}-{item.InboundOrderItemId}-{Guid.NewGuid():N}",
                ManufactureDate = request.ManufactureDate,
                ExpiryDate = request.ExpiryDate,
                FirstReceivedDate = dto.ExpectedReceiptDate,
                Status = "AVAILABLE",
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductLots.Add(layer);
            _context.InboundOrderDetails.Add(new BMWMS.Repository.Models.InboundOrderDetail
            {
                InboundOrderId = inboundOrder.InboundOrderId,
                InboundOrderItemId = item.InboundOrderItemId,
                ProductId = request.ProductId,
                StorageLocationId = receiving.StorageLocationId,
                ProductLot = layer,
                ReceivedQuantity = request.ActualReceivedQuantity,
                ConditionStatus = "GOOD",
                RecordedByUserId = currentUserId,
                RecordedAt = DateTime.UtcNow,
                Notes = "Đã ghi nhận thực nhận; chờ xếp vào vị trí kho."
            });
        }
        await _inboundRepository.SaveChangesAsync();
        await UpdatePurchaseOrderReceiptStatusAsync(inboundOrder.PurchaseOrderId);
        await _inboundRepository.SaveChangesAsync();
        await assignmentTransaction.CommitAsync();

        return inboundOrder.InboundOrderId;
    }

    public async Task<PurchaseOrderForInboundDto?> GetPurchaseOrderForInboundAsync(long purchaseOrderId)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId);

        if (po == null) return null;
        if (NormalizePurchaseOrderStatus(po.Status) is not ("CONFIRMED" or "PARTIALLY_RECEIVED"))
            return null;
        if (po.Supplier == null || !IsActive(po.Supplier.Status))
            return null;

        var inbounds = await _context.InboundOrders
            .Include(order => order.AssignedToUser)
            .Include(order => order.InboundOrderItems)
                .ThenInclude(item => item.InboundOrderDetails)
            .Where(order => order.PurchaseOrderId == purchaseOrderId && order.Status != "CANCELLED")
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

        var inboundItems = inbounds.SelectMany(order => order.InboundOrderItems).ToList();
        var snapshots = new Dictionary<long, PurchaseOrderLineReceiptSnapshot>();
        foreach (var detail in po.PurchaseOrderDetails)
        {
            snapshots[detail.ProductId] = PurchaseOrderReceiptRules.CalculateLine(
                detail.OrderedQuantity,
                inboundItems.Where(item => item.ProductId == detail.ProductId));
        }

        if (snapshots.Values.All(snapshot => snapshot.AvailableToPlanQuantity <= 0))
            return null;

        var previousReceiptCount = inbounds.Count(order =>
            PurchaseOrderReceiptRules.IsCompletedReceipt(order.Status));
        var dto = new PurchaseOrderForInboundDto
        {
            PurchaseOrderId = po.PurchaseOrderId,
            PurchaseOrderNumber = po.PurchaseOrderNumber,
            SupplierName = po.Supplier?.SupplierName ?? string.Empty,
            Status = NormalizePurchaseOrderStatus(po.Status),
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            IsFollowUpReceipt = previousReceiptCount > 0 ||
                NormalizePurchaseOrderStatus(po.Status) == "PARTIALLY_RECEIVED",
            PreviousReceiptCount = previousReceiptCount,
            TotalOrderedQuantity = snapshots.Values.Sum(snapshot => snapshot.OrderedQuantity),
            TotalAcceptedQuantity = snapshots.Values.Sum(snapshot => snapshot.AcceptedQuantity),
            TotalRejectedQuantity = snapshots.Values.Sum(snapshot => snapshot.RejectedQuantity),
            TotalActivePlannedQuantity = snapshots.Values.Sum(snapshot => snapshot.ActivePlannedQuantity),
            TotalAvailableToPlanQuantity = snapshots.Values.Sum(snapshot => snapshot.AvailableToPlanQuantity),
            PreviousInbounds = inbounds.Select(order =>
            {
                var details = order.InboundOrderItems.SelectMany(item => item.InboundOrderDetails).ToList();
                return new PurchaseOrderInboundHistoryDto
                {
                    InboundOrderId = order.InboundOrderId,
                    InboundOrderNumber = order.InboundOrderNumber,
                    ExpectedReceiptDate = order.ExpectedReceiptDate,
                    ActualReceiptAt = details.Count > 0 ? details.Max(detail => detail.RecordedAt) : null,
                    Status = GetInboundDisplayStatus(order),
                    AssignedToUserName = order.AssignedToUser?.FullName ?? "Chưa phân công",
                    ExpectedQuantity = order.InboundOrderItems.Sum(item => item.ExpectedQuantity),
                    AcceptedQuantity = details.Where(detail => detail.ConditionStatus == "GOOD")
                        .Sum(detail => detail.ReceivedQuantity),
                    RejectedQuantity = Math.Max(
                        order.InboundOrderItems.Sum(item => item.DamagedQuantity),
                        details.Where(detail => detail.ConditionStatus is "DAMAGED" or "REJECTED" or "QUARANTINED")
                            .Sum(detail => detail.ReceivedQuantity))
                };
            }).ToList()
        };

        foreach (var detail in po.PurchaseOrderDetails)
        {
            var snapshot = snapshots[detail.ProductId];

            dto.Items.Add(new PurchaseOrderItemForInboundDto
            {
                ProductId = detail.ProductId,
                ProductCode = detail.Product.ProductCode,
                ProductName = detail.Product.ProductName,
                UnitName = detail.Product.UnitOfMeasure?.UnitName ?? string.Empty,
                QuantityScale = detail.Product.UnitOfMeasure?.QuantityScale ?? 0,
                TrackLot = detail.Product.TrackLot,
                TrackExpiry = detail.Product.TrackExpiry,
                RotationMethod = detail.Product.RotationMethod?.Trim().ToUpperInvariant() ?? string.Empty,
                OrderedQuantity = detail.OrderedQuantity,
                InboundQuantity = snapshot.ActivePlannedQuantity + snapshot.AcceptedQuantity,
                AcceptedQuantity = snapshot.AcceptedQuantity,
                RejectedQuantity = snapshot.RejectedQuantity,
                ActivePlannedQuantity = snapshot.ActivePlannedQuantity,
                RemainingQuantity = snapshot.AvailableToPlanQuantity
            });
        }

        return dto;
    }

    public async Task<List<SourceOrderDropdownDto>> GetPendingPurchaseOrdersAsync()
    {
        return (await GetPurchaseOrderInboundSourcesAsync())
            .Select(source => new SourceOrderDropdownDto
            {
                Id = source.PurchaseOrderId,
                Name = $"{source.PurchaseOrderNumber} — {source.SupplierName}"
            })
            .ToList();
    }

    public async Task<List<PurchaseOrderInboundSourceDto>> GetPurchaseOrderInboundSourcesAsync()
    {
        var pos = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderDetails)
                .ThenInclude(detail => detail.Product)
            .Include(po => po.InboundOrders)
                .ThenInclude(order => order.InboundOrderItems)
                    .ThenInclude(item => item.InboundOrderDetails)
            .Where(po => po.Status == "CONFIRMED" || po.Status == "PARTIALLY_RECEIVED" ||
                         po.Status == "PARTIALLYRECEIVED")
            .Where(po => po.Supplier != null &&
                         (po.Supplier.Status == "ACTIVE" || po.Supplier.Status == "AVAILABLE"))
            .ToListAsync();

        var result = new List<PurchaseOrderInboundSourceDto>();
        foreach (var po in pos)
        {
            var activeInboundItems = po.InboundOrders
                .Where(order => order.Status != "CANCELLED")
                .SelectMany(order => order.InboundOrderItems)
                .ToList();
            var snapshots = po.PurchaseOrderDetails.Select(detail =>
                PurchaseOrderReceiptRules.CalculateLine(
                    detail.OrderedQuantity,
                    activeInboundItems.Where(item => item.ProductId == detail.ProductId)))
                .ToList();
            var available = snapshots.Sum(snapshot => snapshot.AvailableToPlanQuantity);
            if (available <= 0) continue;

            var receiptDetails = po.InboundOrders
                .Where(order => PurchaseOrderReceiptRules.IsCompletedReceipt(order.Status))
                .SelectMany(order => order.InboundOrderItems)
                .SelectMany(item => item.InboundOrderDetails)
                .ToList();
            var previousReceiptCount = po.InboundOrders.Count(order =>
                PurchaseOrderReceiptRules.IsCompletedReceipt(order.Status));

            result.Add(new PurchaseOrderInboundSourceDto
            {
                PurchaseOrderId = po.PurchaseOrderId,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                SupplierCode = po.Supplier?.SupplierCode ?? string.Empty,
                SupplierName = po.Supplier?.SupplierName ?? "Không xác định",
                Status = NormalizePurchaseOrderStatus(po.Status),
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                OrderedQuantity = snapshots.Sum(snapshot => snapshot.OrderedQuantity),
                AcceptedQuantity = snapshots.Sum(snapshot => snapshot.AcceptedQuantity),
                RejectedQuantity = snapshots.Sum(snapshot => snapshot.RejectedQuantity),
                ActivePlannedQuantity = snapshots.Sum(snapshot => snapshot.ActivePlannedQuantity),
                AvailableToPlanQuantity = available,
                ProductLineCount = po.PurchaseOrderDetails.Count,
                PreviousReceiptCount = previousReceiptCount,
                LatestReceiptAt = receiptDetails.Count > 0
                    ? receiptDetails.Max(detail => detail.RecordedAt)
                    : null,
                IsFollowUpReceipt = previousReceiptCount > 0 ||
                    NormalizePurchaseOrderStatus(po.Status) == "PARTIALLY_RECEIVED",
                SearchText = string.Join(' ', new[]
                {
                    po.PurchaseOrderNumber,
                    po.Supplier?.SupplierCode ?? string.Empty,
                    po.Supplier?.SupplierName ?? string.Empty,
                    string.Join(' ', po.PurchaseOrderDetails.Select(detail =>
                        $"{detail.Product.ProductCode} {detail.Product.ProductName}"))
                }).ToLowerInvariant()
            });
        }

        return result
            .OrderByDescending(source => source.IsFollowUpReceipt)
            .ThenBy(source => source.ExpectedDeliveryDate ?? DateOnly.MaxValue)
            .ThenBy(source => source.PurchaseOrderNumber)
            .ToList();
    }

    public async Task<List<SourceOrderDropdownDto>> GetReturnableSalesOrdersAsync()
    {
        var salesOrders = await _context.SalesOrders
            .AsNoTracking()
            .Include(so => so.Customer)
            .Include(so => so.SalesOrderDetails)
            .Include(so => so.InboundOrders)
                .ThenInclude(io => io.InboundOrderItems)
                    .ThenInclude(item => item.InboundOrderDetails)
            .Where(so => so.Status == "ISSUED"
                      || so.Status == "PARTIALLY_ISSUED"
                      || so.Status == "FULFILLED"
                      || so.Status == "PARTIALLY_FULFILLED"
                      || so.Status == "COMPLETED"
                      || so.Status == "CLOSED")
            .ToListAsync();

        return salesOrders
            .Where(so => so.SalesOrderDetails.Any(detail =>
            {
                var snapshot = SalesOrderReturnRules.CalculateLine(
                    detail.FulfilledQuantity,
                    so.InboundOrders
                    .SelectMany(io => io.InboundOrderItems)
                    .Where(item => item.ProductId == detail.ProductId));
                return snapshot.AvailableToPlanQuantity > 0;
            }))
            .OrderByDescending(so => so.ExpectedIssueDate ?? so.OrderDate)
            .ThenByDescending(so => so.SalesOrderId)
            .Select(so => new SourceOrderDropdownDto
            {
                Id = so.SalesOrderId,
                Name = $"{so.SalesOrderNumber} — {so.Customer.CustomerName}"
            })
            .ToList();
    }

    public async Task<List<AvailableWarehouseStaffDto>> GetAvailableWarehouseStaffAsync()
    {
        return await _context.Users
            .Where(u => u.Status == "ACTIVE" && u.Role.RoleCode == "WAREHOUSE_STAFF" &&
                        !u.InboundOrderAssignedToUsers.Any(o =>
                            o.Status != "CANCELLED" && o.Status != "PUTAWAY_COMPLETED" &&
                            (o.Status != "COMPLETED" ||
                             o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                                 .Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null))) &&
                        !u.OutboundOrderAssignedToUsers.Any(o =>
                            o.Status != "CANCELLED" && o.Status != "COMPLETED"))
            .OrderBy(u => u.FullName).ThenBy(u => u.Username)
            .Select(u => new AvailableWarehouseStaffDto
            {
                UserId = u.UserId,
                FullName = u.FullName
            })
            .ToListAsync();
    }

    public async Task<PurchaseOrderForInboundDto?> GetSalesOrderForInboundAsync(long salesOrderId)
    {
        var so = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.SalesOrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
            .Include(s => s.InboundOrders)
                .ThenInclude(io => io.InboundOrderItems)
                    .ThenInclude(item => item.InboundOrderDetails)
            .Include(s => s.InboundOrders)
                .ThenInclude(io => io.AssignedToUser)
            .FirstOrDefaultAsync(s => s.SalesOrderId == salesOrderId);

        if (so == null) return null;
        if (NormalizeSalesOrderStatus(so.Status) is not ("ISSUED" or "PARTIALLY_ISSUED"))
            return null;

        var returnItems = so.InboundOrders
            .SelectMany(order => order.InboundOrderItems)
            .ToList();
        var snapshots = so.SalesOrderDetails.ToDictionary(
            detail => detail.ProductId,
            detail => SalesOrderReturnRules.CalculateLine(
                detail.FulfilledQuantity,
                returnItems.Where(item => item.ProductId == detail.ProductId)));
        if (snapshots.Values.All(snapshot => snapshot.AvailableToPlanQuantity <= 0))
            return null;

        var previousReturns = so.InboundOrders
            .Where(order => string.Equals(order.SourceType?.Trim(), "SALES_RETURN", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(order.Status?.Trim(), "CANCELLED", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

        var dto = new PurchaseOrderForInboundDto
        {
            PurchaseOrderId = so.SalesOrderId,
            PurchaseOrderNumber = so.SalesOrderNumber,
            SupplierName = so.Customer.CustomerName,
            Status = NormalizeSalesOrderStatus(so.Status),
            ExpectedDeliveryDate = so.ExpectedIssueDate,
            IsFollowUpReceipt = previousReturns.Any(order =>
                PurchaseOrderReceiptRules.IsCompletedReceipt(order.Status)),
            PreviousReceiptCount = previousReturns.Count(order =>
                PurchaseOrderReceiptRules.IsCompletedReceipt(order.Status)),
            TotalOrderedQuantity = snapshots.Values.Sum(snapshot => snapshot.FulfilledQuantity),
            TotalAcceptedQuantity = snapshots.Values.Sum(snapshot => snapshot.AcceptedQuantity),
            TotalRejectedQuantity = snapshots.Values.Sum(snapshot => snapshot.RejectedQuantity),
            TotalActivePlannedQuantity = snapshots.Values.Sum(snapshot => snapshot.ActivePlannedQuantity),
            TotalAvailableToPlanQuantity = snapshots.Values.Sum(snapshot => snapshot.AvailableToPlanQuantity),
            PreviousInbounds = previousReturns.Select(order =>
            {
                var details = order.InboundOrderItems.SelectMany(item => item.InboundOrderDetails).ToList();
                return new PurchaseOrderInboundHistoryDto
                {
                    InboundOrderId = order.InboundOrderId,
                    InboundOrderNumber = order.InboundOrderNumber,
                    ExpectedReceiptDate = order.ExpectedReceiptDate,
                    ActualReceiptAt = details.Count > 0 ? details.Max(detail => detail.RecordedAt) : null,
                    Status = GetInboundDisplayStatus(order),
                    AssignedToUserName = order.AssignedToUser?.FullName ?? "Chưa phân công",
                    ExpectedQuantity = order.InboundOrderItems.Sum(item => item.ExpectedQuantity),
                    AcceptedQuantity = details
                        .Where(detail => detail.ConditionStatus == "GOOD")
                        .Sum(detail => detail.ReceivedQuantity),
                    RejectedQuantity = Math.Max(
                        order.InboundOrderItems.Sum(item => item.DamagedQuantity),
                        details.Where(detail => detail.ConditionStatus is "DAMAGED" or "REJECTED" or "QUARANTINED")
                            .Sum(detail => detail.ReceivedQuantity))
                };
            }).ToList(),
            Items = so.SalesOrderDetails.Select(d => 
            {
                var snapshot = snapshots[d.ProductId];

                return new PurchaseOrderItemForInboundDto
                {
                    ProductId = d.ProductId,
                    ProductCode = d.Product.ProductCode,
                    ProductName = d.Product.ProductName,
                    UnitName = d.Product.UnitOfMeasure?.UnitName ?? "",
                    QuantityScale = d.Product.UnitOfMeasure?.QuantityScale ?? 0,
                    TrackLot = d.Product.TrackLot,
                    TrackExpiry = d.Product.TrackExpiry,
                    RotationMethod = d.Product.RotationMethod?.Trim().ToUpperInvariant() ?? string.Empty,
                    OrderedQuantity = d.FulfilledQuantity,
                    InboundQuantity = snapshot.AcceptedQuantity + snapshot.ActivePlannedQuantity,
                    AcceptedQuantity = snapshot.AcceptedQuantity,
                    RejectedQuantity = snapshot.RejectedQuantity,
                    ActivePlannedQuantity = snapshot.ActivePlannedQuantity,
                    RemainingQuantity = snapshot.AvailableToPlanQuantity
                };
            }).Where(i => i.RemainingQuantity > 0).ToList()
        };

        return dto;
    }

    private static string NormalizePurchaseOrderStatus(string? status)
    {
        return (status ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "PARTIALLYRECEIVED" => "PARTIALLY_RECEIVED",
            "COMPLETED" or "CLOSED" => "RECEIVED",
            var value => value
        };
    }

    private static string NormalizeSalesOrderStatus(string? status)
    {
        return (status ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "PARTIALLY_FULFILLED" => "PARTIALLY_ISSUED",
            "FULFILLED" or "COMPLETED" or "CLOSED" => "ISSUED",
            var value => value
        };
    }

    public async Task UpdateInboundOrderAsync(long id, UpdateInboundOrderDto dto, long currentUserId)
    {
        await using var assignmentTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
                .ThenInclude(i => i.InboundOrderDetails)
            .FirstOrDefaultAsync(o => o.InboundOrderId == id);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        await EnsureInboundPlannerAuthorizedAsync(order.SourceType, currentUserId, "cập nhật phiếu nhập");
        if (order.Status is not ("DRAFT" or "ASSIGNED") || order.InboundOrderItems.Any(i => i.InboundOrderDetails.Count > 0))
            throw new Exception("Chỉ có thể sửa phiếu ở trạng thái Nháp hoặc Sẵn sàng, trước khi bắt đầu kiểm nhận.");

        if (order.SourceType == "SALES_RETURN" &&
            (string.IsNullOrWhiteSpace(dto.Notes) || dto.Notes.Trim().Length < 10))
            throw new ArgumentException("Hàng khách trả phải giữ lý do và căn cứ chấp thuận rõ ràng, tối thiểu 10 ký tự.");

        if ((dto.IsSubmit || order.Status == "ASSIGNED") && !dto.AssignedToUserId.HasValue)
            throw new Exception("Vui lòng phân công nhân viên kho trước khi chuyển phiếu sang trạng thái Sẵn sàng.");

        await EnsureWarehouseStaffAssigneeAsync(dto.AssignedToUserId, excludeInboundOrderId: order.InboundOrderId);
        order.Notes = dto.Notes?.Trim();

        var previousAssigneeId = order.AssignedToUserId;
        var submittedNow = order.Status == "DRAFT" && dto.IsSubmit;
        if (submittedNow)
        {
            order.Status = "ASSIGNED";
            order.ConfirmedAt = DateTime.UtcNow;
            order.ConfirmedByUserId = currentUserId;
        }
        order.AssignedToUserId = dto.AssignedToUserId;

        await _auditLogService.StageAsync(new AuditEventDto
        {
            UserId = currentUserId,
            ActionType = submittedNow ? "CONFIRM_INBOUND" : "UPDATE_INBOUND",
            EntityName = AuditEntities.InboundOrder,
            EntityId = order.InboundOrderId.ToString(),
            NewValues = new
            {
                order.Notes,
                order.Status,
                order.AssignedToUserId
            }
        });

        await _context.SaveChangesAsync();
        await assignmentTransaction.CommitAsync();

        if (order.Status == "ASSIGNED" && order.AssignedToUserId.HasValue &&
            (submittedNow || previousAssigneeId != order.AssignedToUserId))
        {
            await _notificationService.CreateNotificationAsync(new BMWMS.Business.DTOs.Notification.CreateNotificationDto
            {
                Title = "Bạn được giao một lệnh nhập kho mới",
                Message = $"Lệnh nhập kho {order.InboundOrderNumber} đã được giao cho bạn. Vui lòng kiểm tra.",
                NotificationType = "WORK_ASSIGNMENT",
                TargetUserId = order.AssignedToUserId.Value,
                ReferenceType = "INBOUND",
                ReferenceId = order.InboundOrderId.ToString()
            }, currentUserId);
        }
    }

    public async Task CancelInboundOrderAsync(long id, CancelInboundOrderDto dto, long currentUserId)
    {
        if (string.IsNullOrWhiteSpace(dto.CancellationReason))
            throw new ArgumentException("Vui lòng nhập lý do hủy phiếu.");

        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
                .ThenInclude(i => i.InboundOrderDetails)
            .FirstOrDefaultAsync(o => o.InboundOrderId == id);
        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        await EnsureInboundPlannerAuthorizedAsync(order.SourceType, currentUserId, "hủy phiếu nhập");
        if (order.Status is not ("DRAFT" or "ASSIGNED") || order.InboundOrderItems.Any(i => i.InboundOrderDetails.Count > 0))
            throw new Exception("Chỉ có thể hủy phiếu ở trạng thái Nháp hoặc Sẵn sàng, trước khi bắt đầu kiểm nhận.");

        order.Status = "CANCELLED";
        order.CancellationReason = dto.CancellationReason.Trim();
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledByUserId = currentUserId;

        await _auditLogService.StageAsync(new AuditEventDto
        {
            UserId = currentUserId,
            ActionType = "CANCEL_INBOUND",
            EntityName = AuditEntities.InboundOrder,
            EntityId = order.InboundOrderId.ToString(),
            OldValues = new { Status = "DRAFT_OR_READY" },
            NewValues = new { Status = "CANCELLED", Reason = order.CancellationReason }
        });

        await _context.SaveChangesAsync();
    }

    public async Task ConfirmInboundOrderAsync(long id, long currentUserId)
    {
        var order = await _inboundRepository.GetByIdAsync(id);
        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        await EnsureInboundPlannerAuthorizedAsync(order.SourceType, currentUserId, "chuyển phiếu nhập sang Sẵn sàng");
        if (order.Status != "DRAFT") throw new Exception("Chỉ có thể chuyển lệnh nhập kho sang Sẵn sàng khi phiếu đang ở trạng thái Nháp.");

        await EnsureWarehouseStaffAssigneeAsync(order.AssignedToUserId, excludeInboundOrderId: order.InboundOrderId);
        if (!order.AssignedToUserId.HasValue)
            throw new InvalidOperationException("Phải phân công một nhân viên kho trước khi chuyển phiếu sang trạng thái Sẵn sàng.");

        order.Status = "ASSIGNED";
        order.ConfirmedAt = DateTime.UtcNow;
        order.ConfirmedByUserId = currentUserId;

        await _auditLogService.StageAsync(new AuditEventDto
        {
            UserId = currentUserId,
            ActionType = "CONFIRM_INBOUND",
            EntityName = AuditEntities.InboundOrder,
            EntityId = order.InboundOrderId.ToString(),
            OldValues = new { Status = "DRAFT" },
            NewValues = new { Status = "READY", order.AssignedToUserId }
        });

        await _inboundRepository.UpdateAsync(order);
        await _inboundRepository.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(new BMWMS.Business.DTOs.Notification.CreateNotificationDto
        {
            Title = "Bạn được giao một phiếu nhập kho",
            Message = $"Phiếu nhập kho {order.InboundOrderNumber} đã sẵn sàng để kiểm nhận.",
            NotificationType = "WORK_ASSIGNMENT",
            TargetUserId = order.AssignedToUserId.Value,
            ReferenceType = "INBOUND",
            ReferenceId = order.InboundOrderId.ToString()
        }, currentUserId);
    }

    private async Task<long> ReceiveItemLegacyAsync(long inboundOrderId, ReceiveInboundItemDto dto, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho.");

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId);
        if (user?.Role?.RoleCode == "WAREHOUSE_STAFF" && order.AssignedToUserId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền nhận hàng cho lệnh này vì nó không được phân công cho bạn.");
        }
        if (order.Status != "ASSIGNED" && order.Status != "IN_PROGRESS") 
            throw new Exception("Chỉ có thể nhận hàng khi lệnh ở trạng thái ASSIGNED hoặc IN_PROGRESS.");

        var item = order.InboundOrderItems.FirstOrDefault(i => i.InboundOrderItemId == dto.InboundOrderItemId);
        if (item == null) throw new Exception("Không tìm thấy sản phẩm trong lệnh nhập kho.");

        // Calculate Accepted Quantity
        decimal acceptedQuantity = dto.DeliveredQuantity.GetValueOrDefault() - dto.RejectedQuantity.GetValueOrDefault();
        if (acceptedQuantity <= 0) throw new Exception("Số lượng chấp nhận phải lớn hơn 0.");

        // Update item quantities
        item.ReceivedQuantity += acceptedQuantity;
        item.DamagedQuantity += dto.RejectedQuantity.GetValueOrDefault();

        // Create or find ProductLot
        var legacyLotNumber = string.IsNullOrWhiteSpace(dto.LotNumber) ? $"NO-LOT-{item.ProductId}" : dto.LotNumber.Trim();
        var lot = await _context.ProductLots
            .FirstOrDefaultAsync(l => l.ProductId == item.ProductId && l.LotNumber == legacyLotNumber);

        if (lot == null)
        {
            lot = new BMWMS.Repository.Models.ProductLot
            {
                ProductId = item.ProductId,
                LotNumber = legacyLotNumber,
                ExpiryDate = dto.ExpiryDate,
                FirstReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "AVAILABLE",
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductLots.Add(lot);
            await _context.SaveChangesAsync(); // Save to get ProductLotId
        }

        // Find Staging Location (Assuming Warehouse has a default or staging location)
        // For simplicity, we find any location in this warehouse. In a real system, you'd filter by LocationType == "RECEIVING"
        var stagingLocation = await _context.StorageLocations
            .FirstOrDefaultAsync(l => l.WarehouseId == order.WarehouseId);

        if (stagingLocation == null) throw new Exception("Không tìm thấy vị trí lưu trữ nào trong kho này để nhận tạm.");

        // Create InboundOrderDetail for receipt
        var detail = new BMWMS.Repository.Models.InboundOrderDetail
        {
            InboundOrderItemId = item.InboundOrderItemId,
            InboundOrderId = inboundOrderId,
            ProductId = item.ProductId,
            StorageLocationId = stagingLocation.StorageLocationId,
            ProductLotId = lot.ProductLotId,
            ReceivedQuantity = acceptedQuantity,
            ConditionStatus = "GOOD",
            RecordedByUserId = currentUserId,
            RecordedAt = DateTime.UtcNow
        };
        _context.InboundOrderDetails.Add(detail);
        await _context.SaveChangesAsync(); // To get ID

        // Create InventoryTransaction for receipt
        var transaction = new BMWMS.Repository.Models.InventoryTransaction
        {
            TransactionType = "RECEIPT",
            ProductId = item.ProductId,
            StorageLocationId = stagingLocation.StorageLocationId,
            ProductLotId = lot.ProductLotId,
            OnHandDelta = acceptedQuantity,
            ReservedDelta = 0,
            InboundOrderDetailId = detail.InboundOrderDetailId,
            TransactionAt = DateTime.UtcNow,
            PerformedByUserId = currentUserId
        };
        _context.InventoryTransactions.Add(transaction);

        // Update Order Status
        if (order.Status == "ASSIGNED")
        {
            order.Status = "IN_PROGRESS";
        }

        // Check if fully received
        if (order.InboundOrderItems.All(i => i.ReceivedQuantity >= i.ExpectedQuantity))
        {
            order.Status = "COMPLETED";
        }

        await UpdatePurchaseOrderReceiptStatusAsync(order.PurchaseOrderId);
        await _context.SaveChangesAsync();
        return lot.ProductLotId;
    }

    private async Task ReceiveBatchLegacyAsync(long inboundOrderId, ReceiveBatchInboundDto dto, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho.");

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId);
        if (user?.Role?.RoleCode == "WAREHOUSE_STAFF" && order.AssignedToUserId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền nhận hàng cho lệnh này vì nó không được phân công cho bạn.");
        }
        if (order.Status != "ASSIGNED" && order.Status != "IN_PROGRESS") 
            throw new Exception("Chỉ có thể nhận hàng khi lệnh ở trạng thái ASSIGNED hoặc IN_PROGRESS.");

        var stagingLocation = await _context.StorageLocations
            .FirstOrDefaultAsync(l => l.WarehouseId == order.WarehouseId);

        if (stagingLocation == null) throw new Exception("Không tìm thấy vị trí lưu trữ nào trong kho này để nhận tạm.");

        foreach (var reqItem in dto.Items)
        {
            var item = order.InboundOrderItems.FirstOrDefault(i => i.InboundOrderItemId == reqItem.InboundOrderItemId);
            if (item == null) continue;

            decimal acceptedQuantity = reqItem.DeliveredQuantity.GetValueOrDefault() - reqItem.RejectedQuantity.GetValueOrDefault();
            if (acceptedQuantity <= 0) continue;

            item.ReceivedQuantity += acceptedQuantity;
            item.DamagedQuantity += reqItem.RejectedQuantity.GetValueOrDefault();

            var legacyLotNumber = string.IsNullOrWhiteSpace(reqItem.LotNumber) ? $"NO-LOT-{item.ProductId}" : reqItem.LotNumber.Trim();
            var lot = await _context.ProductLots
                .FirstOrDefaultAsync(l => l.ProductId == item.ProductId && l.LotNumber == legacyLotNumber);

            if (lot == null)
            {
                lot = new BMWMS.Repository.Models.ProductLot
                {
                    ProductId = item.ProductId,
                    LotNumber = legacyLotNumber,
                    ExpiryDate = reqItem.ExpiryDate,
                    FirstReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = "AVAILABLE",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductLots.Add(lot);
                await _context.SaveChangesAsync();
            }

            var detail = new BMWMS.Repository.Models.InboundOrderDetail
            {
                InboundOrderItemId = item.InboundOrderItemId,
                InboundOrderId = inboundOrderId,
                ProductId = item.ProductId,
                StorageLocationId = stagingLocation.StorageLocationId,
                ProductLotId = lot.ProductLotId,
                ReceivedQuantity = acceptedQuantity,
                ConditionStatus = "GOOD",
                RecordedByUserId = currentUserId,
                RecordedAt = DateTime.UtcNow
            };
            _context.InboundOrderDetails.Add(detail);
            await _context.SaveChangesAsync();

            var transaction = new BMWMS.Repository.Models.InventoryTransaction
            {
                TransactionType = "RECEIPT",
                ProductId = item.ProductId,
                StorageLocationId = stagingLocation.StorageLocationId,
                ProductLotId = lot.ProductLotId,
                OnHandDelta = acceptedQuantity,
                ReservedDelta = 0,
                InboundOrderDetailId = detail.InboundOrderDetailId,
                TransactionAt = DateTime.UtcNow,
                PerformedByUserId = currentUserId
            };
            _context.InventoryTransactions.Add(transaction);
        }

        if (order.Status == "ASSIGNED")
        {
            order.Status = "IN_PROGRESS";
        }

        // Check if fully received
        if (order.InboundOrderItems.All(i => i.ReceivedQuantity >= i.ExpectedQuantity))
        {
            order.Status = "COMPLETED";
        }

        await UpdatePurchaseOrderReceiptStatusAsync(order.PurchaseOrderId);
        await _context.SaveChangesAsync();
    }

    private async Task UpdatePurchaseOrderReceiptStatusAsync(long? purchaseOrderId)
    {
        if (!purchaseOrderId.HasValue)
            return;

        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.PurchaseOrderDetails)
            .Include(po => po.InboundOrders)
                .ThenInclude(io => io.InboundOrderItems)
                    .ThenInclude(item => item.InboundOrderDetails)
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId.Value);

        if (purchaseOrder == null || NormalizePurchaseOrderStatus(purchaseOrder.Status) is "CANCELLED" or "CLOSED")
            return;

        var activeInboundItems = purchaseOrder.InboundOrders
            .Where(io => io.Status == "COMPLETED")
            .SelectMany(io => io.InboundOrderItems)
            .ToList();

        var hasReceivedQuantity = activeInboundItems.Any(item =>
            item.InboundOrderDetails.Any(receipt => receipt.ConditionStatus == "GOOD" && receipt.ReceivedQuantity > 0));
        var isFullyReceived = purchaseOrder.PurchaseOrderDetails.Count > 0
            && purchaseOrder.PurchaseOrderDetails.All(detail =>
                activeInboundItems
                    .Where(item => item.ProductId == detail.ProductId)
                    .SelectMany(item => item.InboundOrderDetails)
                    .Where(receipt => receipt.ConditionStatus == "GOOD")
                    .Sum(receipt => receipt.ReceivedQuantity) >= detail.OrderedQuantity);

        purchaseOrder.Status = isFullyReceived
            ? "COMPLETED"
            : hasReceivedQuantity
                ? "PENDING_RECEIPT_REVIEW"
                : "CONFIRMED";
        purchaseOrder.UpdatedAt = DateTime.UtcNow;
    }

    private async Task PutawayBatchLegacyAsync(long inboundOrderId, List<PutawayInboundItemDto> dtos, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho.");

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId);
        if (user?.Role?.RoleCode == "WAREHOUSE_STAFF" && order.AssignedToUserId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xếp vị trí cho lệnh này vì nó không được phân công cho bạn.");
        }

        decimal totalPutawayCurrentBatch = 0;

        foreach (var dto in dtos)
        {
            var item = order.InboundOrderItems.FirstOrDefault(i => i.InboundOrderItemId == dto.InboundOrderItemId);
            if (item == null) continue;

            var transaction = new BMWMS.Repository.Models.InventoryTransaction
            {
                TransactionType = "PUTAWAY",
                ProductId = item.ProductId,
                StorageLocationId = dto.StorageLocationId,
                ProductLotId = dto.ProductLotId,
                OnHandDelta = dto.PutawayQuantity,
                ReservedDelta = 0,
                InboundOrderDetailId = null,
                TransactionAt = DateTime.UtcNow,
                PerformedByUserId = currentUserId
            };
            _context.InventoryTransactions.Add(transaction);

            // Cập nhật tồn kho (Inventory)
            var inventory = await _context.Inventories.FirstOrDefaultAsync(inv => 
                inv.ProductId == item.ProductId && 
                inv.StorageLocationId == dto.StorageLocationId && 
                inv.ProductLotId == dto.ProductLotId);
            
            if (inventory == null)
            {
                inventory = new BMWMS.Repository.Models.Inventory
                {
                    ProductId = item.ProductId,
                    StorageLocationId = dto.StorageLocationId,
                    ProductLotId = dto.ProductLotId,
                    OnHandQuantity = dto.PutawayQuantity,
                    AvailableQuantity = dto.PutawayQuantity,
                    ReservedQuantity = 0,
                    LastUpdatedAt = DateTime.UtcNow
                };
                _context.Inventories.Add(inventory);
            }
            else
            {
                inventory.OnHandQuantity += dto.PutawayQuantity;
                inventory.AvailableQuantity += dto.PutawayQuantity;
                inventory.LastUpdatedAt = DateTime.UtcNow;
            }

            totalPutawayCurrentBatch += dto.PutawayQuantity;
        }

        var totalPutaway = await _context.InventoryTransactions
            .Where(t => t.TransactionType == "PUTAWAY" && _context.InboundOrderItems.Where(i => i.InboundOrderId == inboundOrderId).Select(i => i.ProductId).Contains(t.ProductId))
            .SumAsync(t => t.OnHandDelta);

        var totalExpected = order.InboundOrderItems.Sum(i => i.ExpectedQuantity);

        if (totalPutaway + totalPutawayCurrentBatch >= totalExpected)
        {
            order.Status = "PUTAWAY_COMPLETED";
        }

        await _context.SaveChangesAsync();
    }

    public async Task<long> ReceiveItemAsync(long inboundOrderId, ReceiveInboundItemDto dto, long currentUserId)
    {
        var lotIds = await ReceiveBatchCoreAsync(inboundOrderId, new[] { dto }, currentUserId);
        return lotIds.SingleOrDefault();
    }

    public async Task ReceiveBatchAsync(long inboundOrderId, ReceiveBatchInboundDto dto, long currentUserId)
    {
        await ReceiveBatchCoreAsync(inboundOrderId, dto.Items, currentUserId);
    }

    public async Task CompleteReceiptAsync(long inboundOrderId, CompleteInboundReceiptDto dto, long currentUserId)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.InboundOrderDetails)
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId)
                ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");

            await EnsureAssignedUserAsync(order.AssignedToUserId, currentUserId, "hoàn tất kiểm nhận");
            if (order.Status is not ("ASSIGNED" or "IN_PROGRESS"))
                throw new InvalidOperationException("Chỉ được hoàn tất kiểm nhận khi phiếu ở trạng thái Sẵn sàng hoặc Đang nhận.");

            var decisions = (dto.Decisions ?? new List<InboundReceiptDecisionDto>())
                .GroupBy(decision => decision.InboundOrderItemId)
                .ToDictionary(group => group.Key, group => group.Last());
            var orderItemIds = order.InboundOrderItems.Select(item => item.InboundOrderItemId).ToHashSet();
            if (decisions.Keys.Any(itemId => !orderItemIds.Contains(itemId)))
                throw new ArgumentException("Danh sách xử lý chênh lệch chứa dòng không thuộc phiếu nhập kho.");

            var unconfirmedItems = order.InboundOrderItems
                .Where(item => item.ReceivedQuantity < item.ExpectedQuantity &&
                               (!decisions.TryGetValue(item.InboundOrderItemId, out var decision) ||
                                !decision.CloseAsShort || string.IsNullOrWhiteSpace(decision.Reason)))
                .ToList();
            if (unconfirmedItems.Count > 0)
            {
                var productList = string.Join(", ", unconfirmedItems.Select(item =>
                    $"{item.Product.ProductName} ({item.Product.ProductCode})"));
                throw new InvalidOperationException(
                    $"Chưa kiểm nhận đủ hoặc chưa nhập lý do chốt thiếu cho: {productList}.");
            }

            if (order.InboundOrderItems.Any(item => item.ReceivedQuantity <= 0 &&
                    (!decisions.TryGetValue(item.InboundOrderItemId, out var decision) || !decision.CloseAsShort)))
                throw new InvalidOperationException("Mỗi mặt hàng phải có kết quả kiểm nhận hoặc quyết định chốt thiếu rõ ràng.");

            foreach (var item in order.InboundOrderItems)
            {
                item.ShortageQuantity = Math.Max(0, item.ExpectedQuantity - item.ReceivedQuantity);
                if (item.ShortageQuantity > 0 && decisions.TryGetValue(item.InboundOrderItemId, out var decision))
                {
                    item.Notes = AppendReceiptNote(
                        item.Notes,
                        $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC] Chốt thiếu: {item.ShortageQuantity}; {decision.Reason?.Trim()}");
                }
            }

            order.Status = "COMPLETED";
            await UpdatePurchaseOrderReceiptStatusAsync(order.PurchaseOrderId);
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = currentUserId,
                ActionType = "COMPLETE_INBOUND_RECEIPT",
                EntityName = AuditEntities.InboundOrder,
                EntityId = order.InboundOrderId.ToString(),
                NewValues = new
                {
                    order.InboundOrderNumber,
                    Status = "RECEIVED",
                    Notes = dto.Notes,
                    Decisions = decisions.Values.Select(decision => new
                    {
                        decision.InboundOrderItemId,
                        decision.CloseAsShort,
                        Reason = decision.Reason?.Trim()
                    })
                }
            });
            await _context.SaveChangesAsync();

            var coordinatorRole = order.SourceType == "SALES_RETURN" ? "SALES_STAFF" : "PURCHASING_STAFF";
            var targetUsers = await _context.Users
                .Where(u => u.Status == "ACTIVE" &&
                            (u.UserId == order.CreatedByUserId ||
                             u.Role.RoleCode == coordinatorRole ||
                             u.Role.RoleCode == "WAREHOUSE_MANAGER") &&
                            u.UserId != currentUserId)
                .Select(u => u.UserId)
                .Distinct()
                .ToListAsync();
            var sourceDescription = order.SourceType == "SALES_RETURN"
                ? "hàng khách trả"
                : "hàng mua từ nhà cung cấp";
            foreach (var targetUserId in targetUsers)
            {
                await _notificationService.CreateNotificationAsync(new BMWMS.Business.DTOs.Notification.CreateNotificationDto
                {
                    Title = order.SourceType == "SALES_RETURN"
                        ? "Đã kiểm nhận hàng khách trả"
                        : "Phiếu nhập đã hoàn tất kiểm nhận",
                    Message = $"Phiếu {order.InboundOrderNumber} ({sourceDescription}) đã chốt số lượng chấp nhận, thiếu và từ chối; hàng được chấp nhận đang chờ xếp vị trí.",
                    NotificationType = "SYSTEM",
                    TargetUserId = targetUserId,
                    ReferenceType = "INBOUND",
                    ReferenceId = order.InboundOrderId.ToString()
                }, currentUserId);
            }
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    private async Task<List<long>> ReceiveBatchCoreAsync(
        long inboundOrderId,
        IEnumerable<ReceiveInboundItemDto> requests,
        long currentUserId)
    {
        var allRequests = requests?.ToList() ?? new List<ReceiveInboundItemDto>();
        if (allRequests.GroupBy(x => x.InboundOrderItemId).Any(group => group.Count() > 1))
            throw new ArgumentException("Mỗi dòng hàng chỉ được gửi một lần trong một lần lưu kiểm nhận.");

        var incompleteRows = allRequests.Where(x => !x.DeliveredQuantity.HasValue &&
            (x.AcceptedQuantity.HasValue || x.RejectedQuantity.HasValue ||
             !string.IsNullOrWhiteSpace(x.LotNumber) || x.ManufactureDate.HasValue || x.ExpiryDate.HasValue ||
             !string.IsNullOrWhiteSpace(x.ConditionNotes))).ToList();
        if (incompleteRows.Count > 0)
            throw new ArgumentException("Dòng đã nhập thông tin phải có số lượng thực giao.");

        var requestItems = allRequests.Where(x => x.DeliveredQuantity.HasValue).ToList();
        if (requestItems.Count == 0)
            throw new ArgumentException("Phải có ít nhất một dòng hàng có số lượng thực giao lớn hơn 0.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId)
                ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");

            await EnsureAssignedUserAsync(order.AssignedToUserId, currentUserId, "nhận hàng");
            if (order.Status is not ("ASSIGNED" or "IN_PROGRESS"))
                throw new InvalidOperationException("Chỉ được nhận hàng khi phiếu ở trạng thái Sẵn sàng hoặc Đang nhận.");

            var receiving = await _context.StorageLocations.FirstOrDefaultAsync(l =>
                l.WarehouseId == order.WarehouseId && l.LocationType == "RECEIVING" &&
                l.Status != "BLOCKED" && l.Status != "INACTIVE")
                ?? await _context.StorageLocations.FirstOrDefaultAsync(l =>
                    l.WarehouseId == order.WarehouseId && l.LocationType == "STAGING" &&
                    l.Status != "BLOCKED" && l.Status != "INACTIVE")
                ?? throw new InvalidOperationException("Kho chưa cấu hình vị trí RECEIVING/STAGING đang hoạt động.");

            var lotIds = new List<long>();
            var createdLayers = new List<BMWMS.Repository.Models.ProductLot>();
            foreach (var request in requestItems)
            {
                var item = order.InboundOrderItems.SingleOrDefault(i => i.InboundOrderItemId == request.InboundOrderItemId)
                    ?? throw new ArgumentException("Dòng hàng không thuộc phiếu nhập kho.");
                var delivered = request.DeliveredQuantity.GetValueOrDefault();
                if (delivered <= 0)
                    throw new ArgumentException($"Số lượng thực giao của {item.Product.ProductCode} phải lớn hơn 0.");
                if (!request.AcceptedQuantity.HasValue || !request.RejectedQuantity.HasValue)
                    throw new ArgumentException($"Phải nhập đủ số lượng chấp nhận và từ chối cho {item.Product.ProductCode}.");
                var accepted = request.AcceptedQuantity.Value;
                var rejected = request.RejectedQuantity.Value;
                if (accepted < 0 || rejected < 0 || accepted + rejected != delivered)
                    throw new ArgumentException($"Tổng số lượng chấp nhận và từ chối của {item.Product.ProductCode} phải đúng bằng số lượng thực giao.");
                if (rejected > 0 && string.IsNullOrWhiteSpace(request.ConditionNotes))
                    throw new ArgumentException($"Phải ghi rõ tình trạng hoặc lý do từ chối hàng của {item.Product.ProductCode}.");
                if (!IsActive(item.Product.Status))
                    throw new InvalidOperationException($"Sản phẩm {item.Product.ProductCode} đang ngừng hoạt động.");
                QuantityRules.EnsureValid(item.Product, delivered, "Số lượng thực giao");
                if (accepted > 0) QuantityRules.EnsureValid(item.Product, accepted, "Số lượng chấp nhận");
                if (rejected > 0) QuantityRules.EnsureValid(item.Product, rejected, "Số lượng từ chối");
                if (item.ReceivedQuantity + delivered > item.ExpectedQuantity)
                    throw new ArgumentException($"Số lượng nhận của {item.Product.ProductCode} vượt số lượng dự kiến còn lại.");
                var requiresExpiry = item.Product.TrackExpiry ||
                    string.Equals(item.Product.RotationMethod?.Trim(), "FEFO", StringComparison.OrdinalIgnoreCase);
                if (accepted > 0 && requiresExpiry &&
                    (!request.ExpiryDate.HasValue || request.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow)))
                    throw new ArgumentException($"Sản phẩm FEFO/có quản lý hạn dùng {item.Product.ProductCode} phải có hạn dùng trong tương lai.");
                if (request.ManufactureDate.HasValue && request.ManufactureDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
                    throw new ArgumentException($"Ngày sản xuất của {item.Product.ProductCode} không được nằm trong tương lai.");
                if (request.ManufactureDate.HasValue && request.ExpiryDate.HasValue && request.ManufactureDate > request.ExpiryDate)
                    throw new ArgumentException($"Ngày sản xuất của {item.Product.ProductCode} không được sau hạn dùng.");

                item.ReceivedQuantity += delivered;
                item.DamagedQuantity += rejected;
                item.ShortageQuantity = Math.Max(0, item.ExpectedQuantity - item.ReceivedQuantity);

                var supplierLotNote = string.IsNullOrWhiteSpace(request.LotNumber)
                    ? null
                    : $"Lô tham chiếu NCC: {request.LotNumber.Trim()}. ";
                var conditionNote = supplierLotNote + request.ConditionNotes?.Trim();

                if (accepted > 0)
                {
                    var layerCode = $"RCV-{order.InboundOrderId}-{item.InboundOrderItemId}-{Guid.NewGuid():N}";
                    var lot = new BMWMS.Repository.Models.ProductLot
                    {
                        ProductId = item.ProductId,
                        LotNumber = layerCode,
                        ManufactureDate = request.ManufactureDate,
                        ExpiryDate = request.ExpiryDate,
                        FirstReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        Status = "AVAILABLE",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ProductLots.Add(lot);
                    createdLayers.Add(lot);
                    _context.InboundOrderDetails.Add(new BMWMS.Repository.Models.InboundOrderDetail
                    {
                        InboundOrderItemId = item.InboundOrderItemId,
                        InboundOrderId = order.InboundOrderId,
                        ProductId = item.ProductId,
                        StorageLocationId = receiving.StorageLocationId,
                        ProductLot = lot,
                        ReceivedQuantity = accepted,
                        ConditionStatus = "GOOD",
                        RecordedByUserId = currentUserId,
                        RecordedAt = DateTime.UtcNow,
                        Notes = string.IsNullOrWhiteSpace(conditionNote)
                            ? "Hàng được chấp nhận; chờ xếp vào bin thực tế."
                            : conditionNote
                    });
                }

                if (rejected > 0)
                {
                    var rejectionEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC] Từ chối tại cửa nhận: {rejected}; {conditionNote}";
                    item.Notes = AppendReceiptNote(item.Notes, rejectionEntry);
                }

            }

            order.Status = "IN_PROGRESS";

            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = currentUserId,
                ActionType = "SAVE_INBOUND_RECEIPT",
                EntityName = AuditEntities.InboundOrder,
                EntityId = order.InboundOrderId.ToString(),
                NewValues = requestItems.Select(request => new
                {
                    request.InboundOrderItemId,
                    request.DeliveredQuantity,
                    request.AcceptedQuantity,
                    request.RejectedQuantity,
                    request.ConditionNotes,
                    request.ExpiryDate
                })
            });

            await _context.SaveChangesAsync();
            lotIds.AddRange(createdLayers.Select(layer => layer.ProductLotId));
            await dbTransaction.CommitAsync();
            return lotIds;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task PutawayBatchAsync(long inboundOrderId, PutawayBatchRequestDto request, long currentUserId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        var dtos = request.Items ?? new List<PutawayInboundItemDto>();
        if (dtos.Count == 0 || dtos.Any(x => x.PutawayQuantity <= 0))
            throw new ArgumentException("Danh sách xếp hàng vào vị trí không hợp lệ.");
        if (request.CapacityWarningReason?.Trim().Length > 500)
            throw new ArgumentException("Lý do xác nhận cảnh báo không được vượt quá 500 ký tự.");
        if (dtos.GroupBy(x => new { x.InboundOrderItemId, x.ProductLotId, x.StorageLocationId })
            .Any(group => group.Count() > 1))
            throw new ArgumentException("Không được lặp cùng một sản phẩm, đợt nhận và vị trí đích trong một lần phân bổ.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.InboundOrderDetails)
                        .ThenInclude(d => d.InventoryTransaction)
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId)
                ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");

            await EnsureAssignedUserAsync(order.AssignedToUserId, currentUserId, "xếp hàng vào vị trí");
            if (order.Status != "COMPLETED")
                throw new InvalidOperationException("Chỉ được xếp hàng vào vị trí sau khi đã hoàn tất ghi nhận thực nhận.");

            var waitingByReceipt = order.InboundOrderItems
                .SelectMany(item => item.InboundOrderDetails
                    .Where(detail => detail.ConditionStatus == "GOOD" && detail.InventoryTransaction == null)
                    .Select(detail => new
                    {
                        item.InboundOrderItemId,
                        detail.ProductLotId,
                        detail.ReceivedQuantity
                    }))
                .GroupBy(row => new { row.InboundOrderItemId, row.ProductLotId })
                .ToDictionary(group => group.Key, group => group.Sum(row => row.ReceivedQuantity));
            var submittedByReceipt = dtos
                .GroupBy(row => new { row.InboundOrderItemId, row.ProductLotId })
                .ToDictionary(group => group.Key, group => group.Sum(row => row.PutawayQuantity));
            if (waitingByReceipt.Count == 0)
                throw new InvalidOperationException("Phiếu không còn hàng chờ xếp vị trí.");
            if (waitingByReceipt.Count != submittedByReceipt.Count ||
                waitingByReceipt.Any(expected => !submittedByReceipt.TryGetValue(expected.Key, out var quantity) ||
                                                  quantity != expected.Value))
                throw new ArgumentException("Phải phân bổ đúng toàn bộ số lượng đang chờ cất của tất cả vật tư trong phiếu.");

            var itemsById = order.InboundOrderItems.ToDictionary(item => item.InboundOrderItemId);
            var destinationIds = dtos.Select(allocation => allocation.StorageLocationId).Distinct().ToList();
            var destinations = await _context.StorageLocations
                .Include(location => location.StorageRack)
                    .ThenInclude(rack => rack!.WarehouseZone)
                .Where(location => destinationIds.Contains(location.StorageLocationId))
                .ToDictionaryAsync(location => location.StorageLocationId);
            if (destinations.Count != destinationIds.Count)
                throw new ArgumentException("Một hoặc nhiều vị trí đích không còn tồn tại.");

            foreach (var allocation in dtos)
            {
                if (!itemsById.TryGetValue(allocation.InboundOrderItemId, out var item))
                    throw new ArgumentException("Dòng hàng không thuộc phiếu nhập kho.");
                QuantityRules.EnsureValid(item.Product, allocation.PutawayQuantity, "Số lượng xếp vị trí");
                var destination = destinations[allocation.StorageLocationId];
                if (destination.WarehouseId != order.WarehouseId || destination.LocationType != "BIN" ||
                    !destination.IsPutawayAllowed || !IsActive(destination.Status))
                    throw new InvalidOperationException($"Vị trí {destination.LocationCode} không hợp lệ để xếp hàng.");
                if (destination.StorageRack != null &&
                    (!IsActive(destination.StorageRack.Status) ||
                     !IsActive(destination.StorageRack.WarehouseZone.Status)))
                    throw new InvalidOperationException($"Khu hoặc kệ chứa vị trí {destination.LocationCode} đang bị khóa/ngừng hoạt động.");

                var stagingQuantity = item.InboundOrderDetails
                    .Where(detail => detail.ProductLotId == allocation.ProductLotId &&
                                     detail.ConditionStatus == "GOOD" && detail.InventoryTransaction == null)
                    .Sum(detail => detail.ReceivedQuantity);
                if (stagingQuantity < allocation.PutawayQuantity)
                    throw new InvalidOperationException("Số lượng phân bổ vượt số hàng thực nhận đang chờ xếp vị trí.");
            }

            var productIds = itemsById.Values.Select(item => item.ProductId).Distinct().ToList();
            var recommendationRules = await _context.ProductFixedLocations
                .AsNoTracking()
                .Include(rule => rule.StorageLocation)
                    .ThenInclude(location => location.StorageRack)
                .Where(rule => productIds.Contains(rule.ProductId) && rule.IsActive &&
                               rule.StorageLocation.WarehouseId == order.WarehouseId)
                .ToListAsync();
            var recommendationOverrides = dtos
                .Where(allocation =>
                {
                    var productId = itemsById[allocation.InboundOrderItemId].ProductId;
                    var productRules = recommendationRules.Where(rule => rule.ProductId == productId).ToList();
                    if (productRules.Count == 0) return false;
                    var destination = destinations[allocation.StorageLocationId];
                    return !IsRecommendedLocation(destination, productRules);
                })
                .Select(allocation => new
                {
                    ProductCode = itemsById[allocation.InboundOrderItemId].Product.ProductCode,
                    LocationCode = destinations[allocation.StorageLocationId].LocationCode
                })
                .Distinct()
                .ToList();

            IReadOnlyDictionary<long, LocationCapacityEvaluationDto> capacityEvaluations =
                new Dictionary<long, LocationCapacityEvaluationDto>();
            if (_capacityOptions.Enabled)
            {
                capacityEvaluations = await _capacityEvaluationService.EvaluateAsync(
                    dtos.Select(allocation => new CapacityAllocationDto
                    {
                        StorageLocationId = allocation.StorageLocationId,
                        ProductId = itemsById[allocation.InboundOrderItemId].ProductId,
                        Quantity = allocation.PutawayQuantity
                    }).ToList(),
                    acquireLocationLocks: true);

                var exceeded = capacityEvaluations.Values
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
                        $"Không đủ sức chứa tại vị trí: {string.Join(", ", exceeded)}. Vui lòng chia số lượng hoặc chọn vị trí khác.");

                var incomplete = capacityEvaluations.Values
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

                var requiresAcknowledgement = incomplete.Count > 0 || recommendationOverrides.Count > 0;
                if (requiresAcknowledgement &&
                    (!request.AcknowledgeCapacityWarning || string.IsNullOrWhiteSpace(request.CapacityWarningReason)))
                    throw new ArgumentException(
                        "Vui lòng xác nhận cảnh báo và ghi lý do khi sức chứa chưa đủ dữ liệu hoặc chọn ngoài vị trí khuyến nghị.");
            }

            foreach (var allocation in dtos)
            {
                var item = itemsById[allocation.InboundOrderItemId];
                var destination = destinations[allocation.StorageLocationId];
                var remaining = allocation.PutawayQuantity;
                var stagingDetails = item.InboundOrderDetails
                    .Where(detail => detail.ProductLotId == allocation.ProductLotId &&
                                     detail.ConditionStatus == "GOOD" && detail.InventoryTransaction == null)
                    .OrderBy(detail => detail.RecordedAt)
                    .ToList();

                foreach (var staging in stagingDetails.Where(_ => remaining > 0))
                {
                    var moved = Math.Min(staging.ReceivedQuantity, remaining);
                    BMWMS.Repository.Models.InboundOrderDetail postedDetail;
                    if (moved == staging.ReceivedQuantity)
                    {
                        staging.StorageLocationId = destination.StorageLocationId;
                        staging.Notes = "Đã xếp vào vị trí thực tế.";
                        postedDetail = staging;
                    }
                    else
                    {
                        staging.ReceivedQuantity -= moved;
                        postedDetail = new BMWMS.Repository.Models.InboundOrderDetail
                        {
                            InboundOrderItemId = item.InboundOrderItemId,
                            InboundOrderId = order.InboundOrderId,
                            ProductId = item.ProductId,
                            StorageLocationId = destination.StorageLocationId,
                            ProductLotId = staging.ProductLotId,
                            ReceivedQuantity = moved,
                            ConditionStatus = "GOOD",
                            RecordedByUserId = currentUserId,
                            RecordedAt = DateTime.UtcNow,
                            Notes = "Xếp một đợt nhận vào nhiều vị trí."
                        };
                        _context.InboundOrderDetails.Add(postedDetail);
                        item.InboundOrderDetails.Add(postedDetail);
                    }

                    var inventoryTransaction = new BMWMS.Repository.Models.InventoryTransaction
                    {
                        TransactionType = "INBOUND",
                        ProductId = item.ProductId,
                        StorageLocationId = destination.StorageLocationId,
                        ProductLotId = staging.ProductLotId,
                        OnHandDelta = moved,
                        ReservedDelta = 0,
                        InboundOrderDetail = postedDetail,
                        PerformedByUserId = currentUserId,
                        TransactionAt = DateTime.UtcNow,
                        Notes = $"Xếp hàng từ {order.InboundOrderNumber} vào {destination.LocationCode}."
                    };
                    postedDetail.InventoryTransaction = inventoryTransaction;
                    _context.InventoryTransactions.Add(inventoryTransaction);
                    remaining -= moved;
                }
            }

            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = currentUserId,
                ActionType = "PUTAWAY_INBOUND",
                EntityName = "InboundPutaway",
                EntityId = order.InboundOrderId.ToString(),
                NewValues = new
                {
                    Allocations = dtos.Select(allocation => new
                    {
                        allocation.InboundOrderItemId,
                        allocation.ProductLotId,
                        allocation.StorageLocationId,
                        allocation.PutawayQuantity
                    }),
                    CapacityEnabled = _capacityOptions.Enabled,
                    CapacityEvaluations = capacityEvaluations.Values,
                    RecommendationOverrides = recommendationOverrides,
                    WarningAcknowledged = request.AcknowledgeCapacityWarning,
                    WarningReason = request.CapacityWarningReason?.Trim()
                }
            });
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    private async Task EnsureAssignedUserAsync(long? assignedToUserId, long currentUserId, string action)
    {
        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == currentUserId);
        if (user?.Status != "ACTIVE" || user.Role?.RoleCode != "WAREHOUSE_STAFF")
            throw new UnauthorizedAccessException($"Chỉ nhân viên kho đang hoạt động mới được {action}.");
        if (assignedToUserId != currentUserId)
            throw new UnauthorizedAccessException($"Bạn không được phân công để {action} cho phiếu này.");
    }

    private async Task EnsureWarehouseStaffCreatorAsync(long currentUserId)
    {
        var isWarehouseStaff = await _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.UserId == currentUserId && user.Status == "ACTIVE" &&
                              user.Role.RoleCode == "WAREHOUSE_STAFF");
        if (!isWarehouseStaff)
            throw new UnauthorizedAccessException("Chỉ nhân viên kho đang hoạt động mới được tạo phiếu nhập kho.");
    }

    private async Task EnsureInboundPlannerAuthorizedAsync(string? sourceType, long currentUserId, string action)
    {
        var actor = await _context.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.UserId == currentUserId)
            ?? throw new UnauthorizedAccessException("Không xác định được người thực hiện.");
        if (!string.Equals(actor.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Tài khoản thực hiện đang ngừng hoạt động.");

        var role = actor.Role?.RoleCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var normalizedSource = sourceType?.Trim().ToUpperInvariant() ?? string.Empty;
        var permitted = role is "SYSTEM_ADMIN" or "WAREHOUSE_MANAGER" ||
                        (role == "PURCHASING_STAFF" && normalizedSource == "PURCHASE_ORDER") ||
                        (role == "SALES_STAFF" && normalizedSource == "SALES_RETURN");
        if (!permitted)
        {
            var owner = normalizedSource == "SALES_RETURN" ? "Nhân viên bán hàng" : "Nhân viên mua hàng";
            throw new UnauthorizedAccessException($"Chỉ {owner} hoặc Quản lý kho được {action} cho loại phiếu này.");
        }
    }

    private async Task EnsureWarehouseStaffAssigneeAsync(long? assignedToUserId, long? excludeInboundOrderId = null)
    {
        if (!assignedToUserId.HasValue) return;

        var isWarehouseStaff = await _context.Users.AnyAsync(u =>
            u.UserId == assignedToUserId.Value && u.Status == "ACTIVE" &&
            u.Role.RoleCode == "WAREHOUSE_STAFF" &&
            !u.InboundOrderAssignedToUsers.Any(o =>
                o.InboundOrderId != excludeInboundOrderId &&
                o.Status != "CANCELLED" && o.Status != "PUTAWAY_COMPLETED" &&
                (o.Status != "COMPLETED" ||
                 o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                     .Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null))) &&
            !u.OutboundOrderAssignedToUsers.Any(o =>
                o.Status != "CANCELLED" && o.Status != "COMPLETED"));
        if (!isWarehouseStaff)
            throw new ArgumentException("Nhân viên không thuộc vai trò Nhân viên kho, đang bị khóa hoặc đang phụ trách một phiếu nhập/xuất khác.");
    }

    private async Task ValidateQuantitiesAsync(IEnumerable<(long ProductId, decimal Quantity, string FieldName)> values)
    {
        var lines = values.ToList();
        var productIds = lines.Select(x => x.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.UnitOfMeasure)
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        foreach (var line in lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product) || !IsActive(product.Status))
                throw new ArgumentException($"Sản phẩm ID {line.ProductId} không tồn tại hoặc đã ngừng hoạt động.");
            QuantityRules.EnsureValid(product, line.Quantity, line.FieldName);
        }
    }

    public async Task<List<PutawayLocationDto>> GetPutawayLocationsAsync(
        long warehouseId,
        long productId,
        decimal putawayQuantity = 0)
    {
        if (warehouseId <= 0 || productId <= 0 || putawayQuantity < 0)
            throw new ArgumentException("Thông tin tìm vị trí xếp hàng không hợp lệ.");

        var storageRules = await _context.ProductFixedLocations
            .AsNoTracking()
            .Include(rule => rule.StorageLocation)
                .ThenInclude(location => location.StorageRack)
            .Where(rule => rule.ProductId == productId && rule.IsActive &&
                           rule.StorageLocation.WarehouseId == warehouseId)
            .ToListAsync();

        var locations = await _context.StorageLocations
            .AsNoTracking()
            .Include(location => location.StorageRack)
                .ThenInclude(rack => rack!.WarehouseZone)
            .Include(location => location.Inventories)
            .Where(location => location.WarehouseId == warehouseId &&
                               location.LocationType == "BIN" && location.IsPutawayAllowed &&
                               (location.Status == "ACTIVE" || location.Status == "AVAILABLE" || location.Status == "OCCUPIED"))
            .ToListAsync();

        IReadOnlyDictionary<long, LocationCapacityEvaluationDto> capacityEvaluations =
            new Dictionary<long, LocationCapacityEvaluationDto>();
        if (_capacityOptions.Enabled && locations.Count > 0)
        {
            capacityEvaluations = await _capacityEvaluationService.EvaluateAsync(
                locations.Select(location => new CapacityAllocationDto
                {
                    StorageLocationId = location.StorageLocationId,
                    ProductId = productId,
                    Quantity = putawayQuantity
                }).ToList());
        }

        return locations
            .Select(location =>
            {
                var matchingRules = storageRules
                    .Where(rule => IsRecommendedLocation(location, new[] { rule }))
                    .OrderByDescending(rule => rule.IsDefault)
                    .ThenBy(rule => rule.Priority)
                    .ToList();
                var preferredRule = matchingRules
                    .FirstOrDefault();
                capacityEvaluations.TryGetValue(location.StorageLocationId, out var capacity);
                var capacityStatus = capacity?.OverallStatus ?? "DISABLED";
                var isRecommended = matchingRules.Count > 0;
                var requiresAcknowledgement = _capacityOptions.Enabled &&
                    (capacityStatus is CapacityEvaluationStatuses.Unknown or CapacityEvaluationStatuses.NotConfigured ||
                     (storageRules.Count > 0 && !isRecommended));
                return new PutawayLocationDto
                {
                    StorageLocationId = location.StorageLocationId,
                    LocationCode = location.LocationCode,
                    LocationName = location.LocationName ?? location.LocationCode,
                    ZoneId = location.StorageRack?.ZoneId,
                    ZoneCode = location.StorageRack?.WarehouseZone?.ZoneCode ?? string.Empty,
                    ZoneName = location.StorageRack?.WarehouseZone?.ZoneName ?? string.Empty,
                    RackId = location.RackId,
                    RackCode = location.StorageRack?.RackCode ?? string.Empty,
                    RackName = location.StorageRack?.RackName ?? string.Empty,
                    Status = location.Status,
                    CurrentProductQuantity = location.Inventories
                        .Where(inventory => inventory.ProductId == productId)
                        .Sum(inventory => inventory.OnHandQuantity),
                    StoredProductCount = location.Inventories
                        .Where(inventory => inventory.OnHandQuantity > 0)
                        .Select(inventory => inventory.ProductId)
                        .Distinct()
                        .Count(),
                    IsRecommended = isRecommended,
                    HasRecommendationConfiguration = storageRules.Count > 0,
                    Priority = preferredRule?.Priority ?? int.MaxValue,
                    IsDefault = preferredRule?.IsDefault ?? false,
                    CapacityEvaluationEnabled = _capacityOptions.Enabled,
                    CapacityStatus = capacityStatus,
                    MaxWeightKg = capacity?.MaxWeightKg,
                    CurrentWeightKg = capacity?.CurrentWeightKg,
                    ProjectedWeightKg = capacity?.ProjectedWeightKg,
                    MaxVolumeM3 = capacity?.MaxVolumeM3,
                    CurrentVolumeM3 = capacity?.CurrentVolumeM3,
                    ProjectedVolumeM3 = capacity?.ProjectedVolumeM3,
                    CapacityMessage = BuildCapacityMessage(capacity),
                    RequiresAcknowledgement = requiresAcknowledgement
                };
            })
            .OrderBy(location => location.CapacityStatus == CapacityEvaluationStatuses.Exceeded)
            .ThenByDescending(location => location.IsDefault)
            .ThenByDescending(location => location.IsRecommended)
            .ThenBy(location => location.Priority)
            .ThenBy(location => location.ZoneCode)
            .ThenBy(location => location.RackCode)
            .ThenBy(location => location.LocationCode)
            .ToList();
    }

    private static bool IsRecommendedLocation(
        BMWMS.Repository.Models.StorageLocation destination,
        IReadOnlyCollection<BMWMS.Repository.Models.ProductFixedLocation> rules)
    {
        return rules.Any(rule => rule.StorageLocation.StorageRack != null && destination.StorageRack != null
            ? rule.StorageLocation.StorageRack.ZoneId == destination.StorageRack.ZoneId
            : rule.StorageLocationId == destination.StorageLocationId);
    }

    private static string BuildCapacityMessage(LocationCapacityEvaluationDto? capacity)
    {
        if (capacity == null)
            return "Kiểm tra sức chứa đang tắt.";
        if (capacity.OverallStatus == CapacityEvaluationStatuses.Exceeded)
        {
            var exceededScopes = capacity.Scopes
                .Where(scope => scope.OverallStatus == CapacityEvaluationStatuses.Exceeded)
                .Select(scope => $"{scope.ScopeType} {scope.ScopeCode}");
            return $"Số lượng dự kiến vượt sức chứa tại: {string.Join(", ", exceededScopes)}.";
        }
        if (capacity.OverallStatus == CapacityEvaluationStatuses.Unknown)
        {
            var missing = capacity.MissingWeightProductCodes
                .Concat(capacity.MissingVolumeProductCodes)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var unconfiguredScopes = capacity.Scopes
                .Where(scope => scope.OverallStatus == CapacityEvaluationStatuses.NotConfigured)
                .Select(scope => $"{scope.ScopeType} {scope.ScopeCode}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var unconfiguredMessage = unconfiguredScopes.Count == 0
                ? string.Empty
                : $" Chưa cấu hình giới hạn cho: {string.Join(", ", unconfiguredScopes)}.";
            return $"Chưa đủ hệ số lưu kho để tính sức chứa cho: {string.Join(", ", missing)}.{unconfiguredMessage}";
        }
        if (capacity.OverallStatus == CapacityEvaluationStatuses.NotConfigured)
            return "Bin, Rack và Zone chưa cấu hình giới hạn tải trọng hoặc thể tích.";
        return "Bin, Rack và Zone còn đủ sức chứa theo dữ liệu đã cấu hình.";
    }

    private static bool IsActive(string? status)
    {
        return status is "ACTIVE" or "AVAILABLE" or "OCCUPIED";
    }

    private static string AppendReceiptNote(string? existingNote, string entry)
    {
        const int maximumLength = 1000;
        var normalizedEntry = entry.Trim();
        if (normalizedEntry.Length > maximumLength)
            normalizedEntry = normalizedEntry[..maximumLength];
        if (string.IsNullOrWhiteSpace(existingNote))
            return normalizedEntry;

        var prefix = existingNote.Trim();
        var prefixLimit = Math.Max(0, maximumLength - normalizedEntry.Length - Environment.NewLine.Length);
        if (prefix.Length > prefixLimit)
            prefix = prefix[..prefixLimit];
        return $"{prefix}{Environment.NewLine}{normalizedEntry}";
    }

    private static string GetInboundDisplayStatus(BMWMS.Repository.Models.InboundOrder order)
    {
        if (order.Status == "DRAFT") return "DRAFT";
        if (order.Status == "ASSIGNED") return "READY";
        if (order.Status == "IN_PROGRESS") return "RECEIVING";
        if (order.Status == "CANCELLED") return "CANCELLED";

        var goodDetails = order.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
            .Where(d => d.ConditionStatus == "GOOD").ToList();
        return (goodDetails.Count == 0 || goodDetails.All(d => d.InventoryTransaction?.TransactionType == "INBOUND"))
            ? "PUTAWAY_COMPLETED"
            : "RECEIVED";
    }

}
