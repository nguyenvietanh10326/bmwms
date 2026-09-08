namespace BMWMS.Business.Configuration;

public sealed class WarehouseCapacityOptions
{
    public bool Enabled { get; set; }

    public string EnforcementMode { get; set; } = "ADVISORY";

    public bool IsStrict => string.Equals(EnforcementMode, "STRICT", StringComparison.OrdinalIgnoreCase);
}
