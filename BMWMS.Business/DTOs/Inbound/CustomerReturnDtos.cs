namespace BMWMS.Business.DTOs.Inbound;

public class CreateCustomerReturnRequestDto
{
    public long SalesOrderId { get; set; }
    public string? RowVersion { get; set; }
    public long CustomerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<CustomerReturnLineInput> Items { get; set; } = new();
}
public class CustomerReturnLineInput
{
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
}
public class CustomerReturnDecisionDto
{
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
public class CustomerReturnSourceDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public byte QuantityScale { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal AvailableToReturn { get; set; }
}
public class CustomerReturnRequestDto
{
    public long? SalesOrderId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public bool CanEdit { get; set; }
    public long CreatedByUserId { get; set; }
    public long Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<CustomerReturnRequestLineDto> Items { get; set; } = new();
    public List<long> InboundIds { get; set; } = new();
}
public class CustomerReturnRequestLineDto : CustomerReturnSourceDto
{
    public decimal RequestedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public List<CustomerReturnProofDto> Sources { get; set; } = new();
}
public class CustomerReturnProofDto
{
    public long SalesOrderId { get; set; }
    public string SalesOrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
}

public class CustomerReturnSalesOrderDto
{
    public long SalesOrderId { get; set; }
    public string SalesOrderNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}
