using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces;

public interface IReportRepository
{
    Task<(List<VwInventoryAvailability> Items, int TotalCount, decimal TotalOnHand, decimal TotalReserved, decimal TotalAvailable)> 
        GetInventoryReportAsync(string? keyword, string? locationCode, string? lotNumber, bool positiveStockOnly, int pageIndex, int pageSize);
    Task<(int TotalCount, decimal TotalExpected, decimal TotalReceived, decimal TotalDamaged, decimal TotalShortage, List<VwInboundReport> Items)> GetInboundReportAsync(
        DateTime? fromDate, DateTime? toDate, string? productSearch, string? status, int pageNumber, int pageSize);

    Task<(int TotalCount, decimal TotalRequested, decimal TotalIssued, List<VwOutboundReport> Items)> GetOutboundReportAsync(
        DateTime? fromDate, DateTime? toDate, string? productSearch, string? status, int pageNumber, int pageSize);

    Task<(int TotalCount, List<(string ProductCode, string ProductName, decimal OpeningBalance, decimal InboundQuantity, decimal OutboundQuantity, decimal AdjustmentQuantity, decimal ClosingBalance)> Items)> GetInOutStockReportAsync(
        DateTime? fromDate, DateTime? toDate, string? productSearch, int pageNumber, int pageSize);
}
