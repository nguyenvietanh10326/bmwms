using System;
using System.Collections.Generic;

namespace BMWMS.Business.Common;

/// <summary>
/// Hides legacy specialist and storage-conversion fields from Product Group
/// configuration. The new Attribute CRUD may still create descriptive fields
/// when the team has a documented reason for them.
/// </summary>
public static class ProductAttributePolicy
{
    private static readonly HashSet<string> RetiredLegacyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "STORAGE_VOLUME_M3_PER_BASE_UOM", "STORAGE_WEIGHT_KG_PER_BASE_UOM",
        "STORAGE_FACTOR_BASIS", "STORAGE_FACTOR_REFERENCE",
        "PRESSURE_RATING", "CURRENT_RATING", "VOLTAGE", "POWER",
        "WARRANTY_MONTHS", "USAGE_RATIO", "USAGE_SCOPE", "SOURCE_LOCATION",
        "MOISTURE_RESISTANCE", "CROSS_SECTION", "CORE_COUNT", "THREAD_TYPE",
        "STEEL_STANDARD", "QUALITY_GRADE", "COATING", "PLATING"
    };

    public static bool IsAllowed(string? code) =>
        code is not null && !RetiredLegacyCodes.Contains(code);
}
