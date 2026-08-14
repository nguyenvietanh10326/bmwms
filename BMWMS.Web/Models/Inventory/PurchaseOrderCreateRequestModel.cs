using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BMWMS.Web.Models.Inventory
{
    public class PurchaseOrderCreateRequestModel
    {
        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        public long SupplierId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đặt hàng")]
        public DateOnly OrderDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày giao dự kiến")]
        public DateOnly? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }

        public List<PurchaseOrderDetailRequestModel> OrderDetails { get; set; } = new();
    }

    public class PurchaseOrderDetailRequestModel
    {
        [Required(ErrorMessage = "Vui lòng chọn vật tư")]
        public long ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public decimal OrderedQuantity { get; set; }

        public string? Notes { get; set; }
    }
}
