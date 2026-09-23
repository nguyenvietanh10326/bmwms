namespace BMWMS.Business.Common;

/// <summary>
/// Canonical business actors used by the application.  Older demo databases
/// may still contain ACCOUNTANT and DIRECTOR role codes; those are account
/// naming aliases, not additional actors.
/// </summary>
public static class BusinessRoleCodes
{
    public const string SystemAdmin = "SYSTEM_ADMIN";
    public const string WarehouseManager = "WAREHOUSE_MANAGER";
    public const string WarehouseStaff = "WAREHOUSE_STAFF";
    public const string PurchasingStaff = "PURCHASING_STAFF";
    public const string SalesStaff = "SALES_STAFF";

    public static string Normalize(string? roleCode)
    {
        var value = roleCode?.Trim().ToUpperInvariant() ?? string.Empty;
        return value switch
        {
            "ACCOUNTANT" => PurchasingStaff,
            "DIRECTOR" => SalesStaff,
            _ => value
        };
    }

    public static bool IsPurchasing(string? roleCode) => Normalize(roleCode) == PurchasingStaff;
    public static bool IsSales(string? roleCode) => Normalize(roleCode) == SalesStaff;
}
