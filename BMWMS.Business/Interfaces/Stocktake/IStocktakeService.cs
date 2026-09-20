using BMWMS.Business.DTOs.Stocktake;

namespace BMWMS.Business.Interfaces.Stocktake
{
    public interface IStocktakeService
    {
        Task<List<StocktakeLocationOptionDto>> GetLocationOptionsAsync(long warehouseId, List<long>? rackIds = null, List<long>? productGroupIds = null);
        Task<List<StocktakeStaffOptionDto>> GetStaffUsersAsync();

        Task<StocktakeSessionPagedResultDto> GetSessionsAsync(StocktakeFilterDto filter, long currentUserId, bool canManage);
        Task<StocktakeSessionDetailDto?> GetSessionDetailAsync(long stocktakeSessionId, long currentUserId, bool canManage);
        Task<StocktakeCountTaskDto?> GetCountTaskAsync(long stocktakeSessionId, long storageLocationId, long currentUserId, bool canManage);

        Task<StocktakeActionResultDto> CreateSessionAsync(CreateStocktakeSessionDto dto, long createdByUserId);
        Task<StocktakeActionResultDto> StartSessionAsync(long stocktakeSessionId, long startedByUserId);
        Task<StocktakeActionResultDto> CancelSessionAsync(long stocktakeSessionId, long cancelledByUserId, string? notes);
        Task<StocktakeActionResultDto> SaveCountsAsync(long stocktakeSessionId, long storageLocationId, List<StocktakeCountLineDto> lines, long countedByUserId, bool canManage, string? notes = null);
        Task<StocktakeActionResultDto> SaveSessionCountsAsync(long stocktakeSessionId, SaveStocktakeSessionCountsDto request, long countedByUserId);
        Task<StocktakeActionResultDto> AddUnbookedItemAsync(long stocktakeSessionId, AddUnbookedStocktakeItemDto dto, long currentUserId);
        Task<StocktakeActionResultDto> RemoveUnbookedItemAsync(long stocktakeSessionId, long stocktakeItemId, long currentUserId);
        Task<StocktakeActionResultDto> SetTargetLocationAsync(long stocktakeSessionId, long stocktakeItemId, long? targetStorageLocationId, long currentUserId);
        Task<List<CompatibleLocationDto>> GetCompatibleLocationsAsync(long stocktakeSessionId, long productId, decimal quantity);
        Task<StocktakeActionResultDto> SubmitSessionAsync(long stocktakeSessionId, long submittedByUserId);
        Task<StocktakeActionResultDto> ApproveSessionAsync(long stocktakeSessionId, long approvedByUserId, StocktakeNoteDto request);
        Task<StocktakeActionResultDto> RejectSessionAsync(long stocktakeSessionId, long rejectedByUserId, string? reason);
    }
}
