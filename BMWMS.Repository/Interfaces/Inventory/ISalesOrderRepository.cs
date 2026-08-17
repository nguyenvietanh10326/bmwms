using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces.Inventory
{
    public interface ISalesOrderRepository {
        Task<OutboundOrder?> GetOutboundOrderDetailAsync(long outboundOrderId);
        Task<decimal> GetReservedQuantityAsync(long salesOrderItemId); 
        Task<List<string>> GetReservedLotBinInfoAsync(long salesOrderItemId); 
    Task<List<User>> GetSalesOrderCreatorsAsync(
        CancellationToken cancellationToken = default);
        Task<List<SalesOrder>> GetConfirmedSalesOrdersAsync();
        Task<Customer?> GetActiveCustomerAsync(long customerId);
        Task<List<Product>> GetActiveProductsAsync(IEnumerable<long> productIds);
        Task<List<Product>> GetActiveProductsForLookupAsync();

        // 1. Lấy danh sách phân trang & lọc theo Entity thuần
        Task<(IEnumerable<SalesOrder> Items, int TotalCount)> GetPagedAsync(
            string? keyword,
            string? status,
            DateOnly? fromDate,
            DateOnly? toDate,
            int pageIndex,
            int pageSize);

        // 2. Lấy đơn hàng theo ID (kèm Includes cần thiết)
        Task<SalesOrder?> GetByIdAsync(long salesOrderId);

        // 3. Lấy theo Mã SO
        Task<SalesOrder?> GetByNumberAsync(string salesOrderNumber);

        // 4. Tạo mới Đơn bán hàng (bao gồm Details)
        Task<SalesOrder> CreateAsync(SalesOrder salesOrder);

        // 5. Cập nhật thông tin Đơn bán hàng
        Task<bool> UpdateAsync(SalesOrder salesOrder);

        // 6. Cập nhật trạng thái
        Task<bool> UpdateStatusAsync(long salesOrderId, string status, long? confirmedByUserId = null);

        // 7. Tạo mã SO tự động (Ví dụ: SO-2026-0042)
        Task<string> GenerateSalesOrderNumberAsync();
    }
}
