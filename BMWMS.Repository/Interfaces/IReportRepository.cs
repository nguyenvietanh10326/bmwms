using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface IReportRepository
{
    Task<(List<VwInventoryAvailability> Items, int TotalCount, decimal TotalOnHand, decimal TotalReserved, decimal TotalAvailable)> 
        GetInventoryReportAsync(string? keyword, string? locationCode, string? lotNumber, bool positiveStockOnly, int pageIndex, int pageSize);
    Task<(List<VwInboundReport> Items, decimal TotalExpected, decimal TotalReceived, decimal TotalDamaged, decimal TotalShortage)> 
        GetInboundReportAsync(DateTime? fromDate, DateTime? toDate, string? productSearch, string? status);
}
