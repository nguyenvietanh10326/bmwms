using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces.Inventory
{
    public interface IOutboundOrderRepository
    {
        Task<IEnumerable<OutboundOrder>> GetAllAsync(string? search, string? status, long? warehouseId);
        Task<OutboundOrder?> GetByIdAsync(long id);
        Task<OutboundOrder?> GetByOrderNumberAsync(string orderNumber);
        Task<OutboundOrder> CreateAsync(OutboundOrder outboundOrder);
        Task<bool> UpdateStatusAsync(long outboundOrderId, string status);
        Task<bool> ExistsAsync(long id);
    }
}
