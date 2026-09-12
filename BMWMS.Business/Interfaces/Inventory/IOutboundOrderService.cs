using BMWMS.Business.DTOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Interfaces.Inventory
{
    public interface IOutboundOrderService
    {
        Task<PagedResultDto<OutboundOrderListDto>> GetOutboundOrdersAsync(OutboundOrderQueryFilter filter);
        Task<OutboundOrderDetailDto?> GetOutboundOrderByIdAsync(long id);
        Task<OutboundOrderDetailDto> CreateOutboundOrderAsync(CreateOutboundOrderRequest request, long createdByUserId);
        Task<(bool Success, string Message)> UpdateDraftAsync(long outboundOrderId, UpdateOutboundOrderRequest request, long userId);
        Task<(bool Success, string Message)> ApproveAndAssignAsync(long outboundOrderId, long assignedToUserId, long approvedByUserId);
        Task<(bool Success, string Message)> StartProcessingAsync(long outboundOrderId, long userId);
        Task<(bool Success, string Message)> CancelOutboundOrderAsync(long outboundOrderId, long userId, string reason);
        Task<(bool Success, string Message)> ExecutePickBatchAsync(IReadOnlyCollection<ExecutePickItemRequest> requests, long userId);
        Task<(bool Success, string Message)> CompleteOutboundAsync(long outboundOrderId, long userId, string reason);
        Task<(bool Success, string Message)> CloseRemainingSalesDemandAsync(long outboundOrderId, long userId, string reason);
        Task<OutboundProcessViewDto?> GetOutboundProcessDetailAsync(long outboundOrderId);
        Task<List<PurchaseOrderReturnOptionDto>> GetReturnablePurchaseOrdersAsync();
        Task<PurchaseOrderForReturnDto?> GetPurchaseOrderForReturnAsync(long purchaseOrderId);
        Task<List<UserSelectDto>> GetWarehouseStaffAsync();

    }
}
