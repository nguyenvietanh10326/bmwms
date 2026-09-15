using System;
using System.Collections.Generic;

namespace BMWMS.Business.Common;

// Only descriptive, demonstrable attributes belong in Product Group configuration.
// Legacy technical and storage-conversion attributes remain in the database for
// historical rows but are not offered for new/edited product master data.
public static class ProductAttributePolicy
{
    private static readonly HashSet<string> AllowedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "BRAND", "COLOR", "MATERIAL", "MATERIAL_TYPE", "PHYSICAL_FORM",
        "DIMENSION", "LENGTH", "WIDTH", "THICKNESS", "DIAMETER",
        "GRAIN_SIZE", "MODEL", "VOLUME",
        "CEMENT_TYPE", "PAINT_TYPE", "GLASS_TYPE", "GLUE_TYPE"
    };

    public static bool IsAllowed(string? attributeCode) =>
        attributeCode is not null && AllowedCodes.Contains(attributeCode);
}
