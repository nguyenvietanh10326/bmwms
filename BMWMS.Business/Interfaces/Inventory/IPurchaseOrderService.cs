using BMWMS.Business.DTOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Interfaces.Inventory
{
    public interface IPurchaseOrderService
    {
        // 1. Lấy danh sách PO phân trang
        Task<PagedResultDto<PurchaseOrderListDto>> GetPagedOrdersAsync(PurchaseOrderFilterDto filter);

        // 2. Lấy chi tiết 1 PO
        Task<PurchaseOrderDetailDto?> GetOrderDetailAsync(long purchaseOrderId);

        // 3. Tạo mới PO (Có thể là Nháp hoặc Xác nhận ngay)
        Task<(bool Success, string Message, long OrderId)> CreateOrderAsync(CreatePurchaseOrderDto dto, long currentUserId);

        // 4. Xác nhận đơn mua hàng (Draft -> Confirmed)
        Task<(bool Success, string Message)> ConfirmOrderAsync(long purchaseOrderId, long currentUserId);

        // 5. Hủy đơn mua hàng
        Task<(bool Success, string Message)> CancelOrderAsync(long purchaseOrderId, long currentUserId, string? reason);

        Task<IEnumerable<SupplierLookupDto>> GetLookupListAsync();
        Task<IEnumerable<WarehouseLookupDto>> GetLookListAsync();

        Task<IEnumerable<ProductLookupDto>> GetUpListAsync();
        Task<IEnumerable<CustomerLookupDto>> GetCustomersAsync();
    }
}
