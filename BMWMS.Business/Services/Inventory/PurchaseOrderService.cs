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

        public PurchaseOrderService(IPurchaseOrderRepository poRepository)
        {
            _poRepository = poRepository;
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
                    TotalQuantity = po.PurchaseOrderDetails.Sum(d => d.OrderedQuantity),
                    UnitName = unitName
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
                    Unit = d.Product?.UnitOfMeasure?.ToString() ?? string.Empty,
                    OrderedQuantity = d.OrderedQuantity,
                    UnitPrice = d.UnitPrice ?? 0,
                    Notes = d.Notes
                }).ToList()
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

            if (po.Status != "Draft")
            {
                return (false, $"Không thể xác nhận đơn mua hàng ở trạng thái '{po.Status}'.");
            }

            po.Status = "Confirmed";
            po.ConfirmedByUserId = currentUserId;
            po.ConfirmedAt = DateTime.Now;
            po.UpdatedAt = DateTime.Now;

            await _poRepository.UpdateAsync(po);
            return (true, "Xác nhận đơn mua hàng thành công!");
        }

        public async Task<(bool Success, string Message)> CancelOrderAsync(long purchaseOrderId, long currentUserId, string? reason)
        {
            var po = await _poRepository.GetByIdWithDetailsAsync(purchaseOrderId);
            if (po == null) return (false, "Không tìm thấy đơn mua hàng.");

            if (po.Status == "Completed" || po.Status == "Partial")
            {
                return (false, "Không thể hủy đơn mua hàng đã bắt đầu hoặc hoàn tất nhập kho.");
            }

            po.Status = "Cancelled";
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
            var suppliers = await _poRepository.GetCustomersAsync();
            return suppliers.Select(s => new CustomerLookupDto
            {
                CustomerId = s.CustomerId,
                CustomerCode = s.CustomerCode ?? string.Empty,
                CustomerName = s.CustomerName ?? string.Empty
            });
        }
    }

}
