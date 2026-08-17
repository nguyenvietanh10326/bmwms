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


        Task SavePickDetailAsync(OutboundOrderDetail detail, OutboundOrderItem item, OutboundOrder order);


        Task<List<AvailableLocationModel>> GetAvailableLocationsAsync(long warehouseId, long productId);

        public class AvailableLocationModel
        {
            public long StorageLocationId { get; set; }
            public string LocationCode { get; set; } = null!;
            public long ProductLotId { get; set; }
            public string LotNumber { get; set; } = null!;
            public long? InventoryReservationId { get; set; }
            public decimal AvailableQuantity { get; set; }
        }
    }
}
