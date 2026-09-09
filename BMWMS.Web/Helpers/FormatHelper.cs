using System.Globalization;

namespace BMWMS.Web.Helpers;

public static class FormatHelper
{
    private static readonly string[] IntegerUnits = { "cái", "chiếc", "hộp", "bao", "thùng", "cuộn", "bộ", "cây", "lọ", "chai", "tờ" };

    public static string FormatQuantity(decimal quantity, string? unitName)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            return quantity.ToString("0.############################", CultureInfo.InvariantCulture);

        var lowerUnit = unitName.Trim().ToLowerInvariant();
        
        bool isInteger = false;
        foreach (var u in IntegerUnits)
        {
            if (lowerUnit.Contains(u))
            {
                isInteger = true;
                break;
            }
        }

        if (isInteger)
        {
            return quantity.ToString("0", CultureInfo.InvariantCulture);
        }
        else
        {
            // For continuous units like Kg, m3, Tấn, Lít -> keep up to 3 decimal places without trailing zeros
            return quantity.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }

    public static string FormatQuantity(decimal quantity, byte quantityScale)
    {
        var format = quantityScale == 0
            ? "0"
            : $"0.{new string('#', quantityScale)}";
        return quantity.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>NULL là chưa cấu hình; giá trị 0 vẫn được hiển thị là 0.</summary>
    public static string FormatOptionalNumber(decimal? value, int maximumScale = 4, string emptyValue = "—")
    {
        if (!value.HasValue) return emptyValue;
        var safeScale = Math.Clamp(maximumScale, 0, 28);
        var format = safeScale == 0 ? "0" : $"0.{new string('#', safeScale)}";
        return value.Value.ToString(format, CultureInfo.InvariantCulture);
    }
}
