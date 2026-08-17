using BMWMS.Repository.Models;

namespace BMWMS.Business.Common;

public static class QuantityRules
{
    public static bool IsValid(decimal quantity, byte quantityScale)
        => quantity > 0 && decimal.Round(quantity, quantityScale) == quantity;

    public static void EnsureValid(Product product, decimal quantity, string fieldName)
    {
        var scale = product.UnitOfMeasure?.QuantityScale ?? 0;
        if (!IsValid(quantity, scale))
        {
            var rule = scale == 0 ? "số nguyên" : $"tối đa {scale} chữ số thập phân";
            throw new ArgumentException($"{fieldName} của {product.ProductCode} phải là {rule} theo đơn vị {product.UnitOfMeasure?.UnitName ?? "đã cấu hình"}.");
        }
    }

    public static string Step(byte quantityScale)
        => quantityScale == 0 ? "1" : $"0.{new string('0', quantityScale - 1)}1";
}
