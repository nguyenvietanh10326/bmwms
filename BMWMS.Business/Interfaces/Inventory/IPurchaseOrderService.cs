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
        // 2. Lấy chi tiết 1 PO
        Task<PurchaseOrderDetailDto?> GetOrderDetailAsync(long purchaseOrderId);

        // Tạo PO mới
        Task<(bool Success, string Message)> CreatePurchaseOrderAsync(PurchaseOrderCreateDto request, long userId);


        // Lấy danh sách PO phân trang
        Task<PagedResultDto<PurchaseOrderListDto>> GetPagedOrdersAsync(PurchaseOrderFilterDto filter);

        // Gửi nội dung PO; gửi thành công đồng thời xác nhận PO trên hệ thống.
        Task<(bool Success, string Message)> SendOrderToSupplierAsync(long purchaseOrderId, long currentUserId);

        // Hủy PO
        Task<(bool Success, string Message)> CancelOrderAsync(long purchaseOrderId, long currentUserId, string? reason);
        Task<(bool Success, string Message)> ClosePartiallyReceivedOrderAsync(long purchaseOrderId, long currentUserId, string reason);
        Task<(bool Success, string Message)> ContinuePartiallyReceivedOrderAsync(
            long purchaseOrderId,
            long currentUserId,
            DateOnly requestedDeliveryDate,
            string? note);

        Task<IEnumerable<SupplierLookupDto>> GetLookupListAsync();
        Task<IEnumerable<WarehouseLookupDto>> GetLookListAsync();
        Task<IEnumerable<ProductLookupDto>> GetUpListAsync();
        Task<IEnumerable<CustomerLookupDto>> GetCustomersAsync();
    }
}
