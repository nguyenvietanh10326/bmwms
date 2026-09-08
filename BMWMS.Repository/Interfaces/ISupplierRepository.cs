using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface ISupplierRepository
{
    Task<(List<Supplier> Items, int TotalCount)> GetPagedListAsync(string? keyword, string? status, int pageIndex, int pageSize);
    Task<Supplier?> GetSupplierByCodeAsync(string supplierCode);
    Task<decimal> GetSupplierYtdInboundValueAsync(long supplierId, int year);
    Task<bool> CheckSupplierCodeExistsAsync(string code);
    Task<bool> CheckTaxCodeExistsAsync(string taxCode);
    Task AddAsync(Supplier supplier);
    Task<bool> HasPurchaseReferencesAsync(long supplierId);
    Task UpdateAsync(Supplier supplier);
    Task<(IEnumerable<SupplierProduct> Items, int TotalCount)> GetSupplierProductsAsync(long supplierId, string? search, string? status, int page, int pageSize);
    Task<(IEnumerable<InboundOrder> Items, int TotalCount)> GetSupplierInboundHistoryAsync(long supplierId, string? keyword, string? status, long? warehouseId, DateTime? fromDate, DateTime? toDate, int pageIndex, int pageSize);
    Task<InboundOrder?> GetSupplierInboundHistoryDetailAsync(long supplierId, string inboundOrderNumber);
    Task AssignProductsAsync(long supplierId, List<long> productIds);
}
