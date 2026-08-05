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

    Task<(int TotalCount, List<(long ProductId, string ProductCode, string ProductName, string BaseUnitCode, decimal CurrentStock, decimal InboundQuantity, decimal OutboundQuantity, decimal AdjustmentQuantity, int MovementFrequency, int DaysSinceLastMovement)> Items)> GetProductStatisticsAsync(
        DateTime? fromDate, DateTime? toDate, string? productSearch, string? productGroupCode, int pageNumber, int pageSize);

    Task<(int TotalCount, List<(long SupplierId, string SupplierCode, string SupplierName, int InboundOrderCount, decimal ExpectedQuantity, decimal ReceivedQuantity, decimal DamagedQuantity, decimal ShortageQuantity)> Items)> GetSupplierStatisticsAsync(
        DateTime? fromDate, DateTime? toDate, string? supplierSearch, int pageNumber, int pageSize);

    Task<(int TotalCount, List<(long StocktakeSessionId, string StocktakeNumber, DateOnly PlannedDate, string Status, int BinsCounted, int MatchedItems, int ShortageItems, int ExcessItems, int TotalItemsCounted, decimal TotalShortageQuantity, decimal TotalExcessQuantity, decimal TotalApprovedAdjustmentQuantity)> Items)> GetStocktakeStatisticsAsync(
        DateTime? fromDate, DateTime? toDate, string? countType, string? storageAreaCode, string? productGroupCode, string? sessionStatus, int pageNumber, int pageSize);
    Task<(int TotalCount, List<VwLowStockAlert> Items)> GetLowStockAlertsAsync(
        string? keyword, int pageNumber, int pageSize);

    Task<(int TotalCount, List<VwExpiringLotAlert> Items)> GetExpiringLotAlertsAsync(
        string? keyword, int? maxDaysToExpiry, int pageNumber, int pageSize);
}
