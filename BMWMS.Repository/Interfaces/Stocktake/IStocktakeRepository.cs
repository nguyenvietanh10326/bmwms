using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces.Stocktake
{
    public class StocktakeCountUpdateParam
    {
        public long StocktakeItemId { get; set; }
        public decimal? CountedQuantity { get; set; }
        public string? Notes { get; set; }
    }

    public class StocktakeResolutionParam
    {
        public long StocktakeItemId { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public interface IStocktakeRepository
    {
        Task<List<Warehouse>> GetActiveWarehousesAsync();
        Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId, List<long>? rackIds = null, List<long>? productGroupIds = null);
        Task<List<User>> GetAssignableUsersAsync();
        Task<List<ProductLot>> SearchProductLotsAsync(string? keyword, int take = 20);

        Task<(List<StocktakeSession> Items, int TotalCount, int ScheduledCount, int InProgressCount, int CountedCount, int PendingApprovalCount, int CompletedCount, int CancelledCount)>
            GetPagedSessionsAsync(
                string? keyword,
                string? status,
                long? warehouseId,
                long? assignedToUserId,
                DateOnly? fromDate,
                DateOnly? toDate,
                int pageIndex,
                int pageSize);

        Task<StocktakeSession?> GetSessionDetailAsync(long stocktakeSessionId);
        Task<StocktakeSession?> GetSessionForUpdateAsync(long stocktakeSessionId);
        Task<StocktakeSession> CreateSessionAsync(long warehouseId, DateOnly plannedDate, long createdByUserId, long? assignedToUserId, string? notes, List<long> storageLocationIds);
        Task<StocktakeSession> StartSessionAsync(long stocktakeSessionId, long startedByUserId);
        Task<StocktakeSession> CancelSessionAsync(long stocktakeSessionId, long cancelledByUserId, string? notes);
        Task<StocktakeLocation?> GetLocationCountTaskAsync(long stocktakeSessionId, long storageLocationId);
        Task<StocktakeSession> SaveCountsAsync(long stocktakeSessionId, long storageLocationId, List<StocktakeCountUpdateParam> lines, long countedByUserId, string? notes);
        Task<StocktakeSession> SubmitLocationAsync(long stocktakeSessionId, long storageLocationId, long submittedByUserId, string? notes);
        Task<StocktakeItem> AddUnexpectedItemAsync(long stocktakeSessionId, long storageLocationId, long productId, long productLotId, decimal countedQuantity, long countedByUserId, string? notes);
        Task<StocktakeSession> ApplyResolutionsAsync(long stocktakeSessionId, List<StocktakeResolutionParam> resolutions, long reviewedByUserId);
        Task<StocktakeSession> ApproveSessionAsync(long stocktakeSessionId, long approvedByUserId, string? notes);
    }
}
