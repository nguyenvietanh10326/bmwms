namespace BMWMS.Business.DTOs.Inventory;

public class RejectSalesOrderRequest
{
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}
