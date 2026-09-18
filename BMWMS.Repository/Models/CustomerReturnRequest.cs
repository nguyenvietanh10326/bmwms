namespace BMWMS.Repository.Models;

public class CustomerReturnRequest
{
    public long CustomerReturnRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string Status { get; set; } = "SUBMITTED";
    public string Reason { get; set; } = string.Empty;
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DecisionReason { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Customer Customer { get; set; } = null!;
    public ICollection<CustomerReturnRequestItem> Items { get; set; } = new List<CustomerReturnRequestItem>();
}

public class CustomerReturnRequestItem
{
    public long CustomerReturnRequestItemId { get; set; }
    public long CustomerReturnRequestId { get; set; }
    public long ProductId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public CustomerReturnRequest Request { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<CustomerReturnSourceAllocation> Allocations { get; set; } = new List<CustomerReturnSourceAllocation>();
}

public class CustomerReturnSourceAllocation
{
    public long CustomerReturnSourceAllocationId { get; set; }
    public long CustomerReturnRequestItemId { get; set; }
    public long SalesOrderDetailId { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public CustomerReturnRequestItem RequestItem { get; set; } = null!;
    public SalesOrderDetail SalesOrderDetail { get; set; } = null!;
}
