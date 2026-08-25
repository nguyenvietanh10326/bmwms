using System.Threading.Tasks;
using BMWMS.Business.DTOs.Inbound;

namespace BMWMS.Business.Interfaces;

public interface IInboundService
{
    Task<InboundOrderPageDto> GetInboundOrdersPageAsync(InboundOrderFilterDto filter);
    Task<InboundOrderDetailDto?> GetInboundOrderByIdAsync(long id);
    Task<long> CreateInboundOrderAsync(CreateInboundOrderDto dto, long currentUserId);
    Task<PurchaseOrderForInboundDto?> GetPurchaseOrderForInboundAsync(long purchaseOrderId);
    Task<List<SourceOrderDropdownDto>> GetPendingPurchaseOrdersAsync();
    Task<List<AvailableWarehouseStaffDto>> GetAvailableWarehouseStaffAsync();
    Task<List<SourceOrderDropdownDto>> GetReturnableSalesOrdersAsync();
    Task<PurchaseOrderForInboundDto?> GetSalesOrderForInboundAsync(long salesOrderId);
    Task UpdateInboundOrderAsync(long id, UpdateInboundOrderDto dto, long currentUserId);
    Task CancelInboundOrderAsync(long id, CancelInboundOrderDto dto, long currentUserId);
    Task ConfirmInboundOrderAsync(long id, long currentUserId);
    Task<long> ReceiveItemAsync(long inboundOrderId, ReceiveInboundItemDto dto, long currentUserId);
    Task PutawayBatchAsync(long inboundOrderId, List<PutawayInboundItemDto> dtos, long currentUserId);
    Task ReceiveBatchAsync(long inboundOrderId, ReceiveBatchInboundDto dto, long currentUserId);
    Task CompleteReceiptAsync(long inboundOrderId, CompleteInboundReceiptDto dto, long currentUserId);
    Task<List<PutawayLocationDto>> GetPutawayLocationsAsync(long warehouseId, long productId);
}
