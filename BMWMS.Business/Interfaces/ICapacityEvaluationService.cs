using BMWMS.Business.DTOs.Capacity;

namespace BMWMS.Business.Interfaces;

public interface ICapacityEvaluationService
{
    Task<IReadOnlyDictionary<long, LocationCapacityEvaluationDto>> EvaluateCurrentAsync(
        IReadOnlyCollection<long> storageLocationIds);

    Task<IReadOnlyDictionary<long, LocationCapacityEvaluationDto>> EvaluateAsync(
        IReadOnlyCollection<CapacityAllocationDto> allocations,
        bool acquireLocationLocks = false);
}
