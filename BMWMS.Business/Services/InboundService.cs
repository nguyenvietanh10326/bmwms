using System;
using System.Linq;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.Inbound;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Business.Services;

public class InboundService : IInboundService
{
    private readonly IInboundRepository _inboundRepository;
    private readonly BMWMS.Repository.Models.BmwmsContext _context;
    private readonly INotificationService _notificationService;

    public InboundService(IInboundRepository inboundRepository, BMWMS.Repository.Models.BmwmsContext context, INotificationService notificationService)
    {
        _inboundRepository = inboundRepository;
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<InboundOrderPageDto> GetInboundOrdersPageAsync(InboundOrderFilterDto filter)
    {
        var result = await _inboundRepository.GetInboundOrdersPageAsync(
            filter.Keyword,
            filter.Status,
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
                SupplierName = x.PurchaseOrder?.Supplier?.SupplierName ?? "N/A",
                ExpectedReceiptDate = x.ExpectedReceiptDate,
                Status = x.Status,
                TotalExpectedQuantity = x.InboundOrderItems.Sum(i => i.ExpectedQuantity),
                TotalReceivedQuantity = x.InboundOrderItems.Sum(i => i.ReceivedQuantity),
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
            SupplierName = order.PurchaseOrder?.Supplier?.SupplierName ?? "",
            WarehouseName = order.Warehouse?.WarehouseName ?? "",
            ExpectedReceiptDate = order.ExpectedReceiptDate,
            Status = order.Status,
            AssignedToUserName = order.AssignedToUser?.FullName ?? "",
            CreatedByUserName = order.CreatedByUser?.FullName ?? "",
            ParentInboundOrderNumber = order.ParentInboundOrder?.InboundOrderNumber,
            Items = order.InboundOrderItems.Select(i => new InboundOrderItemDto
            {
                InboundOrderItemId = i.InboundOrderItemId,
                ProductId = i.ProductId,
                ProductCode = i.Product.ProductCode,
                ProductName = i.Product.ProductName,
                ExpectedQuantity = i.ExpectedQuantity,
                ReceivedQuantity = i.ReceivedQuantity,
                PutawayQuantity = i.InboundOrderDetails.Sum(d => d.ReceivedQuantity),
                LotNumber = i.InboundOrderDetails.FirstOrDefault()?.ProductLot?.LotNumber,
                ExpiryDate = i.InboundOrderDetails.FirstOrDefault()?.ProductLot?.ExpiryDate,
                Receipts = i.InboundOrderDetails.Select(d => new BMWMS.Business.DTOs.Inbound.InboundReceiptDto
                {
                    ProductLotId = d.ProductLotId,
                    LotNumber = d.ProductLot?.LotNumber,
                    ExpiryDate = d.ProductLot?.ExpiryDate,
                    ReceivedQuantity = d.ReceivedQuantity,
                    PutawayQuantity = 0
                }).ToList()
            }).ToList()
        };

        var timeline = new List<InboundOrderTimelineDto>();
        
        timeline.Add(new InboundOrderTimelineDto
        {
            EventTime = order.CreatedAt,
            EventName = "Tạo lệnh nhập",
            StatusBadge = "READY",
            StatusBadgeColor = "gy",
            PerformedBy = order.CreatedByUser?.FullName ?? "Hệ thống"
        });

        if (order.ConfirmedAt.HasValue)
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = order.ConfirmedAt.Value,
                EventName = "Xác nhận lệnh nhập",
                StatusBadge = "ASSIGNED",
                StatusBadgeColor = "b",
                PerformedBy = order.ConfirmedByUser?.FullName ?? "Quản lý"
            });
        }
        
        if (order.Status == "RECEIVED" || order.Status == "PUTAWAY_COMPLETED")
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = order.CreatedAt.AddHours(2), // Giả lập thời gian
                EventName = "Nhận hàng hoàn tất",
                StatusBadge = "RECEIVED",
                StatusBadgeColor = "g",
                PerformedBy = order.AssignedToUser?.FullName ?? ""
            });
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
        if (dto.Items == null || !dto.Items.Any())
            throw new ArgumentException("Lệnh nhập kho phải có ít nhất 1 dòng hàng.");

        if (dto.Items.Any(i => i.ExpectedQuantity <= 0))
            throw new ArgumentException("Số lượng dự kiến phải lớn hơn 0.");

        if (dto.SourceType == "PURCHASE_ORDER" && dto.PurchaseOrderId.HasValue)
        {
            var po = await GetPurchaseOrderForInboundAsync(dto.PurchaseOrderId.Value);
            foreach (var item in dto.Items)
            {
                var poItem = po?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (poItem == null || item.ExpectedQuantity > poItem.RemainingQuantity)
                    throw new ArgumentException($"Số lượng dự kiến vượt quá số lượng còn lại.");
            }
        }
        else if (dto.SourceType == "SUPPLEMENT" && dto.ParentInboundOrderId.HasValue)
        {
            var parent = await GetInboundOrderForSupplementAsync(dto.ParentInboundOrderId.Value);
            foreach (var item in dto.Items)
            {
                var parentItem = parent?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (parentItem == null || item.ExpectedQuantity > parentItem.RemainingQuantity)
                    throw new ArgumentException($"Số lượng dự kiến vượt quá số lượng thiếu.");
            }
        }

        var inboundOrder = new BMWMS.Repository.Models.InboundOrder
        {
            InboundOrderNumber = "IN-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
            SourceType = dto.SourceType,
            PurchaseOrderId = dto.PurchaseOrderId,
            SalesOrderId = dto.SalesOrderId,
            WarehouseId = dto.WarehouseId,
            ExpectedReceiptDate = dto.ExpectedReceiptDate,
            Notes = dto.Notes,
            Status = dto.AssignedToUserId.HasValue ? "ASSIGNED" : "DRAFT",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUserId,
            AssignedToUserId = dto.AssignedToUserId,
            ParentInboundOrderId = dto.ParentInboundOrderId
        };

        foreach (var itemDto in dto.Items)
        {
            inboundOrder.InboundOrderItems.Add(new BMWMS.Repository.Models.InboundOrderItem
            {
                ProductId = itemDto.ProductId,
                ExpectedQuantity = itemDto.ExpectedQuantity,
                ReceivedQuantity = 0,
                DamagedQuantity = 0,
                ShortageQuantity = 0,
                Notes = itemDto.Notes
            });
        }

        await _inboundRepository.AddAsync(inboundOrder);
        await _inboundRepository.SaveChangesAsync();

        if (inboundOrder.Status == "ASSIGNED" && inboundOrder.AssignedToUserId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(new BMWMS.Business.DTOs.Notification.CreateNotificationDto
            {
                Title = "Bạn được giao một lệnh nhập kho mới",
                Message = $"Lệnh nhập kho {inboundOrder.InboundOrderNumber} đã được giao cho bạn. Vui lòng kiểm tra.",
                NotificationType = "WORK_ASSIGNMENT",
                TargetUserId = inboundOrder.AssignedToUserId.Value,
                ReferenceType = "INBOUND",
                ReferenceId = inboundOrder.InboundOrderId.ToString()
            }, currentUserId);
        }

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

        var dto = new PurchaseOrderForInboundDto
        {
            PurchaseOrderId = po.PurchaseOrderId,
            PurchaseOrderNumber = po.PurchaseOrderNumber,
            SupplierName = po.Supplier?.SupplierName ?? string.Empty
        };

        // Find existing inbound quantities for this PO
        var inboundItems = await _context.InboundOrderItems
            .Include(i => i.InboundOrder)
            .Where(i => i.InboundOrder.PurchaseOrderId == purchaseOrderId && i.InboundOrder.Status != "CANCELLED")
            .ToListAsync();

        foreach (var detail in po.PurchaseOrderDetails)
        {
            var totalInbound = inboundItems
                .Where(i => i.ProductId == detail.ProductId)
                .Sum(i => i.ExpectedQuantity);

            var remaining = detail.OrderedQuantity - totalInbound;

            dto.Items.Add(new PurchaseOrderItemForInboundDto
            {
                ProductId = detail.ProductId,
                ProductCode = detail.Product.ProductCode,
                ProductName = detail.Product.ProductName,
                UnitName = detail.Product.UnitOfMeasure?.UnitName ?? string.Empty,
                OrderedQuantity = detail.OrderedQuantity,
                InboundQuantity = totalInbound,
                RemainingQuantity = remaining > 0 ? remaining : 0
            });
        }

        return dto;
    }

    public async Task<List<ShortageInboundOrderDto>> GetShortageInboundOrdersAsync()
    {
        var shortageOrders = await _context.InboundOrders
            .Include(o => o.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Include(o => o.InboundOrderItems)
            .Where(o => o.Status == "COMPLETED" 
                     && o.InboundOrderItems.Any(i => i.ReceivedQuantity < i.ExpectedQuantity))
            .ToListAsync();

        return shortageOrders.Select(o => new ShortageInboundOrderDto
        {
            InboundOrderId = o.InboundOrderId,
            InboundOrderNumber = o.InboundOrderNumber,
            PurchaseOrderNumber = o.PurchaseOrder?.PurchaseOrderNumber ?? string.Empty,
            SupplierName = o.PurchaseOrder?.Supplier?.SupplierName ?? string.Empty,
            OriginalExpectedDate = o.ExpectedReceiptDate
        }).ToList();
    }

    public async Task<PurchaseOrderForInboundDto?> GetInboundOrderForSupplementAsync(long parentId)
    {
        var parentOrder = await _context.InboundOrders
            .Include(o => o.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Include(o => o.InboundOrderItems)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(o => o.InboundOrderId == parentId);

        if (parentOrder == null) return null;

        var dto = new PurchaseOrderForInboundDto
        {
            PurchaseOrderId = parentOrder.PurchaseOrderId ?? 0,
            PurchaseOrderNumber = parentOrder.InboundOrderNumber, // Use IO number for reference
            SupplierName = parentOrder.PurchaseOrder?.Supplier?.SupplierName ?? string.Empty
        };

        foreach (var item in parentOrder.InboundOrderItems)
        {
            var shortage = item.ExpectedQuantity - item.ReceivedQuantity;
            if (shortage > 0)
            {
                // Find existing supplements for this parent
                var existingSupplements = await _context.InboundOrderItems
                    .Include(i => i.InboundOrder)
                    .Where(i => i.InboundOrder.ParentInboundOrderId == parentId 
                             && i.InboundOrder.Status != "CANCELLED"
                             && i.ProductId == item.ProductId)
                    .SumAsync(i => i.ExpectedQuantity);

                var remaining = shortage - existingSupplements;

                dto.Items.Add(new PurchaseOrderItemForInboundDto
                {
                    ProductId = item.ProductId,
                    ProductCode = item.Product.ProductCode,
                    ProductName = item.Product.ProductName,
                    UnitName = item.Product.UnitOfMeasure?.UnitName ?? string.Empty,
                    OrderedQuantity = item.ExpectedQuantity, // Treat the initial shortage as ordered
                    InboundQuantity = existingSupplements,
                    RemainingQuantity = remaining > 0 ? remaining : 0
                });
            }
        }

        return dto;
    }

    public async Task<List<SourceOrderDropdownDto>> GetPendingPurchaseOrdersAsync()
    {
        var pos = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderDetails)
            .Include(po => po.InboundOrders)
                .ThenInclude(io => io.InboundOrderItems)
            .Where(po => po.Status == "CONFIRMED" || po.Status == "PARTIALLY_RECEIVED")
            .ToListAsync();

        return pos
            .Where(po => 
            {
                var totalOrdered = po.PurchaseOrderDetails.Sum(d => d.OrderedQuantity);
                var totalExpected = po.InboundOrders
                    .Where(io => io.Status != "CANCELLED")
                    .SelectMany(io => io.InboundOrderItems)
                    .Sum(i => i.ExpectedQuantity);
                return totalOrdered > totalExpected;
            })
            .Select(po => new SourceOrderDropdownDto
            {
                Id = po.PurchaseOrderId,
                Name = $"{po.PurchaseOrderNumber} — {po.Supplier?.SupplierName ?? "N/A"}"
            })
            .ToList();
    }

    public async Task<List<SourceOrderDropdownDto>> GetReturnableSalesOrdersAsync()
    {
        return await _context.SalesOrders
            .Include(so => so.Customer)
            .Where(so => so.Status == "FULFILLED" || so.Status == "PARTIALLY_FULFILLED")
            .Select(so => new SourceOrderDropdownDto
            {
                Id = so.SalesOrderId,
                Name = $"{so.SalesOrderNumber} — {so.Customer.CustomerName}"
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
            .FirstOrDefaultAsync(s => s.SalesOrderId == salesOrderId);

        if (so == null) return null;

        var dto = new PurchaseOrderForInboundDto
        {
            PurchaseOrderId = so.SalesOrderId,
            PurchaseOrderNumber = so.SalesOrderNumber,
            SupplierName = so.Customer.CustomerName,
            Items = so.SalesOrderDetails.Select(d => 
            {
                // Tương tự PO, tính số lượng đã tạo lệnh nhập kho trước đó cho SO này
                var totalExpectedBefore = so.InboundOrders
                    .Where(io => io.Status != "CANCELLED")
                    .SelectMany(io => io.InboundOrderItems)
                    .Where(ioItem => ioItem.ProductId == d.ProductId)
                    .Sum(ioItem => ioItem.ExpectedQuantity);

                // Sales Order có quantity trả lại tối đa bằng số lượng đã xuất kho (FulfilledQuantity)
                var remaining = d.FulfilledQuantity - totalExpectedBefore;
                
                return new PurchaseOrderItemForInboundDto
                {
                    ProductId = d.ProductId,
                    ProductCode = d.Product.ProductCode,
                    ProductName = d.Product.ProductName,
                    UnitName = d.Product.UnitOfMeasure?.UnitName ?? "",
                    OrderedQuantity = d.FulfilledQuantity, // Gán tạm OrderedQuantity = FulfilledQuantity để frontend hiển thị đúng
                    RemainingQuantity = remaining > 0 ? remaining : 0
                };
            }).Where(i => i.RemainingQuantity > 0).ToList() // Chỉ lấy món còn hàng để nhập kho
        };

        return dto;
    }

    public async Task UpdateInboundOrderAsync(long id, UpdateInboundOrderDto dto, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == id);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        if (order.Status != "DRAFT") throw new Exception("Chỉ có thể sửa lệnh nhập kho ở trạng thái Chờ xử lý");

        if (dto.Items == null || !dto.Items.Any())
            throw new Exception("Lệnh nhập kho phải có ít nhất 1 dòng hàng.");

        if (dto.Items.Any(i => i.ExpectedQuantity <= 0))
            throw new Exception("Số lượng dự kiến phải lớn hơn 0.");

        if (order.SourceType == "PURCHASE_ORDER" && order.PurchaseOrderId.HasValue)
        {
            var po = await GetPurchaseOrderForInboundAsync(order.PurchaseOrderId.Value);
            foreach (var item in dto.Items)
            {
                var poItem = po?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (poItem == null || item.ExpectedQuantity > poItem.RemainingQuantity)
                    throw new Exception($"Số lượng dự kiến của sản phẩm {poItem?.ProductName ?? item.ProductId.ToString()} vượt quá số lượng còn lại ({poItem?.RemainingQuantity ?? 0}).");
            }
        }
        else if (order.SourceType == "SUPPLEMENT" && order.ParentInboundOrderId.HasValue)
        {
            var parent = await GetInboundOrderForSupplementAsync(order.ParentInboundOrderId.Value);
            foreach (var item in dto.Items)
            {
                var parentItem = parent?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (parentItem == null || item.ExpectedQuantity > parentItem.RemainingQuantity)
                    throw new Exception($"Số lượng dự kiến của sản phẩm {parentItem?.ProductName ?? item.ProductId.ToString()} vượt quá số lượng thiếu ({parentItem?.RemainingQuantity ?? 0}).");
            }
        }

        order.ExpectedReceiptDate = dto.ExpectedReceiptDate;
        order.Notes = dto.Notes;
        
        bool newlyAssigned = false;
        if (order.Status == "DRAFT" && dto.AssignedToUserId.HasValue)
        {
            order.Status = "ASSIGNED";
            newlyAssigned = true;
        }
        
        order.AssignedToUserId = dto.AssignedToUserId;

        foreach (var itemDto in dto.Items)
        {
            var existingItem = order.InboundOrderItems.FirstOrDefault(i => i.ProductId == itemDto.ProductId);
            if (existingItem != null)
            {
                existingItem.ExpectedQuantity = itemDto.ExpectedQuantity;
                existingItem.Notes = itemDto.Notes;
            }
        }

        await _context.SaveChangesAsync();

        if (newlyAssigned && order.AssignedToUserId.HasValue)
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
        var order = await _inboundRepository.GetByIdAsync(id);
        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        if (order.Status != "DRAFT") throw new Exception("Chỉ có thể hủy lệnh nhập kho ở trạng thái Chờ xử lý");

        order.Status = "CANCELLED";
        order.CancellationReason = dto.CancellationReason;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledByUserId = currentUserId;

        await _inboundRepository.UpdateAsync(order);
        await _inboundRepository.SaveChangesAsync();
    }

    public async Task ConfirmInboundOrderAsync(long id, long currentUserId)
    {
        var order = await _inboundRepository.GetByIdAsync(id);
        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        if (order.Status != "DRAFT") throw new Exception("Chỉ có thể xác nhận lệnh nhập kho ở trạng thái Chờ xử lý (DRAFT)");

        order.Status = "ASSIGNED";
        order.ConfirmedAt = DateTime.UtcNow;
        order.ConfirmedByUserId = currentUserId;

        await _inboundRepository.UpdateAsync(order);
        await _inboundRepository.SaveChangesAsync();
    }

    public async Task<long> ReceiveItemAsync(long inboundOrderId, ReceiveInboundItemDto dto, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho.");
        if (order.Status != "ASSIGNED" && order.Status != "IN_PROGRESS") 
            throw new Exception("Chỉ có thể nhận hàng khi lệnh ở trạng thái ASSIGNED hoặc IN_PROGRESS.");

        var item = order.InboundOrderItems.FirstOrDefault(i => i.InboundOrderItemId == dto.InboundOrderItemId);
        if (item == null) throw new Exception("Không tìm thấy sản phẩm trong lệnh nhập kho.");

        // Calculate Accepted Quantity
        decimal acceptedQuantity = dto.DeliveredQuantity - dto.DamagedQuantity;
        if (acceptedQuantity <= 0) throw new Exception("Số lượng chấp nhận phải lớn hơn 0.");

        // Update item quantities
        item.ReceivedQuantity += acceptedQuantity;
        item.DamagedQuantity += dto.DamagedQuantity;

        // Create or find ProductLot
        var lot = await _context.ProductLots
            .FirstOrDefaultAsync(l => l.ProductId == item.ProductId && l.LotNumber == dto.LotNumber);

        if (lot == null)
        {
            lot = new BMWMS.Repository.Models.ProductLot
            {
                ProductId = item.ProductId,
                LotNumber = dto.LotNumber,
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

        await _context.SaveChangesAsync();
        return lot.ProductLotId;
    }

    public async Task ReceiveBatchAsync(long inboundOrderId, ReceiveBatchInboundDto dto, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho.");
        if (order.Status != "ASSIGNED" && order.Status != "IN_PROGRESS") 
            throw new Exception("Chỉ có thể nhận hàng khi lệnh ở trạng thái ASSIGNED hoặc IN_PROGRESS.");

        var stagingLocation = await _context.StorageLocations
            .FirstOrDefaultAsync(l => l.WarehouseId == order.WarehouseId);

        if (stagingLocation == null) throw new Exception("Không tìm thấy vị trí lưu trữ nào trong kho này để nhận tạm.");

        foreach (var reqItem in dto.Items)
        {
            var item = order.InboundOrderItems.FirstOrDefault(i => i.InboundOrderItemId == reqItem.InboundOrderItemId);
            if (item == null) continue;

            decimal acceptedQuantity = reqItem.DeliveredQuantity - reqItem.DamagedQuantity;
            if (acceptedQuantity <= 0) continue;

            item.ReceivedQuantity += acceptedQuantity;
            item.DamagedQuantity += reqItem.DamagedQuantity;

            var lot = await _context.ProductLots
                .FirstOrDefaultAsync(l => l.ProductId == item.ProductId && l.LotNumber == reqItem.LotNumber);

            if (lot == null)
            {
                lot = new BMWMS.Repository.Models.ProductLot
                {
                    ProductId = item.ProductId,
                    LotNumber = reqItem.LotNumber,
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

        await _context.SaveChangesAsync();
    }

    public async Task PutawayBatchAsync(long inboundOrderId, List<PutawayInboundItemDto> dtos, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho.");

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
                // Assuming InboundOrderItemId maps to detail, but correctly it should be null or map to the specific receipt. 
                // We leave it null for PUTAWAY since PUTAWAY doesn't strictly need detail link.
                InboundOrderDetailId = null,
                TransactionAt = DateTime.UtcNow,
                PerformedByUserId = currentUserId
            };
            _context.InventoryTransactions.Add(transaction);
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
}
