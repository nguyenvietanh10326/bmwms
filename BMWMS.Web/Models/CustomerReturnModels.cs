namespace BMWMS.Web.Models;

public class ReturnCustomerLookupModel
{
    public long CustomerId { get; set; }
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
}

public class CustomerReturnSourceModel
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public byte QuantityScale { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal AvailableToReturn { get; set; }
}
public class CustomerReturnModel
{
    public long? SalesOrderId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public bool CanEdit { get; set; }
    public long CreatedByUserId { get; set; }
    public long Id { get; set; }
    public string RequestNumber { get; set; } = "";
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string Status { get; set; } = "";
    public string Reason { get; set; } = "";
    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RowVersion { get; set; } = "";
    public List<CustomerReturnItemModel> Items { get; set; } = new();
    public List<long> InboundIds { get; set; } = new();
}
public class CustomerReturnItemModel : CustomerReturnSourceModel
{
    public decimal RequestedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public List<CustomerReturnProofModel> Sources { get; set; } = new();
}
public class CustomerReturnProofModel
{
    public long SalesOrderId { get; set; }
    public string SalesOrderNumber { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
}

public class CustomerReturnSalesOrderModel
{
    public long SalesOrderId { get; set; }
    public string SalesOrderNumber { get; set; } = "";
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
}
