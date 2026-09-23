using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.Interfaces;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using QuantityRules = BMWMS.Business.Common.QuantityRules;
using PurchaseOrderReceiptRules = BMWMS.Business.Common.PurchaseOrderReceiptRules;
using BusinessRoleCodes = BMWMS.Business.Common.BusinessRoleCodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using OrderWorkflowLock = BMWMS.Business.Common.OrderWorkflowLock;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ILogger<PurchaseOrderService>? _logger;

        public PurchaseOrderService(
            IPurchaseOrderRepository poRepository,
            IEmailService emailService,
            IPurchaseOrderEmailComposer emailComposer,
            BmwmsContext context,
            IAuditLogService auditLogService,
            ILogger<PurchaseOrderService>? logger = null)
        {
            _poRepository = poRepository;
            _emailService = emailService;
            _emailComposer = emailComposer;
            _context = context;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<PagedResultDto<PurchaseOrderListDto>> GetPagedOrdersAsync(PurchaseOrderFilterDto filter)
        {
            var (items, totalCount) = await _poRepository.GetPagedListAsync(
                filter.SearchTerm,
                filter.Status,
                filter.WarehouseId,
                Math.Max(1, filter.PageIndex),
                Math.Clamp(filter.PageSize, 1, 100),
                filter.SortOrder,
                filter.CreatedByUserId
            );

            var pageIds = items.Select(p => p.PurchaseOrderId).ToList();
            var childIds = (await _context.InboundOrders.Where(o => o.PurchaseOrderId.HasValue && pageIds.Contains(o.PurchaseOrderId.Value))
                .Select(o => o.PurchaseOrderId!.Value).ToListAsync()).Concat(await _context.OutboundOrders
                .Where(o => o.PurchaseOrderId.HasValue && pageIds.Contains(o.PurchaseOrderId.Value)).Select(o => o.PurchaseOrderId!.Value).ToListAsync()).ToHashSet();
            var listDtos = items.Select(po =>
            {
                // Lấy đơn vị tính đại diện từ sản phẩm đầu tiên
                var firstDetail = po.PurchaseOrderDetails.FirstOrDefault();
                string unitName = firstDetail?.Product?.UnitOfMeasure?.UnitName ?? string.Empty;

                return new PurchaseOrderListDto
                {
                    CanExternalCancel = (NormalizePurchaseOrderStatus(po.Status) is "APPROVED" or "CONFIRMED") && !childIds.Contains(po.PurchaseOrderId),
                    PurchaseOrderId = po.PurchaseOrderId,
                    PurchaseOrderNumber = po.PurchaseOrderNumber,
                    SupplierId = po.SupplierId,
                    SupplierCode = po.Supplier?.SupplierCode ?? string.Empty,
                    SupplierName = po.Supplier?.SupplierName ?? string.Empty,
                    OrderDate = po.OrderDate,
                    ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                    Status = NormalizePurchaseOrderStatus(po.Status),
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

            var items = po.PurchaseOrderDetails.Where(d => d.IsActive).Select(d =>
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
                CanExternalCancel = (status is "APPROVED" or "CONFIRMED") && !po.InboundOrders.Any() &&
                    !await _context.OutboundOrders.AnyAsync(o => o.PurchaseOrderId == po.PurchaseOrderId),
                RowVersion = Convert.ToBase64String(po.RowVersion),
                ApprovedAt = po.ApprovedAt,
                CanApprove = status == "DRAFT",
                CanEdit = status == "DRAFT" &&
                    !po.InboundOrders.Any() && !await _context.OutboundOrders.AnyAsync(o => o.PurchaseOrderId == po.PurchaseOrderId),
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
                CanSendToSupplier = status == "APPROVED",
                CanCancel = (status is "DRAFT" or "APPROVED" or "CONFIRMED") && !po.InboundOrders.Any() &&
                    !await _context.OutboundOrders.AnyAsync(o => o.PurchaseOrderId == po.PurchaseOrderId),
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
                "PARTIALLYRECEIVED" or "PENDING_REMAINDER_CONFIRMATION" or "PENDING_RECEIPT_REVIEW" => "PARTIALLY_RECEIVED",
                "PENDING_CONFIRMATION" => "DRAFT",
                "CLOSED" or "RECEIVED" => "COMPLETED",
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

        public async Task<(bool Success, string Message)> SendOrderToSupplierAsync(long purchaseOrderId, long currentUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await OrderWorkflowLock.AcquireAsync(_context, "PO", purchaseOrderId);
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");

            var status = NormalizePurchaseOrderStatus(po.Status);
            var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId && u.Status == "ACTIVE");
            var actorRole = BusinessRoleCodes.Normalize(actor?.Role?.RoleCode);
            if (actorRole is not ("PURCHASING_STAFF" or "SYSTEM_ADMIN") ||
                (actorRole == "PURCHASING_STAFF" && po.CreatedByUserId != currentUserId))
                return (false, "Bạn không có quyền gửi đơn mua hàng này.");
            if (status == "CONFIRMED")
                return (true, "PO đã được xác nhận trước đó; hệ thống không gửi trùng.");
            if (status != "APPROVED")
                return (false, "Manager phải duyệt nội dung PO trước khi gửi email cho NCC.");

            var supplierEmail = po.Supplier?.Email?.Trim();
            if (string.IsNullOrWhiteSpace(supplierEmail))
                return (false, "Nhà cung cấp chưa có địa chỉ email. Vui lòng cập nhật thông tin NCC trước khi gửi PO.");

            var sentAt = DateTime.UtcNow;
            try
            {
                var message = _emailComposer.Compose(po);
                await _emailService.SendEmailAsync(message);
            }
            catch (Exception exception)
            {
                return (false, $"Gửi email cho nhà cung cấp thất bại: {exception.Message}");
            }

            po.Status = "CONFIRMED";
            po.ConfirmedByUserId = currentUserId;
            po.ConfirmedAt = sentAt;
            po.UpdatedAt = sentAt;

            try
            {
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = currentUserId,
                ActionType = "SEND_PURCHASE_ORDER_EMAIL",
                EntityName = AuditEntities.PurchaseOrder,
                EntityId = po.PurchaseOrderId.ToString(),
                NewValues = new
                {
                    PreviousStatus = status,
                    Status = "CONFIRMED",
                    RecipientEmail = supplierEmail,
                    EmailSentAt = sentAt,
                    ConfirmationSource = "EMAIL_SENT"
                }
            });

            await _poRepository.UpdateAsync(po);
            await transaction.CommitAsync();
            return (true, "Đã gửi email kèm file Excel và xác nhận đơn mua hàng.");
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, "SMTP accepted PO {PurchaseOrderId} revision {RevisionNo}, but SQL confirmation failed", po.PurchaseOrderId, po.RevisionNo);
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                return (false, "SMTP đã nhận email nhưng hệ thống chưa lưu được xác nhận PO. Không gửi lại ngay; kiểm tra hộp thư đã gửi và nhờ quản trị viên đối soát trạng thái/database trước khi tiếp tục.");
            }
        }

        public async Task<(bool Success, string Message)> CancelOrderAsync(long purchaseOrderId, long currentUserId, string? reason)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await OrderWorkflowLock.AcquireAsync(_context, "PO", purchaseOrderId);
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");

            var status = NormalizePurchaseOrderStatus(po.Status);
            if (status is not ("DRAFT" or "APPROVED" or "CONFIRMED"))
            {
                return (false, "Chỉ được hủy PO chưa bắt đầu nhận hàng.");
            }

            var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId && u.Status == "ACTIVE");
            var actorRole = BusinessRoleCodes.Normalize(actor?.Role?.RoleCode);
            if (actorRole is not ("PURCHASING_STAFF" or "WAREHOUSE_MANAGER" or "SYSTEM_ADMIN") ||
                (actorRole == "PURCHASING_STAFF" && po.CreatedByUserId != currentUserId))
                return (false, "Bạn không có quyền hủy PO này.");
            if (actorRole == "PURCHASING_STAFF" && status != "DRAFT")
                return (false, "PO đã được Manager duyệt; nhân viên mua hàng không được hủy.");
            if (actorRole == "WAREHOUSE_MANAGER" && status == "DRAFT")
                return (false, "PO đang nháp; hãy dùng chức năng từ chối đơn hàng.");
            if (po.InboundOrders.Any() || await _context.OutboundOrders.AnyAsync(o => o.PurchaseOrderId == purchaseOrderId))
                return (false, "Không thể hủy PO vì đã có phiếu nhập/xuất.");

            reason = reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5 || reason.Length > 500)
                return (false, "Lý do hủy phải có từ 5 đến 500 ký tự.");

            po.Status = "CANCELLED";
            po.Notes = BMWMS.Business.Common.OrderWorkflowNotes.AppendIfFits(po.Notes, $"Lý do hủy: {reason}");
            po.UpdatedAt = DateTime.UtcNow;

            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = currentUserId,
                ActionType = "CANCEL_PURCHASE_ORDER",
                EntityName = AuditEntities.PurchaseOrder,
                EntityId = po.PurchaseOrderId.ToString(),
                OldValues = new { Status = status },
                NewValues = new { Status = "CANCELLED", Reason = reason }
            });

            await _poRepository.UpdateAsync(po);
            await transaction.CommitAsync();
            return (true, "Đã hủy đơn mua hàng thành công.");
        }

        public async Task<(bool Success, string Message)> ClosePartiallyReceivedOrderAsync(long purchaseOrderId, long currentUserId, string reason)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await OrderWorkflowLock.AcquireAsync(_context, "PO", purchaseOrderId);
            var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId && u.Status == "ACTIVE");
            if (actor?.Role.RoleCode is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN")) return (false, "Chỉ Manager được kết thúc sớm PO.");
            reason = reason?.Trim() ?? string.Empty;
            if (reason.Length is < 10 or > 500)
                return (false, "Lý do kết thúc sớm phải có từ 10 đến 500 ký tự.");
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");
            var currentStatus = NormalizePurchaseOrderStatus(po.Status);
            if (currentStatus != "PARTIALLY_RECEIVED")
                return (false, "Chỉ được kết thúc sớm PO đã nhận một phần.");
            if (po.InboundOrders.Any(order => PurchaseOrderReceiptRules.IsActiveInbound(order.Status) ||
                order.InboundOrderItems.SelectMany(i => i.InboundOrderDetails).Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null)))
                return (false, "Còn phiếu nhập đang xử lý; phải hoàn tất hoặc hủy phiếu đó trước.");

            var activeInbounds = po.InboundOrders.Where(order => order.Status != "CANCELLED").ToList();
            var snapshots = po.PurchaseOrderDetails.Where(d => d.IsActive).Select(detail => PurchaseOrderReceiptRules.CalculateLine(
                detail.OrderedQuantity,
                activeInbounds.SelectMany(order => order.InboundOrderItems)
                    .Where(item => item.ProductId == detail.ProductId))).ToList();
            var hasCompletedDeliveryAttempt = activeInbounds.Any(order =>
                PurchaseOrderReceiptRules.IsCompletedReceipt(order.Status));
            if (!hasCompletedDeliveryAttempt ||
                !snapshots.Any(snapshot => snapshot.AcceptedQuantity < snapshot.OrderedQuantity))
                return (false, "PO chưa có đợt giao đã hoàn tất nhưng còn thiếu để kết thúc sớm.");

            var supplierEmail = po.Supplier?.Email?.Trim();
            var remainderLines = BuildRemainderLines(po);

            po.Status = "COMPLETED";
            po.UpdatedAt = DateTime.UtcNow;
            po.Notes = BMWMS.Business.Common.OrderWorkflowNotes.AppendIfFits(po.Notes, $"Kết thúc khi chưa nhận đủ: {reason}");
            await _auditLogService.StageAsync(new AuditEventDto
            {
                UserId = currentUserId,
                ActionType = "CLOSE_PARTIAL_PURCHASE_ORDER",
                EntityName = AuditEntities.PurchaseOrder,
                EntityId = po.PurchaseOrderId.ToString(),
                OldValues = new { Status = currentStatus },
                NewValues = new
                {
                    Status = "COMPLETED",
                    Reason = reason,
                    RecipientEmail = supplierEmail,
                    CancelledRemainder = remainderLines.Select(line => new
                    {
                        line.ProductCode,
                        line.RemainingQuantity,
                        line.UnitName
                    })
                }
            });
            await _poRepository.UpdateAsync(po);
            await transaction.CommitAsync();
            try
            {
                if (!string.IsNullOrWhiteSpace(supplierEmail))
                    await _emailService.SendEmailAsync(_emailComposer.ComposePartialClosureNotice(po, remainderLines, reason));
                else return (true, "PO đã kết thúc. NCC thiếu email; Purchasing cần thông báo bên ngoài.");
            }
            catch { return (true, "PO đã kết thúc; gửi thông báo NCC thất bại. Purchasing cần thông báo bên ngoài, không đóng lại phiếu."); }
            return (true, "Đã kết thúc PO theo số thực nhận và gửi thông báo NCC.");
        }

        // Compatibility endpoint: partial receipts no longer need a second approval.
        public Task<(bool Success, string Message)> ContinuePartiallyReceivedOrderAsync(
            long purchaseOrderId, long currentUserId, DateOnly requestedDeliveryDate, string? note)
            => Task.FromResult((false, "PO nhận một phần đã tự mở cho đợt nhập tiếp. Không cần duyệt hoặc gửi lại email để mở phiếu."));

        private static List<PurchaseOrderRemainderLine> BuildRemainderLines(PurchaseOrder po)
        {
            var activeInbounds = po.InboundOrders
                .Where(order => NormalizeInboundStatus(order.Status) != "CANCELLED")
                .ToList();
            return po.PurchaseOrderDetails.Where(d => d.IsActive)
                .Select(detail =>
                {
                    var snapshot = PurchaseOrderReceiptRules.CalculateLine(
                        detail.OrderedQuantity,
                        activeInbounds.SelectMany(order => order.InboundOrderItems)
                            .Where(item => item.ProductId == detail.ProductId));
                    return new PurchaseOrderRemainderLine(
                        detail.Product?.ProductCode ?? string.Empty,
                        detail.Product?.ProductName ?? string.Empty,
                        detail.Product?.UnitOfMeasure?.UnitName ?? detail.Product?.UnitOfMeasure?.UnitCode ?? string.Empty,
                        detail.Product?.UnitOfMeasure?.QuantityScale ?? 0,
                        snapshot.OrderedQuantity,
                        snapshot.AcceptedQuantity,
                        snapshot.RejectedQuantity,
                        Math.Max(0, snapshot.OrderedQuantity - snapshot.AcceptedQuantity));
                })
                .Where(line => line.RemainingQuantity > 0)
                .ToList();
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
    
        public async Task<(bool Success, string Message)> ApproveAsync(long id, long userId, string? rowVersion = null)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await OrderWorkflowLock.AcquireAsync(_context, "PO", id);
            var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId && u.Status == "ACTIVE");
            if (actor?.Role.RoleCode is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN")) return (false, "Chỉ Manager được duyệt PO.");
            var po = await _poRepository.GetByIdWithDetailsAsync(id);
            if (po == null || NormalizePurchaseOrderStatus(po.Status) != "DRAFT") return (false, "PO không còn ở trạng thái nháp.");
            if (await _context.InboundOrders.AnyAsync(o => o.PurchaseOrderId == id) || await _context.OutboundOrders.AnyAsync(o => o.PurchaseOrderId == id))
                return (false, "PO đã có phiếu nhập/xuất; cần đối soát dữ liệu cũ trước khi duyệt.");
            if (rowVersion != null && rowVersion != Convert.ToBase64String(po.RowVersion)) return (false, "Nội dung PO đã thay đổi. Tải lại và xem xét phiên bản mới trước khi duyệt.");
            if (actor.Role.RoleCode != "SYSTEM_ADMIN" && po.CreatedByUserId == userId)
                return (false, "Người tạo không được tự duyệt PO.");
            if (!po.PurchaseOrderDetails.Any(d => d.IsActive) || po.Supplier?.Status != "ACTIVE" || string.IsNullOrWhiteSpace(po.Supplier.Email))
                return (false, "PO phải có vật tư và NCC hoạt động có email.");
            po.Status = "APPROVED";
            po.ApprovedByUserId = userId;
            po.ConfirmedByUserId = null;
            po.ConfirmedAt = null;
            po.ApprovedAt = po.UpdatedAt = DateTime.UtcNow;
            await _auditLogService.StageAsync(new AuditEventDto { UserId = userId, ActionType = "APPROVE_PURCHASE_ORDER",
                EntityName = AuditEntities.PurchaseOrder, EntityId = id.ToString(),
                NewValues = new { po.Status, po.RevisionNo, po.ApprovedAt } });
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, "Đã duyệt PO. Purchasing có thể gửi mail cho NCC.");
        }

        public async Task<(bool Success, string Message)> RejectDraftAsync(long id, long userId, string reason, string rowVersion)
        {
            reason = reason?.Trim() ?? "";
            if (reason.Length is < 10 or > 500) return (false, "Lý do từ chối phải từ 10 đến 500 ký tự.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await OrderWorkflowLock.AcquireAsync(_context, "PO", id);
            var actor = await _context.Users.Include(u => u.Role).AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Status == "ACTIVE");
            if (actor?.Role?.RoleCode is not ("WAREHOUSE_MANAGER" or "SYSTEM_ADMIN")) return (false, "Chỉ Manager được từ chối PO.");
            var po = await _poRepository.GetByIdWithDetailsAsync(id);
            if (po == null || NormalizePurchaseOrderStatus(po.Status) != "DRAFT") return (false, "Chỉ được từ chối PO đang nháp.");
            if (rowVersion != Convert.ToBase64String(po.RowVersion)) return (false, "PO đã thay đổi. Tải lại trước khi từ chối.");
            if (po.InboundOrders.Any() || await _context.OutboundOrders.AnyAsync(o => o.PurchaseOrderId == id))
                return (false, "PO đã có phiếu nhập/xuất; không được từ chối.");
            po.Status = "REJECTED";
            po.Notes = BMWMS.Business.Common.OrderWorkflowNotes.AppendIfFits(po.Notes, $"Quản lý từ chối: {reason}");
            po.UpdatedAt = DateTime.UtcNow;
            await _auditLogService.StageAsync(new AuditEventDto { UserId = userId, ActionType = "REJECT_PURCHASE_ORDER",
                EntityName = AuditEntities.PurchaseOrder, EntityId = id.ToString(),
                OldValues = new { Status = "DRAFT" }, NewValues = new { po.Status, Reason = reason } });
            await _poRepository.UpdateAsync(po);
            await tx.CommitAsync();
            return (true, "Đã từ chối đơn mua hàng.");
        }

        public async Task<(bool Success, string Message)> UpdateAsync(long id, PurchaseOrderCreateDto request, long userId)
        {
            if (request.OrderDetails == null || request.OrderDetails.Count == 0 || (request.Notes?.Length ?? 0) > 2000 || request.OrderDetails.Any(d => (d.Notes?.Length ?? 0) > 1000) ||
                request.OrderDetails.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
                return (false, "Danh sách vật tư hoặc ghi chú không hợp lệ.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await OrderWorkflowLock.AcquireAsync(_context, "PO", id);
            var actor = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId && u.Status == "ACTIVE");
            var po = await _context.PurchaseOrders.IgnoreQueryFilters().Include(p => p.PurchaseOrderDetails).FirstOrDefaultAsync(p => p.PurchaseOrderId == id);
            if (po == null || NormalizePurchaseOrderStatus(po.Status) != "DRAFT") return (false, "Chỉ được sửa PO nháp trước khi Manager duyệt.");
            if (request.ExpectedDeliveryDate.HasValue && request.ExpectedDeliveryDate.Value < po.OrderDate)
                return (false, "Ngày giao dự kiến không được trước ngày đặt hàng.");
            var actorRole = BusinessRoleCodes.Normalize(actor?.Role?.RoleCode);
            if (actorRole is not ("PURCHASING_STAFF" or "SYSTEM_ADMIN") ||
                (actorRole == "PURCHASING_STAFF" && po.CreatedByUserId != userId)) return (false, "Bạn không được sửa PO này.");
            if (request.RowVersion != Convert.ToBase64String(po.RowVersion)) return (false, "PO đã thay đổi. Vui lòng tải lại trước khi sửa.");
            if (await _context.InboundOrders.AnyAsync(o => o.PurchaseOrderId == id) || await _context.OutboundOrders.AnyAsync(o => o.PurchaseOrderId == id))
                return (false, "PO đã có phiếu nhập/xuất; không được sửa.");
            if (!await _context.Suppliers.AnyAsync(s => s.SupplierId == request.SupplierId && s.Status == "ACTIVE" && s.Email != null && s.Email != ""))
                return (false, "NCC phải hoạt động và có email.");
            var ids = request.OrderDetails.Select(i => i.ProductId).ToList();
            var products = await _context.Products.Include(p => p.UnitOfMeasure).Where(p => ids.Contains(p.ProductId) && p.Status == "ACTIVE").ToDictionaryAsync(p => p.ProductId);
            if (products.Count != ids.Count) return (false, "Vật tư không tồn tại hoặc đã ngừng sử dụng.");
            try { foreach (var item in request.OrderDetails) QuantityRules.EnsureValid(products[item.ProductId], item.OrderedQuantity, "Số lượng đặt"); }
            catch (ArgumentException ex) { return (false, ex.Message); }
            var old = new { po.Status, po.SupplierId, po.RevisionNo, Items = po.PurchaseOrderDetails.Where(d => d.IsActive).Select(d => new { d.ProductId, d.OrderedQuantity }).ToList() };
            foreach (var d in po.PurchaseOrderDetails)
            {
                var item = request.OrderDetails.SingleOrDefault(i => i.ProductId == d.ProductId);
                d.IsActive = item != null;
                if (item != null) { d.OrderedQuantity = item.OrderedQuantity; d.Notes = item.Notes; }
            }
            foreach (var item in request.OrderDetails.Where(i => !po.PurchaseOrderDetails.Any(d => d.ProductId == i.ProductId)))
                po.PurchaseOrderDetails.Add(new PurchaseOrderDetail { ProductId = item.ProductId, OrderedQuantity = item.OrderedQuantity, Notes = item.Notes });
            po.SupplierId = request.SupplierId;
            // Ngày đặt là dấu thời gian do hệ thống cấp khi tạo PO, không cho sửa lại.
            po.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
            po.Notes = request.Notes;
            po.Status = "DRAFT";
            po.ApprovedAt = po.ConfirmedAt = null;
            po.ApprovedByUserId = po.ConfirmedByUserId = null;
            po.RevisionNo++;
            po.UpdatedAt = DateTime.UtcNow;
            await _auditLogService.StageAsync(new AuditEventDto { UserId = userId, ActionType = "UPDATE_PURCHASE_ORDER",
                EntityName = AuditEntities.PurchaseOrder, EntityId = id.ToString(), OldValues = old,
                NewValues = new { po.Status, po.RevisionNo, request.SupplierId, request.OrderDetails } });
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, "Đã sửa PO và đưa về nháp để duyệt lại. Email cũ không còn đại diện nội dung mới.");
        }

        public async Task<(bool Success, string Message)> CreatePurchaseOrderAsync(PurchaseOrderCreateDto request, long userId)
        {
            var actor = await _context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == userId && u.Status == "ACTIVE");
            if (BusinessRoleCodes.Normalize(actor?.Role?.RoleCode) is not ("PURCHASING_STAFF" or "SYSTEM_ADMIN"))
                return (false, "Chỉ Purchasing Staff được tạo đơn mua hàng.");
            if (request.OrderDetails == null || (request.Notes?.Length ?? 0) > 2000 || request.OrderDetails.Any(d => (d.Notes?.Length ?? 0) > 1000))
                return (false, "Danh sách vật tư hoặc ghi chú không hợp lệ.");
            if (!await _context.Suppliers.AnyAsync(s => s.SupplierId == request.SupplierId && s.Status == "ACTIVE" && s.Email != null && s.Email != ""))
                return (false, "Nhà cung cấp phải đang hoạt động và có email.");
            // 1. Validate
            var systemOrderDate = DateOnly.FromDateTime(DateTime.Today);
            if (request.ExpectedDeliveryDate.HasValue && request.ExpectedDeliveryDate.Value < systemOrderDate)
            {
                return (false, "Ngày giao dự kiến không được nhỏ hơn ngày đặt hàng.");
            }
            if (!request.OrderDetails.Any())
            {
                return (false, "Đơn mua hàng phải có ít nhất một vật tư.");
            }

            var duplicateProducts = request.OrderDetails.GroupBy(x => x.ProductId).Where(g => g.Count() > 1).ToList();
            if (duplicateProducts.Any())
            {
                return (false, "Không được chọn trùng lặp vật tư trong cùng một đơn mua hàng.");
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
            await using var creationTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await OrderWorkflowLock.AcquireAsync(_context, "PO_NUMBER", DateTime.Today.Year);
            string poNumber = await _poRepository.GeneratePurchaseOrderNumberAsync();

            // 3. Map to Entity
            var po = new BMWMS.Repository.Models.PurchaseOrder
            {
                PurchaseOrderNumber = poNumber,
                SupplierId = request.SupplierId,
                OrderDate = systemOrderDate,
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
                await _auditLogService.StageAsync(new AuditEventDto { UserId = userId, ActionType = "CREATE_PURCHASE_ORDER",
                    EntityName = AuditEntities.PurchaseOrder, EntityId = po.PurchaseOrderId.ToString(),
                    NewValues = new { po.PurchaseOrderNumber, po.Status, po.SupplierId, request.OrderDetails } });
                await _context.SaveChangesAsync();
                await creationTransaction.CommitAsync();
                return (true, poNumber);
            }
            catch (Exception ex)
            {
                await creationTransaction.RollbackAsync();
                _logger?.LogError(ex, "Cannot persist new purchase order");
                return (false, "Không thể lưu đơn mua hàng. Kiểm tra kết nối và schema database; không gửi lại khi chưa kiểm tra danh sách.");
            }
        }
}

}
