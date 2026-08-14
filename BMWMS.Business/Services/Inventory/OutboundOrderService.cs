using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMWMS.Business.Services.Inventory
{
    public class OutboundOrderService : IOutboundOrderService
    {
        private readonly IOutboundOrderRepository _outboundOrderRepo;

        public OutboundOrderService(IOutboundOrderRepository outboundOrderRepo)
        {
            _outboundOrderRepo = outboundOrderRepo;
        }

        public async Task<PagedResultDto<OutboundOrderListDto>> GetOutboundOrdersAsync(OutboundOrderQueryFilter filter)
        {
            // 1. Validate tham số phân trang
            int pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            int pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

            // 2. Lấy dữ liệu từ Repository (Đã được Include đầy đủ Quan hệ)
            var rawOrders = await _outboundOrderRepo.GetAllAsync(filter.Search, filter.Status, filter.WarehouseId);

            // 3. Đếm số lượng tổng
            int totalCount = rawOrders.Count();

            // 4. Phân trang & Mapping DTOs (Trả về Raw Status từ DB)
            var pagedOrders = rawOrders
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OutboundOrderListDto
                {
                    OutboundOrderId = o.OutboundOrderId,
                    OutboundOrderNumber = o.OutboundOrderNumber,
                    SourceType = o.SourceType,
                    SalesOrderId = o.SalesOrderId,
                    SalesOrderNumber = o.SalesOrder?.SalesOrderNumber ?? "N/A",

                    // Dynamic Customer Name từ Navigation Property
                    CustomerName = o.SalesOrder?.Customer?.CustomerName ?? "N/A",

                    WarehouseId = o.WarehouseId,
                    WarehouseName = o.Warehouse?.WarehouseName ?? "N/A",
                    Status = o.Status, // Chuẩn Database Status (DRAFT, ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED)
                    TotalRequestedQuantity = o.OutboundOrderItems.Sum(i => i.RequestedQuantity),
                    TotalIssuedQuantity = o.OutboundOrderItems.Sum(i => i.IssuedQuantity),
                    ExpectedIssueDate = o.ExpectedIssueDate,
                    CreatedAt = o.CreatedAt
                })
                .ToList();

            // 5. Trả kết quả phân trang
            return new PagedResultDto<OutboundOrderListDto>
            {
                Items = pagedOrders,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<OutboundOrderDetailDto?> GetOutboundOrderByIdAsync(long id)
        {
            var order = await _outboundOrderRepo.GetByIdAsync(id);
            if (order == null) return null;

            return new OutboundOrderDetailDto
            {
                OutboundOrderId = order.OutboundOrderId,
                OutboundOrderNumber = order.OutboundOrderNumber,
                SourceType = order.SourceType,
                SalesOrderId = order.SalesOrderId,
                SalesOrderNumber = order.SalesOrder?.SalesOrderNumber ?? "N/A",

                // Dynamic Customer Name
                CustomerName = order.SalesOrder?.Customer?.CustomerName ?? "N/A",

                WarehouseId = order.WarehouseId,
                WarehouseName = order.Warehouse?.WarehouseName ?? "N/A",
                ExpectedIssueDate = order.ExpectedIssueDate,
                AssignedToUserId = order.AssignedToUserId,

                // Dynamic User Name
                AssignedToUserName = order.AssignedToUser != null
                    ? (!string.IsNullOrEmpty(order.AssignedToUser.FullName) ? order.AssignedToUser.FullName : order.AssignedToUser.Username)
                    : "Unassigned",

                Status = order.Status,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                Items = order.OutboundOrderItems.Select(i => new OutboundOrderItemDto
                {
                    OutboundOrderItemId = i.OutboundOrderItemId,
                    ProductId = i.ProductId,
                    ProductCode = i.Product?.ProductCode ?? "",
                    ProductName = i.Product?.ProductName ?? "",
                    UnitOfMeasure = i.Product?.UnitOfMeasure?.UnitCode ?? "",
                    RequestedQuantity = i.RequestedQuantity,
                    IssuedQuantity = i.IssuedQuantity,
                    Notes = i.Notes
                }).ToList()
            };
        }

        public async Task<OutboundOrderDetailDto> CreateOutboundOrderAsync(CreateOutboundOrderRequest request, long createdByUserId)
        {
            if (request.Items == null || !request.Items.Any())
            {
                throw new ArgumentException("Danh sách sản phẩm xuất kho không được để trống!");
            }

            string orderCode = $"OUT-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            // Gán Status theo chuẩn DB Check Constraint: ASSIGNED hoặc DRAFT
            string initialStatus = request.IsSubmit
                ? (request.AssignedToUserId.HasValue ? "ASSIGNED" : "IN_PROGRESS")
                : "DRAFT";

            var outboundOrder = new OutboundOrder
            {
                OutboundOrderNumber = orderCode,
                WarehouseId = request.WarehouseId,
                SourceType = request.SourceType, // Phải thuộc SALES_ORDER, PURCHASE_RETURN, TRANSFER_ORDER
                SalesOrderId = request.SalesOrderId,
                ExpectedIssueDate = request.ExpectedIssueDate,
                AssignedToUserId = request.AssignedToUserId,
                Notes = request.Notes,
                Status = initialStatus,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.Now
            };

            foreach (var item in request.Items)
            {
                outboundOrder.OutboundOrderItems.Add(new OutboundOrderItem
                {
                    ProductId = item.ProductId,
                    RequestedQuantity = item.RequestedQuantity,
                    IssuedQuantity = 0,
                    Notes = item.Notes
                });
            }

            var createdEntity = await _outboundOrderRepo.CreateAsync(outboundOrder);
            return (await GetOutboundOrderByIdAsync(createdEntity.OutboundOrderId))!;
        }

        public async Task<bool> UpdateStatusAsync(long outboundOrderId, string newStatus)
        {
            var exists = await _outboundOrderRepo.ExistsAsync(outboundOrderId);
            if (!exists) return false;

            return await _outboundOrderRepo.UpdateStatusAsync(outboundOrderId, newStatus);
        }

        public async Task<bool> CancelOutboundOrderAsync(long outboundOrderId)
        {
            var order = await _outboundOrderRepo.GetByIdAsync(outboundOrderId);
            if (order == null) return false;

            // Kiểm tra theo DB Status Enum: COMPLETED
            if (order.Status == "COMPLETED")
            {
                throw new InvalidOperationException("Không thể hủy lệnh xuất kho đã hoàn thành (COMPLETED)!");
            }

            return await _outboundOrderRepo.UpdateStatusAsync(outboundOrderId, "CANCELLED");
        }

        // Thêm phương thức xử lý Màn 2 vào OutboundOrderService
        public async Task<OutboundProcessViewDto?> GetOutboundProcessDetailAsync(long outboundOrderId)
        {
            var order = await _outboundOrderRepo.GetByIdAsync(outboundOrderId);
            if (order == null) return null;

            var result = new OutboundProcessViewDto
            {
                OutboundOrderId = order.OutboundOrderId,
                OutboundOrderNumber = order.OutboundOrderNumber,
                SourceType = order.SourceType,
                SalesOrderNumber = order.SalesOrder?.SalesOrderNumber ?? "N/A",
                CustomerName = order.SalesOrder?.Customer?.CustomerName ?? "N/A",
                WarehouseId = order.WarehouseId,
                WarehouseName = order.Warehouse?.WarehouseName ?? "N/A",
                Status = order.Status,
                Notes = order.Notes,
                Items = new List<OutboundProcessItemDto>()
            };

            foreach (var item in order.OutboundOrderItems)
            {
                var itemDto = new OutboundProcessItemDto
                {
                    OutboundOrderItemId = item.OutboundOrderItemId,
                    ProductId = item.ProductId,
                    ProductCode = item.Product?.ProductCode ?? "",
                    ProductName = item.Product?.ProductName ?? "",
                    UnitName = item.Product?.UnitOfMeasure?.UnitName ?? "Đơn vị",
                    RequestedQuantity = item.RequestedQuantity,
                    IssuedQuantity = item.IssuedQuantity,
                    PickedDetails = item.OutboundOrderDetails.Select(d => new OutboundPickedDetailDto
                    {
                        OutboundOrderDetailId = d.OutboundOrderDetailId,
                        LocationCode = d.StorageLocation?.LocationCode ?? "N/A",
                        LotNumber = d.ProductLot?.LotNumber ?? "N/A",
                        IssuedQuantity = d.IssuedQuantity,
                        RecordedByUserName = d.RecordedByUser?.FullName ?? d.RecordedByUser?.Username ?? "N/A",
                        RecordedAt = d.RecordedAt
                    }).ToList()
                };

                result.Items.Add(itemDto);
            }

            return result;
        }

        public async Task<(bool Success, string Message)> ExecutePickAsync(ExecutePickItemRequest request, long userId)
        {
            var order = await _outboundOrderRepo.GetByIdAsync(request.OutboundOrderId);
            if (order == null) return (false, "Không tìm thấy Lệnh xuất kho!");

            if (order.Status == "COMPLETED" || order.Status == "CANCELLED")
                return (false, "Lệnh xuất kho đã hoàn thành hoặc bị hủy, không thể thực hiện!");

            var item = order.OutboundOrderItems.FirstOrDefault(x => x.OutboundOrderItemId == request.OutboundOrderItemId);
            if (item == null) return (false, "Sản phẩm không thuộc Lệnh xuất kho này!");

            decimal remaining = item.RequestedQuantity - item.IssuedQuantity;
            if (request.PickQuantity > remaining)
            {
                return (false, $"Số lượng lấy ({request.PickQuantity}) vượt quá số lượng còn thiếu ({remaining})!");
            }

            var detail = new OutboundOrderDetail
            {
                OutboundOrderId = request.OutboundOrderId,
                OutboundOrderItemId = request.OutboundOrderItemId,
                ProductId = item.ProductId,
                StorageLocationId = request.StorageLocationId,
                ProductLotId = request.ProductLotId,
                InventoryReservationId = request.InventoryReservationId,
                IssuedQuantity = request.PickQuantity,
                RecordedByUserId = userId,
                RecordedAt = DateTime.Now,
                Notes = request.Notes
            };

            // 2. Cập nhật cộng dồn IssuedQuantity trong OutboundOrderItem
            item.IssuedQuantity += request.PickQuantity;

            // 3. Cập nhật Status Lệnh Outbound
            bool isAllCompleted = order.OutboundOrderItems.All(i => i.IssuedQuantity >= i.RequestedQuantity);
            order.Status = isAllCompleted ? "COMPLETED" : "IN_PROGRESS";

            // 4. Lưu DB (Cần Inject DBContext/UnitOfWork để Save + Trừ Tồn Kho)
            await _outboundOrderRepo.SavePickDetailAsync(detail, item, order);

            return (true, isAllCompleted ? "Đã pick đủ hàng! Lệnh xuất kho đã HOÀN THÀNH." : "Ghi nhận Pick hàng thành công!");
        }
    }
}