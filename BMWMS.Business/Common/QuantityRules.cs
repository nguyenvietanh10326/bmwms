using BMWMS.Repository.Models;

namespace BMWMS.Business.Common;

/// <summary>
/// Utility class to validate product quantities based on UoM rules.
/// </summary>
public static class QuantityRules
{
    /// <summary>
    /// Ensures the quantity is valid for the given product's unit of measure.
    /// Throws ArgumentException if quantity is invalid (e.g., negative or zero).
    /// </summary>
    public static void EnsureValid(Product product, decimal quantity, string fieldName)
    {
        if (quantity < 0)
            throw new ArgumentException($"{fieldName} không được âm.");

        if (quantity == 0)
            throw new ArgumentException($"{fieldName} phải lớn hơn 0.");
    }
}
