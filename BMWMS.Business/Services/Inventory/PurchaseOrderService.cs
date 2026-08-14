using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
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

        public PurchaseOrderService(IPurchaseOrderRepository poRepository, IEmailService emailService)
        {
            _poRepository = poRepository;
            _emailService = emailService;
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

            var inboundOrder = po.InboundOrders.FirstOrDefault();

            return new PurchaseOrderDetailDto
            {
                PurchaseOrderId = po.PurchaseOrderId,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                SupplierId = po.SupplierId,
                SupplierCode = po.Supplier?.SupplierCode ?? string.Empty,
                SupplierName = po.Supplier?.SupplierName ?? string.Empty,
                OrderDate = po.OrderDate,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                WarehouseId = inboundOrder?.WarehouseId,
                WarehouseName = inboundOrder?.Warehouse?.WarehouseName,
                Status = po.Status,
                Notes = po.Notes,
                CreatedByUserId = po.CreatedByUserId,
                CreatedByUserName = po.CreatedByUser?.FullName ?? po.CreatedByUser?.Username ?? string.Empty,
                CreatedAt = po.CreatedAt,
                ConfirmedByUserId = po.ConfirmedByUserId,
                ConfirmedByUserName = po.ConfirmedByUser?.FullName,
                ConfirmedAt = po.ConfirmedAt,
                TotalAmount = po.PurchaseOrderDetails.Sum(d => d.OrderedQuantity * (d.UnitPrice ?? 0)),
                Items = po.PurchaseOrderDetails.Select(d => new PurchaseOrderItemDto
                {
                    PurchaseOrderDetailId = d.PurchaseOrderDetailId,
                    ProductId = d.ProductId,
                    ProductCode = d.Product?.ProductCode ?? string.Empty,
                    ProductName = d.Product?.ProductName ?? string.Empty,
                    Unit = d.Product?.UnitOfMeasure?.UnitName ?? string.Empty,
                    OrderedQuantity = d.OrderedQuantity,
                    ReceivedQuantity = po.InboundOrders != null 
                        ? po.InboundOrders.SelectMany(io => io.InboundOrderItems)
                                          .Where(ioItem => ioItem.ProductId == d.ProductId)
                                          .Sum(ioItem => ioItem.ReceivedQuantity)
                        : 0m,
                    UnitPrice = d.UnitPrice ?? 0,
                    Notes = d.Notes
                }).ToList(),
                Inbounds = po.InboundOrders != null ? po.InboundOrders.Select(io => new RelatedInboundDto
                {
                    InboundOrderId = io.InboundOrderId,
                    InboundOrderNumber = io.InboundOrderNumber,
                    ExpectedReceiptDate = io.ExpectedReceiptDate,
                    Status = io.Status,
                    Notes = io.Notes
                }).ToList() : new List<RelatedInboundDto>()
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

            return (true, dto.IsSubmitForConfirmation ? "Tạo và xác nhận đơn thành công!" : "Lưu đơn nháp thành công!", createdEntity.PurchaseOrderId);
        }

        public async Task<(bool Success, string Message)> ConfirmOrderAsync(long purchaseOrderId, long currentUserId)
        {
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");

            if (po.Status != "DRAFT" && po.Status != "Draft")
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
                await _emailService.SendEmailAsync(po.Supplier.Email, subject, body);
            }

            return (true, "Xác nhận đơn mua hàng thành công!");
        }

        public async Task<(bool Success, string Message)> CancelOrderAsync(long purchaseOrderId, long currentUserId, string? reason)
        {
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");

            if (po.Status != "DRAFT")
            {
                return (false, "Chỉ có thể hủy đơn mua hàng ở trạng thái Nháp (DRAFT).");
            }

            po.Status = "CANCELLED";
            po.Notes = string.IsNullOrWhiteSpace(reason) ? po.Notes : $"{po.Notes} [Lý do hủy: {reason}]";
            po.UpdatedAt = DateTime.Now;

            await _poRepository.UpdateAsync(po);
            return (true, "Đã hủy đơn mua hàng thành công!");
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
