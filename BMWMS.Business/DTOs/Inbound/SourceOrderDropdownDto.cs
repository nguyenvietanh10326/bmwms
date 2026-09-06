namespace BMWMS.Business.DTOs.Inbound;

public class SourceOrderDropdownDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PurchaseOrderInboundSourceDto
{
    public long PurchaseOrderId { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal ActivePlannedQuantity { get; set; }
    public decimal AvailableToPlanQuantity { get; set; }
    public int ProductLineCount { get; set; }
    public int PreviousReceiptCount { get; set; }
    public DateTime? LatestReceiptAt { get; set; }
    public bool IsFollowUpReceipt { get; set; }
    public string SearchText { get; set; } = string.Empty;
}
