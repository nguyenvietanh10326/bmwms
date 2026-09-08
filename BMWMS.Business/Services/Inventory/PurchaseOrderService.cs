using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.Interfaces;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using QuantityRules = BMWMS.Business.Common.QuantityRules;
using PurchaseOrderReceiptRules = BMWMS.Business.Common.PurchaseOrderReceiptRules;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Services.Inventory
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _poRepository;
        private readonly IEmailService _emailService;
        private readonly IPurchaseOrderEmailComposer _emailComposer;
        private readonly BmwmsContext _context;
        private readonly IAuditLogService _auditLogService;

        public PurchaseOrderService(
            IPurchaseOrderRepository poRepository,
            IEmailService emailService,
            IPurchaseOrderEmailComposer emailComposer,
            BmwmsContext context,
            IAuditLogService auditLogService)
        {
            _poRepository = poRepository;
            _emailService = emailService;
            _emailComposer = emailComposer;
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResultDto<PurchaseOrderListDto>> GetPagedOrdersAsync(PurchaseOrderFilterDto filter)
        {
            var (items, totalCount) = await _poRepository.GetPagedListAsync(
                filter.SearchTerm,
                filter.Status,
                filter.WarehouseId,
                filter.PageIndex,
                filter.PageSize
            );

            var listDtos = items.Select(po =>
            {
                // Lấy đơn vị tính đại diện từ sản phẩm đầu tiên
                var firstDetail = po.PurchaseOrderDetails.FirstOrDefault();
                string unitName = firstDetail?.Product?.UnitOfMeasure?.UnitName ?? string.Empty;

                return new PurchaseOrderListDto
                {
                    PurchaseOrderId = po.PurchaseOrderId,
                    PurchaseOrderNumber = po.PurchaseOrderNumber,
                    SupplierId = po.SupplierId,
                    SupplierCode = po.Supplier?.SupplierCode ?? string.Empty,
                    SupplierName = po.Supplier?.SupplierName ?? string.Empty,
                    OrderDate = po.OrderDate,
                    ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                    Status = po.Status,
                    ItemCount = po.PurchaseOrderDetails.Count
                };
            });

            return new PagedResultDto<PurchaseOrderListDto>
            {
                Items = listDtos,
                TotalCount = totalCount,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize
            };
        }

        public async Task<PurchaseOrderDetailDto?> GetOrderDetailAsync(long purchaseOrderId)
        {
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return null;

            var status = NormalizePurchaseOrderStatus(po.Status);
            var activeInbounds = po.InboundOrders
                .Where(io => NormalizeInboundStatus(io.Status) != "CANCELLED")
                .ToList();

            var items = po.PurchaseOrderDetails.Select(d =>
            {
                var inboundItems = activeInbounds
                    .SelectMany(io => io.InboundOrderItems)
                    .Where(item => item.ProductId == d.ProductId)
                    .ToList();
                var snapshot = PurchaseOrderReceiptRules.CalculateLine(d.OrderedQuantity, inboundItems);

                return new PurchaseOrderItemDto
                {
                    PurchaseOrderDetailId = d.PurchaseOrderDetailId,
                    ProductId = d.ProductId,
                    ProductCode = d.Product?.ProductCode ?? string.Empty,
                    ProductName = d.Product?.ProductName ?? string.Empty,
                    Unit = d.Product?.UnitOfMeasure?.UnitName ?? string.Empty,
                    QuantityScale = d.Product?.UnitOfMeasure?.QuantityScale ?? 0,
                    OrderedQuantity = d.OrderedQuantity,
                    PlannedInboundQuantity = snapshot.ActivePlannedQuantity,
                    ReceivedQuantity = snapshot.AcceptedQuantity,
                    RejectedQuantity = snapshot.RejectedQuantity,
                    RemainingQuantity = snapshot.AvailableToPlanQuantity,
                    Notes = d.Notes
                };
            }).ToList();

            var hasActiveInbound = activeInbounds.Count > 0;
            var hasQuantityToPlan = po.PurchaseOrderDetails.Any(d =>
            {
                var inboundItems = activeInbounds
                    .SelectMany(io => io.InboundOrderItems)
                    .Where(item => item.ProductId == d.ProductId)
                    .ToList();
                return PurchaseOrderReceiptRules.CalculateLine(d.OrderedQuantity, inboundItems)
                    .AvailableToPlanQuantity > 0;
            });

            return new PurchaseOrderDetailDto
            {
                PurchaseOrderId = po.PurchaseOrderId,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                SupplierId = po.SupplierId,
                SupplierCode = po.Supplier?.SupplierCode ?? string.Empty,
                SupplierName = po.Supplier?.SupplierName ?? string.Empty,
                SupplierEmail = po.Supplier?.Email,
                OrderDate = po.OrderDate,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Status = status,
                Notes = po.Notes,
                CreatedByUserId = po.CreatedByUserId,
                CreatedByUserName = po.CreatedByUser?.FullName ?? po.CreatedByUser?.Username ?? string.Empty,
                CreatedAt = po.CreatedAt,
                ConfirmedByUserId = po.ConfirmedByUserId,
                ConfirmedByUserName = po.ConfirmedByUser?.FullName,
                ConfirmedAt = po.ConfirmedAt,
                CanSendToSupplier = status == "DRAFT",
                CanCancel = status == "DRAFT" && !hasActiveInbound,
                CanCreateInbound = (status == "CONFIRMED" || status == "PARTIALLY_RECEIVED") && hasQuantityToPlan,
                Items = items,
                Inbounds = po.InboundOrders.Select(io =>
                {
                    var receiptDetails = io.InboundOrderItems.SelectMany(item => item.InboundOrderDetails).ToList();
                    return new RelatedInboundDto
                    {
                        InboundOrderId = io.InboundOrderId,
                        InboundOrderNumber = io.InboundOrderNumber,
                        ExpectedReceiptDate = io.ExpectedReceiptDate,
                        Status = GetInboundDisplayStatus(io),
                        Notes = io.Notes,
                        LineCount = io.InboundOrderItems.Count,
                        ExpectedQuantity = io.InboundOrderItems.Sum(item => item.ExpectedQuantity),
                        ReceivedQuantity = receiptDetails.Where(detail => detail.ConditionStatus == "GOOD")
                            .Sum(detail => detail.ReceivedQuantity),
                        RejectedQuantity = Math.Max(
                            io.InboundOrderItems.Sum(item => item.DamagedQuantity),
                            receiptDetails.Where(detail => detail.ConditionStatus is "DAMAGED" or "REJECTED" or "QUARANTINED")
                                .Sum(detail => detail.ReceivedQuantity))
                    };
                }).OrderByDescending(io => io.ExpectedReceiptDate).ToList()
            };
        }

        private static string NormalizePurchaseOrderStatus(string? status)
        {
            return (status ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "PARTIALLYRECEIVED" => "PARTIALLY_RECEIVED",
                "COMPLETED" => "RECEIVED",
                var value => value
            };
        }

        private static string NormalizeInboundStatus(string? status)
        {
            return (status ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "ASSIGNED" or "PENDING" => "READY",
                "IN_PROGRESS" => "RECEIVING",
                "COMPLETED" => "PUTAWAY_COMPLETED",
                var value => value
            };
        }

        private static string GetInboundDisplayStatus(InboundOrder order)
        {
            var normalized = NormalizeInboundStatus(order.Status);
            if (normalized != "PUTAWAY_COMPLETED") return normalized;

            var goodReceipts = order.InboundOrderItems
                .SelectMany(item => item.InboundOrderDetails)
                .Where(detail => detail.ConditionStatus == "GOOD")
                .ToList();
            return goodReceipts.Count == 0 ||
                   goodReceipts.All(detail => detail.InventoryTransaction?.TransactionType == "INBOUND")
                ? "PUTAWAY_COMPLETED"
                : "RECEIVED";
        }

        public async Task<(bool Success, string Message)> ConfirmOrderAsync(long purchaseOrderId, long currentUserId)
        {
            await Task.CompletedTask;
            return (false, "PO được hệ thống tự động xác nhận sau khi gửi email thành công; không có bước xác nhận thủ công.");
        }

        public async Task<(bool Success, string Message)> SendOrderToSupplierAsync(long purchaseOrderId, long currentUserId)
        {
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");

            var status = NormalizePurchaseOrderStatus(po.Status);
            if (status == "CONFIRMED")
                return (true, "PO đã được xác nhận trước đó; hệ thống không gửi lại email.");
            if (status != "DRAFT")
                return (false, "Chỉ được gửi email cho nhà cung cấp khi PO đang ở trạng thái Nháp.");

            var supplierEmail = po.Supplier?.Email?.Trim();
            if (string.IsNullOrWhiteSpace(supplierEmail))
                return (false, "Nhà cung cấp chưa có địa chỉ email. Vui lòng cập nhật thông tin NCC trước khi gửi PO.");

            try
            {
                var message = _emailComposer.Compose(po);
                await _emailService.SendEmailAsync(message);
            }
            catch (Exception exception)
            {
                return (false, $"Gửi email cho nhà cung cấp thất bại: {exception.Message}");
            }

            var sentAt = DateTime.UtcNow;
            po.Status = "CONFIRMED";
            po.ConfirmedByUserId = currentUserId;
            po.ConfirmedAt = sentAt;
            po.UpdatedAt = sentAt;

            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = currentUserId,
                ActionType = "SEND_PURCHASE_ORDER_EMAIL",
                EntityName = AuditEntities.PurchaseOrder,
                EntityId = po.PurchaseOrderId.ToString(),
                NewValues = new
                {
                    PreviousStatus = "DRAFT",
                    Status = "CONFIRMED",
                    RecipientEmail = supplierEmail,
                    EmailSentAt = sentAt,
                    po.ConfirmedAt
                }
            });

            await _poRepository.UpdateAsync(po);
            return (true, "Đã gửi email kèm file Excel cho nhà cung cấp và xác nhận PO thành công.");
        }

        public async Task<(bool Success, string Message)> CancelOrderAsync(long purchaseOrderId, long currentUserId, string? reason)
        {
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");

            var status = NormalizePurchaseOrderStatus(po.Status);
            if (status != "DRAFT")
            {
                return (false, "Chỉ có thể hủy đơn mua hàng khi còn ở trạng thái Nháp. PO đã xác nhận không được phép hủy.");
            }

            if (po.InboundOrders.Any(io => NormalizeInboundStatus(io.Status) != "CANCELLED"))
                return (false, "Không thể hủy PO vì đã có phiếu nhập kho đang hoạt động.");

            reason = reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5 || reason.Length > 500)
                return (false, "Lý do hủy phải có từ 5 đến 500 ký tự.");

            po.Status = "CANCELLED";
            po.Notes = string.IsNullOrWhiteSpace(po.Notes)
                ? $"Lý do hủy: {reason}"
                : $"{po.Notes}\nLý do hủy: {reason}";
            po.UpdatedAt = DateTime.UtcNow;

            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = currentUserId,
                ActionType = "CANCEL_PURCHASE_ORDER",
                EntityName = AuditEntities.PurchaseOrder,
                EntityId = po.PurchaseOrderId.ToString(),
                OldValues = new { Status = "DRAFT" },
                NewValues = new { Status = "CANCELLED", Reason = reason }
            });

            await _poRepository.UpdateAsync(po);

            return (true, "Đã hủy đơn mua hàng thành công.");
        }

        public async Task<(bool Success, string Message)> ClosePartiallyReceivedOrderAsync(long purchaseOrderId, long currentUserId, string reason)
        {
            reason = reason?.Trim() ?? string.Empty;
            if (reason.Length is < 10 or > 500)
                return (false, "Lý do kết thúc sớm phải có từ 10 đến 500 ký tự.");
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");
            if (NormalizePurchaseOrderStatus(po.Status) != "PARTIALLY_RECEIVED")
                return (false, "Chỉ được kết thúc sớm PO đã nhận một phần.");
            if (po.InboundOrders.Any(order => order.Status is "DRAFT" or "ASSIGNED" or "IN_PROGRESS"))
                return (false, "Còn phiếu nhập đang xử lý; phải hoàn tất hoặc hủy phiếu đó trước.");

            var activeInbounds = po.InboundOrders.Where(order => order.Status != "CANCELLED").ToList();
            var snapshots = po.PurchaseOrderDetails.Select(detail => PurchaseOrderReceiptRules.CalculateLine(
                detail.OrderedQuantity,
                activeInbounds.SelectMany(order => order.InboundOrderItems)
                    .Where(item => item.ProductId == detail.ProductId))).ToList();
            if (!snapshots.Any(snapshot => snapshot.AcceptedQuantity > 0) ||
                !snapshots.Any(snapshot => snapshot.AcceptedQuantity < snapshot.OrderedQuantity))
                return (false, "PO không ở tình huống đã nhận thiếu để kết thúc sớm.");

            po.Status = "CLOSED";
            po.UpdatedAt = DateTime.UtcNow;
            po.Notes = string.IsNullOrWhiteSpace(po.Notes)
                ? $"Kết thúc khi chưa nhận đủ: {reason}"
                : $"{po.Notes}\nKết thúc khi chưa nhận đủ: {reason}";
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = currentUserId,
                ActionType = "CLOSE_PARTIAL_PURCHASE_ORDER",
                EntityName = AuditEntities.PurchaseOrder,
                EntityId = po.PurchaseOrderId.ToString(),
                OldValues = new { Status = "PARTIALLY_RECEIVED" },
                NewValues = new { Status = "CLOSED", Reason = reason }
            });
            await _poRepository.UpdateAsync(po);
            return (true, "Đã kết thúc PO theo số lượng thực nhận; không thể tạo thêm phiếu nhập cho phần còn lại.");
        }

        public async Task<IEnumerable<ProductLookupDto>> GetUpListAsync()
        {
            var products = await _poRepository.GetAllProductAsync();
            return products.Select(p => new ProductLookupDto
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode ?? string.Empty,
                ProductName = p.ProductName ?? string.Empty,
                UnitOfMeasure = p.UnitOfMeasure?.UnitName ?? string.Empty,
                ProductGroupId = p.ProductGroupId,
                ProductGroupName = p.ProductGroup?.GroupName ?? string.Empty,
                Barcode = p.Barcode,
                RotationMethod = p.RotationMethod,
                OnHandQuantity = p.Inventories.Sum(x => x.OnHandQuantity),
                ReservedQuantity = p.Inventories.Sum(x => x.ReservedQuantity),
                AvailableQuantity = p.Inventories.Sum(x => x.OnHandQuantity - x.ReservedQuantity),
                PurchasePrice = p.SupplierProducts.FirstOrDefault()?.LastPurchasePrice ?? 0
            });
        }

        public async Task<IEnumerable<WarehouseLookupDto>> GetLookListAsync()
        {
            var warehouses = await _poRepository.GetAllWareAsync();
            return warehouses.Select(w => new WarehouseLookupDto
            {
                WarehouseId = w.WarehouseId,
                WarehouseCode = w.WarehouseCode ?? string.Empty,
                WarehouseName = w.WarehouseName ?? string.Empty
            });
        }

        public async Task<IEnumerable<SupplierLookupDto>> GetLookupListAsync()
        {
            var suppliers = await _poRepository.GetAllAsync();
            return suppliers.Select(s => new SupplierLookupDto
            {
                SupplierId = s.SupplierId,
                SupplierCode = s.SupplierCode ?? string.Empty,
                SupplierName = s.SupplierName ?? string.Empty
            });
        }

        public async Task<IEnumerable<CustomerLookupDto>> GetCustomersAsync()
        {
            var customers = await _poRepository.GetCustomersAsync();
            return customers.Select(s => new CustomerLookupDto
            {
                CustomerId = s.CustomerId,
                CustomerCode = s.CustomerCode ?? string.Empty,
                CustomerName = s.CustomerName ?? string.Empty,
                PhoneNumber = s.PhoneNumber ?? string.Empty,
                Address = s.Address ?? string.Empty
            });
        }
    
        public async Task<(bool Success, string Message)> CreatePurchaseOrderAsync(PurchaseOrderCreateDto request, long userId)
        {
            // 1. Validate
            if (request.ExpectedDeliveryDate.HasValue && request.ExpectedDeliveryDate.Value < request.OrderDate)
            {
                return (false, "Ngày giao dự kiến không được nhỏ hơn ngày đặt hàng.");
            }
            if (!request.OrderDetails.Any())
            {
                return (false, "Lệnh mua hàng phải có ít nhất một vật tư.");
            }

            var duplicateProducts = request.OrderDetails.GroupBy(x => x.ProductId).Where(g => g.Count() > 1).ToList();
            if (duplicateProducts.Any())
            {
                return (false, "Không được chọn trùng lặp vật tư trong cùng một lệnh mua hàng.");
            }

            var productIds = request.OrderDetails.Select(x => x.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Include(p => p.UnitOfMeasure)
                .Where(p => productIds.Contains(p.ProductId) && p.Status == "ACTIVE")
                .ToDictionaryAsync(p => p.ProductId);
            if (products.Count != productIds.Count)
                return (false, "Một hoặc nhiều vật tư không tồn tại hoặc đã ngừng hoạt động.");

            try
            {
                foreach (var item in request.OrderDetails)
                    QuantityRules.EnsureValid(products[item.ProductId], item.OrderedQuantity, "Số lượng đặt");
            }
            catch (ArgumentException ex)
            {
                return (false, ex.Message);
            }

            // 2. Generate PO Number
            string poNumber = await _poRepository.GeneratePurchaseOrderNumberAsync();

            // 3. Map to Entity
            var po = new BMWMS.Repository.Models.PurchaseOrder
            {
                PurchaseOrderNumber = poNumber,
                SupplierId = request.SupplierId,
                OrderDate = request.OrderDate,
                ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                Status = "DRAFT", // Hardcoded as per implementation plan
                Notes = request.Notes,
                CreatedByUserId = userId,
                CreatedAt = DateTime.Now,
                PurchaseOrderDetails = new List<BMWMS.Repository.Models.PurchaseOrderDetail>()
            };

            foreach (var item in request.OrderDetails)
            {
                if (item.OrderedQuantity <= 0)
                {
                    return (false, "Số lượng đặt phải lớn hơn 0.");
                }

                po.PurchaseOrderDetails.Add(new BMWMS.Repository.Models.PurchaseOrderDetail
                {
                    ProductId = item.ProductId,
                    OrderedQuantity = item.OrderedQuantity,
                    Notes = item.Notes
                });
            }

            // 4. Save
            try
            {
                await _poRepository.AddAsync(po);
                return (true, poNumber);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi hệ thống khi tạo Lệnh mua hàng: {ex.Message}");
            }
        }
}

}
