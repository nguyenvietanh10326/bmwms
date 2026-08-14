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

        Task<IEnumerable<SupplierLookupDto>> GetLookupListAsync();
        Task<IEnumerable<WarehouseLookupDto>> GetLookListAsync();
        Task<IEnumerable<ProductLookupDto>> GetUpListAsync();
    }
}
