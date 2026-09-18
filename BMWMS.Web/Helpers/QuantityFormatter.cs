using System.Globalization;
namespace BMWMS.Web.Helpers;

// Return-request forms use the same formatting policy as existing order forms.
public static class QuantityFormatter
{
    public static string Format(decimal quantity, byte scale) => FormatHelper.FormatQuantity(quantity, scale);
    public static string Step(byte scale) => (1m / (decimal)Math.Pow(10, scale)).ToString(CultureInfo.InvariantCulture);
}
