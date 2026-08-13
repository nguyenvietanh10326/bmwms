using BMWMS.Web.Models;

namespace BMWMS.Web.Services
{
    public interface IStocktakeApiService
    {
        Task<List<StocktakeLocationOptionModel>> GetLocationsAsync(long warehouseId, List<long>? rackIds = null, List<long>? productGroupIds = null);
        Task<List<StocktakeStaffOptionModel>> GetStaffUsersAsync();
        Task<List<StocktakeProductLotOptionModel>> SearchProductLotsAsync(string? keyword, int take = 20);

        Task<StocktakeSessionPagedResultModel> GetSessionsAsync(StocktakeFilterModel filter);
        Task<StocktakeSessionDetailModel?> GetSessionByIdAsync(long id);
        Task<StocktakeCountTaskModel?> GetCountTaskAsync(long id, long locationId);

        Task<StocktakeActionResultModel> CreateSessionAsync(CreateStocktakeSessionModel request);
        Task<StocktakeActionResultModel> StartSessionAsync(long id);
        Task<StocktakeActionResultModel> CancelSessionAsync(long id, string? notes);
        Task<StocktakeActionResultModel> SaveCountsAsync(long id, long locationId, List<StocktakeCountLineModel> lines);
        Task<StocktakeActionResultModel> SubmitLocationAsync(long id, long locationId, string? notes);
        Task<StocktakeActionResultModel> AddUnexpectedItemAsync(long id, UnexpectedStocktakeItemModel request);
        Task<StocktakeActionResultModel> RequestRecountAsync(long id, long locationId);
        Task<StocktakeActionResultModel> SubmitForReviewAsync(long id);
        Task<StocktakeActionResultModel> ApplyResolutionsAsync(long id, List<StocktakeResolutionModel> resolutions);
        Task<StocktakeActionResultModel> ApproveSessionAsync(long id, string? notes);
    }
}
