using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using BMWMS.Repository.Repositories.Inventory;
using BMWMS.Business.Common;
using Microsoft.EntityFrameworkCore;
using System.Data;
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
        private readonly BmwmsContext _context;

        public SalesOrderService(
            ISalesOrderRepository salesOrderRepository,
            IInventoryRepository invenRepository,
            BmwmsContext context)
        {
            _salesOrderRepository = salesOrderRepository;
            _invenRepository = invenRepository;
            _context = context;
        }

        public async Task<SalesOrderDetailApiResponse?>
            GetSalesOrderDetailForOutboundAsync(
                long salesOrderId)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(salesOrderId);
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

                    // Số lượng có thể lập phiếu xuất. Với SO cũ chưa có reservation,
                    // CreateOutboundOrder sẽ giữ bù tồn kho trong transaction trước khi tạo phiếu.
                    ReservedQuantity = Math.Max(0, detail.OrderedQuantity - detail.FulfilledQuantity),

                    // Đơn vị tính
                    UnitName =
                        detail.Product?.UnitOfMeasure?.UnitName
                        ?? "Đơn vị",
                    QuantityScale = detail.Product?.UnitOfMeasure?.QuantityScale ?? 0,
                    TrackLot = detail.Product?.TrackLot ?? false,

                    // LOT · BIN
                    LotBinInfo = lotBinInfo
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

                WarehouseId = null,
                WarehouseName = null,

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
            criteria.PageIndex = Math.Max(1, criteria.PageIndex);
            criteria.PageSize = Math.Clamp(criteria.PageSize, 10, 100);

            var (entities, totalCount) = await _salesOrderRepository.GetPagedAsync(
                criteria.Keyword,
                criteria.Status,
                criteria.FromDate,
                criteria.ToDate,
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
                Status = NormalizeSalesOrderStatus(x.Status),
                TotalQuantity = x.SalesOrderDetails.Sum(d => d.OrderedQuantity),
                PrimaryUnitName = x.SalesOrderDetails.Select(d => d.Product?.UnitOfMeasure?.UnitName).FirstOrDefault() ?? "",
                ItemCount = x.SalesOrderDetails.Count,
                CreatedByName = x.CreatedByUser?.FullName ?? string.Empty,
                CreatedAt = x.CreatedAt
            }).ToList();

            return new PagedResult<SalesOrderListDto>
            {
                Items = list,
                TotalCount = totalCount,
                PageIndex = criteria.PageIndex,
                PageSize = criteria.PageSize
            };
        }

        private static string NormalizeSalesOrderStatus(string status)
        {
            return status switch
            {
                "ALLOCATED" => "CONFIRMED",
                "PARTIALLY_FULFILLED" => "PARTIALLY_ISSUED",
                "FULFILLED" or "CLOSED" => "ISSUED",
                _ => status
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
                    QuantityScale = d.Product?.UnitOfMeasure?.QuantityScale ?? 0,
                    TrackLot = d.Product?.TrackLot ?? false,
                    OrderedQuantity = d.OrderedQuantity,
                    ReservedQuantity = d.ReservedQuantity,
                    FulfilledQuantity = d.FulfilledQuantity,
                    AvailableQuantity = 1000, // Logic: Cần join Inventory để lấy OnHand - Reserved thực tế
                    Notes = d.Notes
                }).ToList()
            };
        }

        public async Task<List<SalesOrderProductLookupDto>> GetActiveProductLookupsAsync()
        {
            var products = await _salesOrderRepository.GetActiveProductsForLookupAsync();

            return products.Select(p => new SalesOrderProductLookupDto
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                ProductGroupId = p.ProductGroupId,
                ProductGroupName = p.ProductGroup?.GroupName ?? string.Empty,
                Barcode = p.Barcode,
                UnitOfMeasure = p.UnitOfMeasure?.UnitName ?? string.Empty,
                QuantityScale = p.UnitOfMeasure?.QuantityScale ?? 0,
                TrackLot = p.TrackLot,
                TrackExpiry = p.TrackExpiry,
                RotationMethod = p.RotationMethod,
                OnHandQuantity = p.Inventories.Sum(i => i.OnHandQuantity),
                ReservedQuantity = p.Inventories.Sum(i => i.ReservedQuantity),
                AvailableQuantity = p.Inventories.Sum(i => i.OnHandQuantity - i.ReservedQuantity)
            }).ToList();
        }

        public async Task<SalesOrderDetailDto> CreateDraftAsync(CreateUpdateSalesOrderDto dto)
        {
            if (dto.CustomerId <= 0)
                throw new ArgumentException("Vui lòng chọn khách hàng.");

            if (dto.OrderDate == default)
                throw new ArgumentException("Ngày đặt hàng không hợp lệ.");

            if (dto.ExpectedIssueDate.HasValue && dto.ExpectedIssueDate.Value < dto.OrderDate)
                throw new ArgumentException("Ngày xuất dự kiến không được trước ngày đặt hàng.");

            if (dto.Items == null || dto.Items.Count == 0)
                throw new ArgumentException("Đơn bán hàng phải có ít nhất một sản phẩm.");

            if (dto.Items.Any(i => i.ProductId <= 0 || i.OrderedQuantity <= 0))
                throw new ArgumentException("Mỗi sản phẩm phải có số lượng đặt lớn hơn 0.");

            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            if (productIds.Distinct().Count() != productIds.Count)
                throw new ArgumentException("Mỗi sản phẩm chỉ được xuất hiện một lần trong đơn bán hàng.");

            var customer = await _salesOrderRepository.GetActiveCustomerAsync(dto.CustomerId);
            if (customer == null)
                throw new ArgumentException("Khách hàng không tồn tại hoặc đã ngừng hoạt động.");

            var activeProducts = await _salesOrderRepository.GetActiveProductsAsync(productIds);
            if (activeProducts.Count != productIds.Count)
                throw new ArgumentException("Một hoặc nhiều sản phẩm không tồn tại hoặc đã ngừng hoạt động.");

            var productsById = activeProducts.ToDictionary(p => p.ProductId);
            foreach (var item in dto.Items)
                QuantityRules.EnsureValid(productsById[item.ProductId], item.OrderedQuantity, "Số lượng đặt");

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
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
                    AllocationStrategy = dto.AllocationStrategy,
                    CreatedByUserId = dto.CurrentUserId,
                    CreatedAt = DateTime.UtcNow,
                    SalesOrderDetails = dto.Items.Select(i => new SalesOrderDetail
                    {
                        ProductId = i.ProductId,
                        OrderedQuantity = i.OrderedQuantity,
                        ReservedQuantity = 0,
                        FulfilledQuantity = 0,
                        Notes = i.Notes
                    }).ToList()
                };

                var created = await _salesOrderRepository.CreateAsync(order);
                foreach (var detail in created.SalesOrderDetails)
                {
                    var product = productsById[detail.ProductId];
                    var reserved = await _invenRepository.ReserveStockForOrderAsync(
                        detail.ProductId, detail.OrderedQuantity, detail.SalesOrderDetailId,
                        dto.CurrentUserId, product.RotationMethod);
                    if (!reserved)
                        throw new InvalidOperationException($"Tồn khả dụng của {product.ProductCode} không đủ để giữ cho đơn nháp.");
                    detail.ReservedQuantity = detail.OrderedQuantity;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (await GetByIdAsync(created.SalesOrderId))!;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateDraftAsync(CreateUpdateSalesOrderDto dto)
        {
            if (!dto.SalesOrderId.HasValue || dto.Items == null || dto.Items.Count == 0) return false;
            if (dto.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
                throw new ArgumentException("Mỗi sản phẩm chỉ được xuất hiện một lần trong đơn bán hàng.");

            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var activeProducts = await _salesOrderRepository.GetActiveProductsAsync(productIds);
            if (activeProducts.Count != productIds.Distinct().Count())
                throw new ArgumentException("Một hoặc nhiều sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
            var productsById = activeProducts.ToDictionary(p => p.ProductId);
            foreach (var item in dto.Items)
                QuantityRules.EnsureValid(productsById[item.ProductId], item.OrderedQuantity, "Số lượng đặt");

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var existing = await _context.SalesOrders
                    .Include(s => s.SalesOrderDetails)
                    .FirstOrDefaultAsync(s => s.SalesOrderId == dto.SalesOrderId.Value);
                if (existing == null || existing.Status != "DRAFT") return false;

                var existingProductIds = existing.SalesOrderDetails.Select(d => d.ProductId).OrderBy(x => x).ToList();
                var requestedProductIds = dto.Items.Select(d => d.ProductId).OrderBy(x => x).ToList();
                if (!existingProductIds.SequenceEqual(requestedProductIds))
                    throw new InvalidOperationException("SO nháp đã giữ tồn nên không thể thêm hoặc xóa dòng vật tư. Hãy hủy đơn và tạo SO mới để giữ đúng lịch sử.");

                await ReleaseReservationsAsync(existing.SalesOrderId, dto.CurrentUserId);

                existing.CustomerId = dto.CustomerId;
                existing.OrderDate = dto.OrderDate;
                existing.ExpectedIssueDate = dto.ExpectedIssueDate;
                existing.Notes = dto.Notes;
                existing.AllocationStrategy = dto.AllocationStrategy ?? "FIFO";
                existing.UpdatedAt = DateTime.UtcNow;

                foreach (var detail in existing.SalesOrderDetails)
                {
                    var requested = dto.Items.Single(i => i.ProductId == detail.ProductId);
                    detail.OrderedQuantity = requested.OrderedQuantity;
                    detail.ReservedQuantity = 0;
                    detail.Notes = requested.Notes;
                }
                await _context.SaveChangesAsync();

                foreach (var detail in existing.SalesOrderDetails)
                {
                    var product = productsById[detail.ProductId];
                    if (!await _invenRepository.ReserveStockForOrderAsync(
                            detail.ProductId, detail.OrderedQuantity, detail.SalesOrderDetailId,
                            dto.CurrentUserId, product.RotationMethod))
                        throw new InvalidOperationException($"Tồn khả dụng của {product.ProductCode} không đủ để cập nhật đơn nháp.");
                    detail.ReservedQuantity = detail.OrderedQuantity;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(bool IsSuccess, string Message)> ConfirmAndReserveStockAsync(
          long salesOrderId,
          long confirmedByUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var order = await _salesOrderRepository.GetByIdAsync(salesOrderId);

            if (order == null)
                return (false, "Không tìm thấy đơn bán hàng!");

            if (order.Status != "DRAFT")
                return (false, "Đơn hàng phải ở trạng thái Nháp mới có thể xác nhận!");

            foreach (var detail in order.SalesOrderDetails)
            {
                var activeReserved = await _context.InventoryReservations
                    .Where(r => r.SalesOrderDetailId == detail.SalesOrderDetailId &&
                                (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"))
                    .SumAsync(r => (decimal?)(r.ReservedQuantity - r.ConsumedQuantity)) ?? 0;
                var need = detail.OrderedQuantity - activeReserved;
                if (need > 0 && !await _invenRepository.ReserveStockForOrderAsync(
                        detail.ProductId, need, detail.SalesOrderDetailId, confirmedByUserId,
                        detail.Product?.RotationMethod ?? "FIFO"))
                {
                    await transaction.RollbackAsync();
                    return (false, $"Tồn khả dụng của {detail.Product?.ProductCode ?? detail.ProductId.ToString()} không đủ để hoàn tất giữ tồn.");
                }
                detail.ReservedQuantity = detail.OrderedQuantity;
            }

            order.Status = "CONFIRMED";
            order.ConfirmedByUserId = confirmedByUserId;
            order.ConfirmedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (
                true,
                "Đã xác nhận đơn bán hàng; phần giữ tồn từ bản nháp tiếp tục có hiệu lực."
            );
        }


        public async Task<(bool IsSuccess, string Message)> CancelOrderAsync(long salesOrderId, long userId, string reason)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var order = await _salesOrderRepository.GetByIdAsync(salesOrderId);
            if (order == null) return (false, "Không tìm thấy đơn bán hàng!");

            if (order.Status is not ("DRAFT" or "CONFIRMED" or "ALLOCATED"))
                return (false, "Chỉ có thể hủy đơn bán hàng ở trạng thái Nháp hoặc Đã xác nhận!");

            await ReleaseReservationsAsync(salesOrderId, userId);
            order.Status = "CANCELLED";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, "Hủy đơn bán hàng và giải phóng giữ tồn thành công!");
        }

        private async Task ReleaseReservationsAsync(long salesOrderId, long userId)
        {
            var reservations = await _context.InventoryReservations
                .Include(r => r.SalesOrderDetail)
                .Where(r => r.SalesOrderDetail.SalesOrderId == salesOrderId &&
                            (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"))
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                var releasable = reservation.ReservedQuantity - reservation.ConsumedQuantity;
                if (releasable > 0)
                {
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "RELEASE_RESERVATION",
                        ProductId = reservation.ProductId,
                        StorageLocationId = reservation.StorageLocationId,
                        ProductLotId = reservation.ProductLotId,
                        OnHandDelta = 0,
                        ReservedDelta = -releasable,
                        InventoryReservationId = reservation.InventoryReservationId,
                        PerformedByUserId = userId,
                        TransactionAt = DateTime.UtcNow,
                        Notes = "Giải phóng giữ tồn của Sales Order nháp."
                    });
                }
                reservation.Status = "RELEASED";
                reservation.ReleasedAt = DateTime.UtcNow;
                reservation.SalesOrderDetail.ReservedQuantity = 0;
            }

            await _context.SaveChangesAsync();
        }
    }
}



