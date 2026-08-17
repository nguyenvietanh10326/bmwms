using System;
using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Inventory
{
    public class PurchaseOrderCreateDto
    {
        public long SupplierId { get; set; }
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }
        public string? Notes { get; set; }

        public List<PurchaseOrderDetailCreateDto> OrderDetails { get; set; } = new();
    }

    public class PurchaseOrderDetailCreateDto
    {
        public long ProductId { get; set; }
        public decimal OrderedQuantity { get; set; }
        public string? Notes { get; set; }
    }
}
