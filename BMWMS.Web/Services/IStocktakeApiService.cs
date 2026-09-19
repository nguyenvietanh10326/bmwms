using BMWMS.Web.Models;

namespace BMWMS.Web.Services
{
    public interface IStocktakeApiService
    {
        Task<List<StocktakeLocationOptionModel>> GetLocationsAsync(long warehouseId, List<long>? rackIds = null, List<long>? productGroupIds = null);
        Task<List<StocktakeStaffOptionModel>> GetStaffUsersAsync();

        Task<StocktakeSessionPagedResultModel> GetSessionsAsync(StocktakeFilterModel filter);
        Task<StocktakeSessionDetailModel?> GetSessionByIdAsync(long id);
        Task<StocktakeCountTaskModel?> GetCountTaskAsync(long id, long locationId);

        Task<StocktakeActionResultModel> CreateSessionAsync(CreateStocktakeSessionModel request);
        Task<StocktakeActionResultModel> StartSessionAsync(long id);
        Task<StocktakeActionResultModel> CancelSessionAsync(long id, string? notes);
        Task<StocktakeActionResultModel> SaveCountsAsync(long id, long locationId, List<StocktakeCountLineModel> lines);
        Task<StocktakeActionResultModel> SaveSessionCountsAsync(long id, SaveStocktakeSessionCountsModel request);
        Task<StocktakeActionResultModel> SubmitSessionAsync(long id);
        Task<StocktakeActionResultModel> ApproveSessionAsync(long id, StocktakeNoteModel request);
        Task<StocktakeActionResultModel> RejectSessionAsync(long id, string reason);
    }
}
