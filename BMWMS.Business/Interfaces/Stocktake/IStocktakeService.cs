using BMWMS.Business.DTOs.Stocktake;

namespace BMWMS.Business.Interfaces.Stocktake
{
    public interface IStocktakeService
    {
        Task<List<StocktakeLocationOptionDto>> GetLocationOptionsAsync(long warehouseId, List<long>? rackIds = null, List<long>? productGroupIds = null);
        Task<List<StocktakeStaffOptionDto>> GetStaffUsersAsync();
        Task<List<StocktakeProductLotOptionDto>> SearchProductLotsAsync(string? keyword, int take = 20);

        Task<StocktakeSessionPagedResultDto> GetSessionsAsync(StocktakeFilterDto filter, long currentUserId, bool canManage);
        Task<StocktakeSessionDetailDto?> GetSessionDetailAsync(long stocktakeSessionId, long currentUserId, bool canManage);
        Task<StocktakeCountTaskDto?> GetCountTaskAsync(long stocktakeSessionId, long storageLocationId, long currentUserId, bool canManage);

        Task<StocktakeActionResultDto> CreateSessionAsync(CreateStocktakeSessionDto dto, long createdByUserId);
        Task<StocktakeActionResultDto> StartSessionAsync(long stocktakeSessionId, long startedByUserId);
        Task<StocktakeActionResultDto> CancelSessionAsync(long stocktakeSessionId, long cancelledByUserId, string? notes);
        Task<StocktakeActionResultDto> SaveCountsAsync(long stocktakeSessionId, long storageLocationId, List<StocktakeCountLineDto> lines, long countedByUserId, bool canManage, string? notes = null);
        Task<StocktakeActionResultDto> SubmitLocationAsync(long stocktakeSessionId, long storageLocationId, long submittedByUserId, bool canManage, string? notes);
        Task<StocktakeActionResultDto> AddUnexpectedItemAsync(long stocktakeSessionId, UnexpectedStocktakeItemDto dto, long countedByUserId, bool canManage);
        Task<StocktakeActionResultDto> ApplyResolutionsAsync(long stocktakeSessionId, List<StocktakeResolutionDto> resolutions, long reviewedByUserId);
        Task<StocktakeActionResultDto> ApproveSessionAsync(long stocktakeSessionId, long approvedByUserId, StocktakeNoteDto request);
    }
}
