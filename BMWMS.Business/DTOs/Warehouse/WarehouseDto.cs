using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.DTOs.Warehouse
{
    public class CreateWarehouseDto
    {
        [Required(ErrorMessage = "Mã kho không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã kho không vượt quá 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-0_-]+$", ErrorMessage = "Mã kho chỉ chứa chữ cái, số, dấu gạch ngang hoặc gạch dưới.")]
        public string WarehouseCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên kho không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên kho không vượt quá 200 ký tự.")]
        public string WarehouseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không vượt quá 500 ký tự.")]
        public string Address { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
        [StringLength(20, ErrorMessage = "Số điện thoại không vượt quá 20 ký tự.")]
        public string? PhoneNumber { get; set; }

        public bool IsPrimary { get; set; } = false;
    }

    // Request cập nhật kho
    public class UpdateWarehouseDto : CreateWarehouseDto
    {
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [RegularExpression(@"^(ACTIVE|INACTIVE)$", ErrorMessage = "Trạng thái chỉ nhận giá trị ACTIVE hoặc INACTIVE.")]
        public string Status { get; set; } = "ACTIVE";
    }

    // Response trả ra cho UI
    public class WarehouseResponseDto
    {
        public long WarehouseID { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsPrimary { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class WarehouseFilterDto
    {
        public string? Keyword { get; set; } 
        public string? Status { get; set; }
        public bool? IsPrimary { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
