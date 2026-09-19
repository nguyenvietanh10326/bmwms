using BMWMS.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(long productId, bool includeDetails = false);
        Task<Product?> GetByCodeAsync(string productCode);
        Task<(List<Product> Items, int TotalCount)> GetPagedListAsync(
            string? keyword,
            long? productGroupId,
            int? unitOfMeasureId,
            string? status,
            string? rotationMethod,
            int pageIndex,
            int pageSize);

        Task<List<UnitsOfMeasure>> GetUnitsOfMeasureAsync();
        Task<List<ProductGroup>> GetProductGroupsAsync();

        Task<bool> IsCodeExistsAsync(string productCode, long? excludeProductId = null);
        Task<bool> IsBarcodeExistsAsync(string barcode, long? excludeProductId = null);
        Task<bool> HasTransactionsOrInventoryAsync(long productId);

        Task<long> AddAsync(Product product, List<ProductAttributeValue>? attributeValues = null);
        Task UpdateAsync(Product product, List<ProductAttributeValue>? attributeValues = null);
        Task DeleteAsync(long productId);

        Task<List<ProductAttributeValue>> GetProductAttributeValuesAsync(long productId);
        Task<List<ProductAttribute>> GetProductAttributesByIdsAsync(IEnumerable<long> productAttributeIds);

        /// <summary>Tự động insert ProductWarehousePolicy mặc định (MinStock=0, ExpiryWarning=30)
        /// cho tất cả kho đang ACTIVE nếu chưa có policy cho productId này.</summary>
        Task SeedDefaultWarehousePoliciesAsync(long productId,
            decimal defaultMinimumStockQuantity = 0m,
            int defaultExpiryWarningDays = 30);
    }
}
