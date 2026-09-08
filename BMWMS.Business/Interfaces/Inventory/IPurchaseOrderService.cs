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

        // Xác nhận PO
        Task<(bool Success, string Message)> ConfirmOrderAsync(long purchaseOrderId, long currentUserId);

        // Gửi nội dung PO cho nhà cung cấp nhưng vẫn giữ trạng thái DRAFT
        Task<(bool Success, string Message)> SendOrderToSupplierAsync(long purchaseOrderId, long currentUserId);

        // Hủy PO
        Task<(bool Success, string Message)> CancelOrderAsync(long purchaseOrderId, long currentUserId, string? reason);
        Task<(bool Success, string Message)> ClosePartiallyReceivedOrderAsync(long purchaseOrderId, long currentUserId, string reason);

        Task<IEnumerable<SupplierLookupDto>> GetLookupListAsync();
        Task<IEnumerable<WarehouseLookupDto>> GetLookListAsync();
        Task<IEnumerable<ProductLookupDto>> GetUpListAsync();
        Task<IEnumerable<CustomerLookupDto>> GetCustomersAsync();
    }
}
