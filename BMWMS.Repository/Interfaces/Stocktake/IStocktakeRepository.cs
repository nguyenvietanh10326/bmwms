using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces.Stocktake
{
    public class StocktakeCountUpdateParam
    {
        public long StocktakeItemId { get; set; }
        public decimal? CountedQuantity { get; set; }
        public string? Notes { get; set; }
    }

    public class AddUnbookedStocktakeItemParam
    {
        public long StorageLocationId { get; set; }
        public long? TargetStorageLocationId { get; set; }
        public long ProductId { get; set; }
        public decimal CountedQuantity { get; set; }
        public DateOnly? FirstReceivedDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string? Notes { get; set; }
    }

    public interface IStocktakeRepository
    {
        Task<List<Warehouse>> GetActiveWarehousesAsync();
        Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId, List<long>? rackIds = null, List<long>? productGroupIds = null);
        Task<List<User>> GetAssignableUsersAsync();

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
        Task<StocktakeSession> SaveSessionCountsAsync(long stocktakeSessionId, List<StocktakeCountUpdateParam> lines, List<long> confirmedEmptyLocationIds, long countedByUserId);
        Task<StocktakeItem> AddUnbookedItemAsync(long stocktakeSessionId, AddUnbookedStocktakeItemParam param, long countedByUserId);
        Task<bool> RemoveUnbookedItemAsync(long stocktakeSessionId, long stocktakeItemId, long currentUserId);
        Task<StocktakeItem> SetTargetLocationAsync(long stocktakeSessionId, long stocktakeItemId, long? targetStorageLocationId, long currentUserId);
        Task<List<StorageLocation>> GetCompatibleLocationsAsync(long warehouseId, long productId, decimal quantity);
        Task<StocktakeSession> SubmitSessionAsync(long stocktakeSessionId, long submittedByUserId);
        Task<StocktakeSession> ApproveSessionAsync(long stocktakeSessionId, long approvedByUserId, string? notes);
        Task<StocktakeSession> RejectSessionAsync(long stocktakeSessionId, long rejectedByUserId, string reason);
    }
}
