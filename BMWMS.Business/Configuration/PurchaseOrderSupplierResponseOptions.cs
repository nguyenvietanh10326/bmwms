namespace BMWMS.Business.Configuration;

public sealed class PurchaseOrderSupplierResponseOptions
{
    public string PublicBaseUrl { get; set; } = "http://localhost:5076";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpireHours { get; set; } = 168;

    public string Issuer { get; set; } = "BMWMS.SupplierResponse";
    public string Audience { get; set; } = "BMWMS.PurchaseOrder";
}
