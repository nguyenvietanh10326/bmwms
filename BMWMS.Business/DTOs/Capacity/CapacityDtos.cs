namespace BMWMS.Business.DTOs.Capacity;

public static class CapacityEvaluationStatuses
{
    public const string Available = "AVAILABLE";
    public const string Exceeded = "EXCEEDED";
    public const string Unknown = "UNKNOWN";
    public const string NotConfigured = "NOT_CONFIGURED";
}

public sealed class CapacityAllocationDto
{
    public long StorageLocationId { get; set; }
    public long ProductId { get; set; }
    /// <summary>
    /// Biến động tồn vật lý theo đơn vị cơ sở. Số dương là đưa hàng vào,
    /// số âm là lấy hàng ra. Inbound hiện chỉ truyền số dương.
    /// </summary>
    public decimal Quantity { get; set; }
}

public sealed class LocationCapacityEvaluationDto
{
    public long StorageLocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;

    public decimal? MaxWeightKg { get; set; }
    public decimal? CurrentWeightKg { get; set; }
    public decimal? AddedWeightKg { get; set; }
    public decimal? ProjectedWeightKg { get; set; }
    public string WeightStatus { get; set; } = CapacityEvaluationStatuses.NotConfigured;

    public decimal? MaxVolumeM3 { get; set; }
    public decimal? CurrentVolumeM3 { get; set; }
    public decimal? AddedVolumeM3 { get; set; }
    public decimal? ProjectedVolumeM3 { get; set; }
    public string VolumeStatus { get; set; } = CapacityEvaluationStatuses.NotConfigured;

    public string OverallStatus { get; set; } = CapacityEvaluationStatuses.NotConfigured;
    public List<string> MissingWeightProductCodes { get; set; } = new();
    public List<string> MissingVolumeProductCodes { get; set; } = new();
    public List<CapacityScopeEvaluationDto> Scopes { get; set; } = new();
}

public sealed class CapacityScopeEvaluationDto
{
    public string ScopeType { get; set; } = string.Empty;
    public long ScopeId { get; set; }
    public string ScopeCode { get; set; } = string.Empty;
    public decimal? MaxWeightKg { get; set; }
    public decimal? CurrentWeightKg { get; set; }
    public decimal? AddedWeightKg { get; set; }
    public decimal? ProjectedWeightKg { get; set; }
    public string WeightStatus { get; set; } = CapacityEvaluationStatuses.NotConfigured;
    public decimal? MaxVolumeM3 { get; set; }
    public decimal? CurrentVolumeM3 { get; set; }
    public decimal? AddedVolumeM3 { get; set; }
    public decimal? ProjectedVolumeM3 { get; set; }
    public string VolumeStatus { get; set; } = CapacityEvaluationStatuses.NotConfigured;
    public string OverallStatus { get; set; } = CapacityEvaluationStatuses.NotConfigured;
    public List<string> MissingWeightProductCodes { get; set; } = new();
    public List<string> MissingVolumeProductCodes { get; set; } = new();
}
