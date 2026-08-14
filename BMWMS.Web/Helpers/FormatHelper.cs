namespace BMWMS.Web.Helpers;

public static class FormatHelper
{
    private static readonly string[] IntegerUnits = { "cái", "chiếc", "hộp", "bao", "thùng", "cuộn", "bộ", "cây", "lọ", "chai", "tờ" };

    public static string FormatQuantity(decimal quantity, string? unitName)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            return quantity.ToString("G29"); // Default format removing trailing zeros

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
            return quantity.ToString("N0");
        }
        else
        {
            // For continuous units like Kg, m3, Tấn, Lít -> keep up to 3 decimal places without trailing zeros
            return quantity.ToString("0.###"); 
        }
    }
}
