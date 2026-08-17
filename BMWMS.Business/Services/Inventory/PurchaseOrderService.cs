using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using QuantityRules = BMWMS.Business.Common.QuantityRules;
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
        private readonly BmwmsContext _context;

        public PurchaseOrderService(IPurchaseOrderRepository poRepository, IEmailService emailService, BmwmsContext context)
        {
            _poRepository = poRepository;
            _emailService = emailService;
            _context = context;
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
                var plannedQuantity = inboundItems.Sum(item => item.ExpectedQuantity);
                var receivedQuantity = inboundItems.Sum(item => item.ReceivedQuantity);

                return new PurchaseOrderItemDto
                {
                    PurchaseOrderDetailId = d.PurchaseOrderDetailId,
                    ProductId = d.ProductId,
                    ProductCode = d.Product?.ProductCode ?? string.Empty,
                    ProductName = d.Product?.ProductName ?? string.Empty,
                    Unit = d.Product?.UnitOfMeasure?.UnitName ?? string.Empty,
                    OrderedQuantity = d.OrderedQuantity,
                    PlannedInboundQuantity = plannedQuantity,
                    ReceivedQuantity = receivedQuantity,
                    RemainingQuantity = Math.Max(0, d.OrderedQuantity - receivedQuantity),
                    Notes = d.Notes
                };
            }).ToList();

            var hasActiveInbound = activeInbounds.Count > 0;
            var hasQuantityToPlan = po.PurchaseOrderDetails.Any(d =>
            {
                var plannedQuantity = activeInbounds
                    .SelectMany(io => io.InboundOrderItems)
                    .Where(item => item.ProductId == d.ProductId)
                    .Sum(item => item.ExpectedQuantity);
                return plannedQuantity < d.OrderedQuantity;
            });

            return new PurchaseOrderDetailDto
            {
                PurchaseOrderId = po.PurchaseOrderId,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                SupplierId = po.SupplierId,
                SupplierCode = po.Supplier?.SupplierCode ?? string.Empty,
                SupplierName = po.Supplier?.SupplierName ?? string.Empty,
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
                CanConfirm = status == "DRAFT",
                CanCancel = (status == "DRAFT" || status == "CONFIRMED") && !hasActiveInbound,
                CanCreateInbound = (status == "CONFIRMED" || status == "PARTIALLY_RECEIVED") && hasQuantityToPlan,
                Items = items,
                Inbounds = po.InboundOrders.Select(io => new RelatedInboundDto
                {
                    InboundOrderId = io.InboundOrderId,
                    InboundOrderNumber = io.InboundOrderNumber,
                    ExpectedReceiptDate = io.ExpectedReceiptDate,
                    Status = NormalizeInboundStatus(io.Status),
                    Notes = io.Notes,
                    ExpectedQuantity = io.InboundOrderItems.Sum(item => item.ExpectedQuantity),
                    ReceivedQuantity = io.InboundOrderItems.Sum(item => item.ReceivedQuantity),
                    IsSupplemental = io.ParentInboundOrderId.HasValue
                }).OrderByDescending(io => io.ExpectedReceiptDate).ToList()
            };
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

        public async Task<(bool Success, string Message, long OrderId)> CreateOrderAsync(CreatePurchaseOrderDto dto, long currentUserId)
        {
            if (dto.Items == null || !dto.Items.Any())
            {
                return (false, "Đơn mua hàng phải chọn ít nhất 1 sản phẩm.", 0);
            }

            // 1. Tự động sinh mã PO
            string poNumber = await _poRepository.GeneratePurchaseOrderNumberAsync();

            // 2. Xác định trạng thái ban đầu: "Draft" (Nháp) hoặc "Confirmed" (Đã xác nhận)
            string initialStatus = dto.IsSubmitForConfirmation ? "Confirmed" : "Draft";

            var poEntity = new PurchaseOrder
            {
                PurchaseOrderNumber = poNumber,
                SupplierId = dto.SupplierId,
                OrderDate = dto.OrderDate,
                ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                Status = initialStatus,
                Notes = dto.Notes,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.Now
            };

            if (dto.IsSubmitForConfirmation)
            {
                poEntity.ConfirmedByUserId = currentUserId;
                poEntity.ConfirmedAt = DateTime.Now;
            }

            // 3. Thêm danh sách chi tiết sản phẩm
            foreach (var item in dto.Items)
            {
                poEntity.PurchaseOrderDetails.Add(new PurchaseOrderDetail
                {
                    ProductId = item.ProductId,
                    OrderedQuantity = item.OrderedQuantity,
                    UnitPrice = item.UnitPrice,
                    Notes = item.Notes
                });
            }

            // 4. Lưu vào Database
            var createdEntity = await _poRepository.AddAsync(poEntity);

            if (dto.IsSubmitForConfirmation)
            {
                var supplier = await _context.Suppliers.FindAsync(dto.SupplierId);
                if (supplier != null && !string.IsNullOrEmpty(supplier.Email))
                {
                    string subject = $"Xác nhận Đơn đặt hàng {poNumber}";
                    string body = $@"
                        <h3>Kính gửi {supplier.SupplierName},</h3>
                        <p>Đơn đặt hàng <strong>{poNumber}</strong> của chúng tôi đã được xác nhận.</p>
                        <p>Ngày đặt: {dto.OrderDate:dd/MM/yyyy}</p>
                        <p>Ngày giao dự kiến: {dto.ExpectedDeliveryDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định"}</p>
                        <p>Số lượng vật tư: {dto.Items.Sum(d => d.OrderedQuantity)}</p>
                        <br/>
                        <p>Trân trọng,<br/>BMWMS System</p>
                    ";
                    try
                    {
                        await _emailService.SendEmailAsync(supplier.Email, subject, body);
                    }
                    catch
                    {
                        // Ignore email error
                    }
                }
            }

            return (true, dto.IsSubmitForConfirmation ? "Tạo và xác nhận đơn thành công!" : "Lưu đơn nháp thành công!", createdEntity.PurchaseOrderId);
        }

        public async Task<(bool Success, string Message)> ConfirmOrderAsync(long purchaseOrderId, long currentUserId)
        {
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");

            if (NormalizePurchaseOrderStatus(po.Status) != "DRAFT")
            {
                return (false, $"Không thể xác nhận đơn mua hàng ở trạng thái '{po.Status}'.");
            }

            po.Status = "CONFIRMED";
            po.ConfirmedByUserId = currentUserId;
            po.ConfirmedAt = DateTime.Now;
            po.UpdatedAt = DateTime.Now;

            await _poRepository.UpdateAsync(po);

            // Send email to supplier
            if (po.Supplier != null && !string.IsNullOrEmpty(po.Supplier.Email))
            {
                string subject = $"Xác nhận Đơn đặt hàng {po.PurchaseOrderNumber}";
                string body = $@"
                    <h3>Kính gửi {po.Supplier.SupplierName},</h3>
                    <p>Đơn đặt hàng <strong>{po.PurchaseOrderNumber}</strong> của chúng tôi đã được xác nhận.</p>
                    <p>Ngày đặt: {po.OrderDate:dd/MM/yyyy}</p>
                    <p>Ngày giao dự kiến: {po.ExpectedDeliveryDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định"}</p>
                    <p>Số lượng vật tư: {po.PurchaseOrderDetails.Sum(d => d.OrderedQuantity)}</p>
                    <br/>
                    <p>Trân trọng,<br/>BMWMS System</p>
                ";
                try
                {
                    await _emailService.SendEmailAsync(po.Supplier.Email, subject, body);
                }
                catch
                {
                    // Xác nhận PO là nghiệp vụ chính; lỗi kênh thông báo không được làm sai trạng thái đã lưu.
                }
            }

            return (true, "Xác nhận đơn mua hàng thành công!");
        }

        public async Task<(bool Success, string Message)> CancelOrderAsync(long purchaseOrderId, long currentUserId, string? reason)
        {
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");

            var status = NormalizePurchaseOrderStatus(po.Status);
            if (status != "DRAFT" && status != "CONFIRMED")
            {
                return (false, "Chỉ có thể hủy đơn mua hàng ở trạng thái Nháp hoặc Đã xác nhận.");
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
            po.UpdatedAt = DateTime.Now;

            await _poRepository.UpdateAsync(po);

            // Send email to supplier for cancellation
            if (po.Supplier != null && !string.IsNullOrEmpty(po.Supplier.Email))
            {
                string subject = $"Hủy Đơn đặt hàng {po.PurchaseOrderNumber}";
                string body = $@"
                    <h3>Kính gửi {po.Supplier.SupplierName},</h3>
                    <p>Đơn đặt hàng <strong>{po.PurchaseOrderNumber}</strong> của chúng tôi đã bị hủy.</p>
                    <p>Lý do hủy: {reason}</p>
                    <br/>
                    <p>Trân trọng,<br/>BMWMS System</p>
                ";
                try
                {
                    await _emailService.SendEmailAsync(po.Supplier.Email, subject, body);
                }
                catch
                {
                    // Ignore email error
                }
            }

            return (true, "Đã hủy đơn mua hàng thành công.");
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
