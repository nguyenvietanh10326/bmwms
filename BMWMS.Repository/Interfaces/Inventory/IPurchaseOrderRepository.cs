using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces.Inventory
{
    public interface IPurchaseOrderRepository
    {
        // Lấy danh sách PO phân trang + bộ lọc
        Task<(IEnumerable<PurchaseOrder> Items, int TotalCount)> GetPagedListAsync(
            string? searchTerm,
            string? status,
            long? warehouseId,
            int pageIndex,
            int pageSize);

        // Lấy chi tiết 1 PO đầy đủ gồm Supplier, Details, Products, InboundOrders
        Task<PurchaseOrder?> GetByIdWithDetailsAsync(long purchaseOrderId);

        // Sinh mã PO tự động (Ví dụ: PO-2026-0079)
        Task<string> GeneratePurchaseOrderNumberAsync();

        // Các hàm CRUD cơ bản
        Task<PurchaseOrder> AddAsync(PurchaseOrder entity);
        Task UpdateAsync(PurchaseOrder entity);
        Task DeleteAsync(long purchaseOrderId);
        Task<bool> ExistsAsync(long purchaseOrderId);

        Task<IEnumerable<Supplier>> GetAllAsync();
        Task<IEnumerable<Warehouse>> GetAllWareAsync();
        Task<IEnumerable<Product>> GetAllProductAsync();
        Task<IEnumerable<Customer>> GetCustomersAsync();
    }
}
