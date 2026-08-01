using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces.Inventory
{
    public interface IDashboardRepository
    {
        Task<int> GetTotalProductsCountAsync();
        Task<int> GetActiveWarehousesCountAsync();

        Task<List<BMWMS.Repository.Models.Inventory>> GetLowStockInventoriesAsync(int top = 5);

        Task<List<BMWMS.Repository.Models.InventoryTransaction>> GetRecentActivitiesAsync(int top = 5);

        Task<int> GetPendingPurchaseOrdersCountAsync();
        Task<int> GetPendingSalesOrdersCountAsync();
        Task<int> GetProcessingInboundOrdersCountAsync();
        Task<int> GetPickingOutboundOrdersCountAsync();
    }
}
