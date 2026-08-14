using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using BMWMS.Business.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Services.Inventory
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IOutboundOrderService _outboundOrderService;

        public SalesOrderService(
            ISalesOrderRepository salesOrderRepository,
            IOutboundOrderService outboundOrderService)
        {
            _salesOrderRepository = salesOrderRepository;
            _outboundOrderService = outboundOrderService;
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
                // Láº¥y danh sÃ¡ch Lot + Bin Ä‘Ã£ reserve
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

                    // Sá»‘ lÆ°á»£ng khÃ¡ch Ä‘áº·t
                    Quantity = detail.OrderedQuantity,

                    // Tá»•ng sá»‘ lÆ°á»£ng Ä‘Ã£ reserve
                    ReservedQuantity = detail.ReservedQuantity,

                    // ÄÆ¡n vá»‹ tÃ­nh
                    UnitName =
                        detail.Product?.UnitOfMeasure?.UnitName
                        ?? "ÄÆ¡n vá»‹",

                    // LOT Â· BIN
                    LotBinInfo = lotBinInfo,

                    // GiÃ¡ bÃ¡n
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

        public async Task<BMWMS.Business.Common.PagedResultDto<SalesOrderListDto>> GetPagedOrdersAsync(SalesOrderFilterDto filter)
        {
            var (items, totalCount) = await _salesOrderRepository.GetPagedListAsync(
                filter.SearchTerm,
                filter.Status,
                filter.WarehouseId,
                filter.PageIndex,
                filter.PageSize
            );

            var listDtos = items.Select(so =>
            {
                var firstDetail = so.SalesOrderDetails.FirstOrDefault();
                string unitName = firstDetail?.Product?.UnitOfMeasure?.UnitName ?? string.Empty;

                return new SalesOrderListDto
                {
                    SalesOrderId = so.SalesOrderId,
                    SalesOrderNumber = so.SalesOrderNumber,
                    CustomerId = so.CustomerId,
                    CustomerCode = so.Customer?.CustomerCode ?? string.Empty,
                    CustomerName = so.Customer?.CustomerName ?? string.Empty,
                    OrderDate = so.OrderDate,
                    ExpectedIssueDate = so.ExpectedIssueDate,
                    Status = so.Status,
                    TotalQuantity = so.SalesOrderDetails.Sum(d => d.OrderedQuantity),
                    UnitName = unitName
                };
            }).ToList();

            return new BMWMS.Business.Common.PagedResultDto<SalesOrderListDto>
            {
                Items = listDtos,
                TotalCount = totalCount,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize
            };
        }

        public async Task<SalesOrderDetailDto?> GetOrderDetailAsync(long salesOrderId)
        {
            var so = await _salesOrderRepository.GetByIdWithDetailsAsync(salesOrderId);
            if (so == null) return null;

            var outboundOrder = so.OutboundOrders.FirstOrDefault();

            return new SalesOrderDetailDto
            {
                SalesOrderId = so.SalesOrderId,
                SalesOrderNumber = so.SalesOrderNumber,
                CustomerId = so.CustomerId,
                CustomerCode = so.Customer?.CustomerCode ?? string.Empty,
                CustomerName = so.Customer?.CustomerName ?? string.Empty,
                CustomerPhone = so.Customer?.PhoneNumber ?? string.Empty,
                CustomerAddress = so.Customer?.Address ?? string.Empty,
                OrderDate = so.OrderDate,
                ExpectedIssueDate = so.ExpectedIssueDate,
                WarehouseId = outboundOrder?.WarehouseId,
                WarehouseName = outboundOrder?.Warehouse?.WarehouseName,
                Status = so.Status,
                Notes = so.Notes,
                CreatedByUserId = so.CreatedByUserId,
                CreatedByUserName = so.CreatedByUser?.FullName ?? so.CreatedByUser?.Username ?? string.Empty,
                CreatedAt = so.CreatedAt,
                ConfirmedByUserId = so.ConfirmedByUserId,
                ConfirmedByUserName = so.ConfirmedByUser?.FullName,
                ConfirmedAt = so.ConfirmedAt,
                Items = so.SalesOrderDetails.Select(d => new SalesOrderItemDetailDto
                {
                    SalesOrderDetailId = d.SalesOrderDetailId,
                    ProductId = d.ProductId,
                    ProductCode = d.Product?.ProductCode ?? string.Empty,
                    ProductName = d.Product?.ProductName ?? string.Empty,
                    Unit = d.Product?.UnitOfMeasure?.UnitName ?? string.Empty,
                    OrderedQuantity = d.OrderedQuantity,
                    Notes = d.Notes
                }).ToList()
            };
        }
    }
}
