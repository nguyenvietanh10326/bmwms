namespace BMWMS.Business.Common;

public static class StocktakeLocationLockError
{
    public const string Code = "STOCKTAKE_LOCATION_LOCKED";
    public const string Message = "Vị trí hiện tại đang kiểm kho.";

    public static bool Is(Exception? exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains("51130", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("51131", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("Vị trí đang bị khóa bởi phiếu kiểm kho", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("vị trí đang được kiểm kho", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}