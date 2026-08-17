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
        public string Status { get; set; } = "Active";
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
        public string? ManagerName { get; set; }
        public int TotalLocations { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<StorageLocationItemDto> StorageLocations { get; set; } = new();
    }
    public class WarehouseFilterDto
    {
        public string? Keyword { get; set; } 
        public string? Status { get; set; }
        public bool? IsPrimary { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // DTO Tổng quan thông số Kho
    public class StorageLocationOverviewDto
    {
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int TotalZones { get; set; }
        public int TotalRacks { get; set; }
        public int TotalBins { get; set; }
        public int EmptyBins { get; set; }
        public int OccupiedBins { get; set; }
        public int LockedBins { get; set; }
        public double OccupancyRate => TotalBins == 0 ? 0 : Math.Round((double)OccupiedBins / TotalBins * 100, 1);
        public List<ZoneMatrixDto> Zones { get; set; } = new();
    }
    // DTO rút gọn chỉ truyền thông tin vị trí cần hiển thị trên danh sách
    public class StorageLocationItemDto
    {
        public long LocationID { get; set; }
        public string LocationCode { get; set; } = string.Empty; // VD: LOC-A1-01
        public string? LocationType { get; set; }               // VD: Kệ RACK-A1
        public string? Description { get; set; }                // Mô tả thêm
        public decimal? AreaSquareMeter { get; set; }           // Diện tích (10m²)
        public string Status { get; set; } = "Empty";           // Có hàng / Trống
        public int TotalProducts { get; set; }                  // Số lượng SP chứa
    }
    // DTO Cấp 1: Khu vực (Zone)
    public class ZoneMatrixDto
    {
        public string ZoneCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TotalRacks { get; set; }
        public int TotalBins { get; set; }
        public int OccupiedBins { get; set; }
        public List<RackMatrixDto> Racks { get; set; } = new();
    }

    // DTO Cấp 2: Kệ (Rack)
    public class RackMatrixDto
    {
        public string RackCode { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public int TotalBins { get; set; }
        public List<BinMatrixDto> Bins { get; set; } = new();
    }

    // DTO Cấp 3: Ô vị trí (Bin / Location)
    public class BinMatrixDto
    {
        public long LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty; // VD: LOC-A1-01
        public string Status { get; set; } = "EMPTY"; // EMPTY, OCCUPIED, LOCKED
        public string CapacityInfo { get; set; } = string.Empty; // VD: BIN - 10m²
        public int TotalProducts { get; set; }
        public int TotalLots { get; set; }
        public decimal TotalQuantity { get; set; }
    }
}
