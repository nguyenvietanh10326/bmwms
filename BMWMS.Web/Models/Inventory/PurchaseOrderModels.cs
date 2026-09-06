using System;
using System.Collections.Generic;

namespace BMWMS.Web.Models.Inventory
{
    public class PurchaseOrderFilterDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public long? WarehouseId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }



    public class PurchaseOrderListDto
    {
        public long PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; } = null!;
        public long SupplierId { get; set; }
        public string SupplierCode { get; set; } = null!;
        public string SupplierName { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class PurchaseOrderDetailDto
    {
        public long PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; } = null!;
        public long SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = null!;
        public string? SupplierEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public long CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long? ConfirmedByUserId { get; set; }
        public string? ConfirmedByUserName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public bool CanSendToSupplier { get; set; }
        public bool CanCancel { get; set; }
        public bool CanCreateInbound { get; set; }
        public List<PurchaseOrderItemDto> Items { get; set; } = new();
        public List<RelatedInboundDto> Inbounds { get; set; } = new();
    }

    public class RelatedInboundDto
    {
        public long InboundOrderId { get; set; }
        public string InboundOrderNumber { get; set; } = string.Empty;
        public DateOnly ExpectedReceiptDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int LineCount { get; set; }
        public decimal ExpectedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
    }

    public class PurchaseOrderItemDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string Unit { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal PlannedInboundQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public string? Notes { get; set; }
    }
}
