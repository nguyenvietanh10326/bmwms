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
        Task<bool> UpdateStatusAsync(long outboundOrderId, string newStatus);
        Task<bool> CancelOutboundOrderAsync(long outboundOrderId);
    }
}
