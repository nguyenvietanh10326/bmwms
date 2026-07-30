using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface ISupplierRepository
{
    Task<(List<Supplier> Items, int TotalCount)> GetPagedListAsync(string? keyword, string? status, int pageIndex, int pageSize);
}
