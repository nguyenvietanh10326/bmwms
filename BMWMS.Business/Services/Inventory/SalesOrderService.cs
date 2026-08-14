using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using BMWMS.Repository.Repositories.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Services.Inventory
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IInventoryRepository _invenRepository;

        public SalesOrderService(
            ISalesOrderRepository salesOrderRepository, IInventoryRepository invenRepository)
        {
            _salesOrderRepository = salesOrderRepository;
            _invenRepository = invenRepository;
        }

        public async Task<SalesOrderDetailApiResponse?>
            GetSalesOrderDetailForOutboundAsync(
                long outboundOrderId)
        {
            var outboundOrder =
                await _salesOrderRepository
                    .GetOutboundOrderDetailAsync(outboundOrderId);

            if (outboundOrder == null)
            {
                return null;
            }

            var salesOrder = outboundOrder.SalesOrder;

            if (salesOrder == null)
            {
                return null;
            }

            var items = new List<SalesOrderItemDto>();

            foreach (var detail in salesOrder.SalesOrderDetails)
            {
                // Lấy danh sách Lot + Bin đã reserve
                var lotBinList =
                    await _salesOrderRepository
                        .GetReservedLotBinInfoAsync(
                            detail.SalesOrderDetailId);

                var lotBinInfo = lotBinList.Any()
                    ? string.Join(", ", lotBinList)
                    : "N/A";

                items.Add(new SalesOrderItemDto
                {
                    ProductId = detail.ProductId,

                    ProductCode =
                        detail.Product?.ProductCode
                        ?? string.Empty,

                    ProductName =
                        detail.Product?.ProductName
                        ?? string.Empty,

                    // Số lượng khách đặt
                    Quantity = detail.OrderedQuantity,

                    // Tổng số lượng đã reserve
                    ReservedQuantity = detail.ReservedQuantity,

                    // Đơn vị tính
                    UnitName =
                        detail.Product?.UnitOfMeasure?.UnitName
                        ?? "Đơn vị",

                    // LOT · BIN
                    LotBinInfo = lotBinInfo,

                    // Giá bán
                    UnitPrice = detail.UnitPrice ?? 0
                });
            }

            return new SalesOrderDetailApiResponse
            {
                SalesOrderId =
                    salesOrder.SalesOrderId,

                SalesOrderNumber =
                    salesOrder.SalesOrderNumber,

                CustomerName =
                    salesOrder.Customer?.CustomerName
                    ?? "N/A",

                WarehouseId =
                    outboundOrder.WarehouseId,

                WarehouseName =
                    outboundOrder.Warehouse?.WarehouseName,

                Items = items
            };
        }
        public async Task<List<UserSelectDto>> GetSalesOrderCreatorsAsync(
       CancellationToken cancellationToken = default)
        {
            var users = await _salesOrderRepository
                .GetSalesOrderCreatorsAsync(cancellationToken);

            return users.Select(x => new UserSelectDto
            {
                UserId = x.UserId,
                FullName = x.FullName
            }).ToList();
        }
        public async Task<List<SalesOrderApiResponse>> GetConfirmedSalesOrdersAsync()
        {
            var salesOrders = await _salesOrderRepository.GetConfirmedSalesOrdersAsync();

            return salesOrders.Select(so => new SalesOrderApiResponse
            {
                SalesOrderId = so.SalesOrderId,
                SalesOrderNumber = so.SalesOrderNumber
            }).ToList();
        }
        public async Task<PagedResult<SalesOrderListDto>> GetPagedAsync(SalesOrderSearchCriteria criteria)
        {
            var (entities, totalCount) = await _salesOrderRepository.GetPagedAsync(
                criteria.Keyword,
                criteria.Status,
                criteria.PageIndex,
                criteria.PageSize
            );

            // Mapping từ Entity sang DTO
            var list = entities.Select(x => new SalesOrderListDto
            {
                SalesOrderId = x.SalesOrderId,
                SalesOrderNumber = x.SalesOrderNumber,
                CustomerCode = x.Customer?.CustomerCode ?? string.Empty,
                CustomerName = x.Customer?.CustomerName ?? string.Empty,
                OrderDate = x.OrderDate,
                ExpectedIssueDate = x.ExpectedIssueDate,
                Status = x.Status,
                TotalQuantity = x.SalesOrderDetails.Sum(d => d.OrderedQuantity),
                PrimaryUnitName = x.SalesOrderDetails.Select(d => d.Product?.UnitOfMeasure?.UnitName).FirstOrDefault() ?? ""
            }).ToList();

            return new PagedResult<SalesOrderListDto>
            {
                Items = list,
                TotalCount = totalCount,
                PageIndex = criteria.PageIndex,
                PageSize = criteria.PageSize
            };
        }

        public async Task<SalesOrderDetailDto?> GetByIdAsync(long salesOrderId)
        {
            var entity = await _salesOrderRepository.GetByIdAsync(salesOrderId);
            if (entity == null) return null;

            return new SalesOrderDetailDto
            {
                SalesOrderId = entity.SalesOrderId,
                SalesOrderNumber = entity.SalesOrderNumber,
                CustomerId = entity.CustomerId,
                CustomerCode = entity.Customer?.CustomerCode ?? string.Empty,
                CustomerName = entity.Customer?.CustomerName ?? string.Empty,
                OrderDate = entity.OrderDate,
                ExpectedIssueDate = entity.ExpectedIssueDate,
                Status = entity.Status,
                Notes = entity.Notes,
                CreatedByName = entity.CreatedByUser?.FullName ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                ConfirmedByName = entity.ConfirmedByUser?.FullName,
                ConfirmedAt = entity.ConfirmedAt,
                Items = entity.SalesOrderDetails.Select(d => new SalesOrderItemDtos
                {
                    SalesOrderDetailId = d.SalesOrderDetailId,
                    ProductId = d.ProductId,
                    ProductCode = d.Product?.ProductCode ?? string.Empty,
                    ProductName = d.Product?.ProductName ?? string.Empty,
                    UnitName = d.Product?.UnitOfMeasure?.UnitName ?? string.Empty,
                    OrderedQuantity = d.OrderedQuantity,
                    ReservedQuantity = d.ReservedQuantity,
                    FulfilledQuantity = d.FulfilledQuantity,
                    AvailableQuantity = 1000, // Logic: Cần join Inventory để lấy OnHand - Reserved thực tế
                    UnitPrice = d.UnitPrice,
                    Notes = d.Notes
                }).ToList()
            };
        }

        public async Task<SalesOrderDetailDto> CreateDraftAsync(CreateUpdateSalesOrderDto dto)
        {
            string newSoNumber = await _salesOrderRepository.GenerateSalesOrderNumberAsync();

            var order = new SalesOrder
            {
                SalesOrderNumber = newSoNumber,
                CustomerId = dto.CustomerId,
                OrderDate = dto.OrderDate,
                ExpectedIssueDate = dto.ExpectedIssueDate,
                Status = "DRAFT",
                Notes = dto.Notes,
                CreatedByUserId = dto.CurrentUserId,
                CreatedAt = DateTime.Now,
                SalesOrderDetails = dto.Items.Select(i => new SalesOrderDetail
                {
                    ProductId = i.ProductId,
                    OrderedQuantity = i.OrderedQuantity,
                    ReservedQuantity = 0,
                    FulfilledQuantity = 0,
                    UnitPrice = i.UnitPrice,
                    Notes = i.Notes
                }).ToList()
            };

            var created = await _salesOrderRepository.CreateAsync(order);
            return (await GetByIdAsync(created.SalesOrderId))!;
        }

        public async Task<bool> UpdateDraftAsync(CreateUpdateSalesOrderDto dto)
        {
            if (!dto.SalesOrderId.HasValue) return false;

            var existing = await _salesOrderRepository.GetByIdAsync(dto.SalesOrderId.Value);
            if (existing == null || existing.Status != "DRAFT")
            {
                return false; // Chỉ cho sửa đơn đang ở trạng thái Nháp
            }

            existing.CustomerId = dto.CustomerId;
            existing.OrderDate = dto.OrderDate;
            existing.ExpectedIssueDate = dto.ExpectedIssueDate;
            existing.Notes = dto.Notes;
            existing.SalesOrderDetails = dto.Items.Select(i => new SalesOrderDetail
            {
                SalesOrderId = existing.SalesOrderId,
                ProductId = i.ProductId,
                OrderedQuantity = i.OrderedQuantity,
                ReservedQuantity = 0,
                FulfilledQuantity = 0,
                UnitPrice = i.UnitPrice,
                Notes = i.Notes
            }).ToList();

            return await _salesOrderRepository.UpdateAsync(existing);
        }

        public async Task<(bool IsSuccess, string Message)> ConfirmAndReserveStockAsync(
          long salesOrderId,
          long confirmedByUserId)
        {
            var order = await _salesOrderRepository.GetByIdAsync(salesOrderId);

            if (order == null)
                return (false, "Không tìm thấy đơn bán hàng!");

            if (order.Status != "DRAFT")
                return (false, "Đơn hàng phải ở trạng thái Nháp mới có thể xác nhận!");

            // 1. Kiểm tra tồn kho khả dụng cho TẤT CẢ sản phẩm trước
            foreach (var detail in order.SalesOrderDetails)
            {
                decimal available =
                    await _invenRepository.GetAvailableQuantityAsync(
                        detail.ProductId);

                if (available < detail.OrderedQuantity)
                {
                    return (
                        false,
                        $"Sản phẩm {detail.Product?.ProductName ?? $"ID {detail.ProductId}"} " +
                        $"không đủ tồn kho khả dụng! " +
                        $"(Cần: {detail.OrderedQuantity}, Có: {available})"
                    );
                }
            }

            // 2. Sau khi tất cả sản phẩm đều đủ tồn
            // mới tiến hành giữ tồn
            foreach (var detail in order.SalesOrderDetails)
            {
                bool reserved =
                    await _invenRepository.ReserveStockAsync(
                        detail.ProductId,
                        detail.OrderedQuantity);

                if (!reserved)
                {
                    return (
                        false,
                        $"Không thể giữ tồn cho sản phẩm " +
                        $"{detail.Product?.ProductName ?? $"ID {detail.ProductId}"}!"
                    );
                }

                detail.ReservedQuantity = detail.OrderedQuantity;
            }

            // 3. Cập nhật trạng thái đơn hàng
            bool updateSuccess =
                await _salesOrderRepository.UpdateStatusAsync(
                    salesOrderId,
                    "ALLOCATED",
                    confirmedByUserId);

            if (!updateSuccess)
                return (false, "Cập nhật trạng thái thất bại!");

            return (
                true,
                "Đã kiểm tra tồn kho và xác nhận giữ tồn thành công!"
            );
        }


        public async Task<(bool IsSuccess, string Message)> CancelOrderAsync(long salesOrderId, long userId, string reason)
        {
            var order = await _salesOrderRepository.GetByIdAsync(salesOrderId);
            if (order == null) return (false, "Không tìm thấy đơn bán hàng!");

            if (order.Status == "FULFILLED")
                return (false, "Không thể hủy đơn bán hàng đã hoàn tất!");

            // Nếu đơn đã giữ tồn kho thì giải phóng lượng ReservedQuantity
            if (order.Status == "ALLOCATED")
            {
                foreach (var detail in order.SalesOrderDetails)
                {
                    detail.ReservedQuantity = 0;
                }
            }

            bool result = await _salesOrderRepository.UpdateStatusAsync(salesOrderId, "CANCELLED");
            return result ? (true, "Hủy đơn bán hàng thành công!") : (false, "Thao tác thất bại!");
        }
    }
}



