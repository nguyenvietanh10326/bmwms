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
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ISalesOrderRepository _salesOrderRepository;

        public SalesOrderService(
            ISalesOrderRepository salesOrderRepository)
        {
            _salesOrderRepository = salesOrderRepository;
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
    }
}


