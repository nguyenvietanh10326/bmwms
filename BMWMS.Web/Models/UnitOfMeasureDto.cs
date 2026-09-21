using System.ComponentModel.DataAnnotations;

namespace BMWMS.Web.Models
{
    public class UnitOfMeasureDto
    {
        public int UnitOfMeasureId { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public byte QuantityScale { get; set; } = 0;
        public string Status { get; set; } = "ACTIVE";
        public bool IsInUse { get; set; }
    }

    public class CreateUnitOfMeasureDto
    {
        [Required(ErrorMessage = "Mã ĐVT không được để trống")]
        [StringLength(20)]
        public string UnitCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên ĐVT không được để trống")]
        [StringLength(50)]
        public string UnitName { get; set; } = string.Empty;

        [Range(0, 4, ErrorMessage = "Thang tỷ lệ phải từ 0 đến 4")]
        public byte QuantityScale { get; set; } = 0;
        public string Status { get; set; } = "ACTIVE";
    }

    public class UpdateUnitOfMeasureDto
    {
        [Required(ErrorMessage = "Mã ĐVT không được để trống")]
        [StringLength(20)]
        public string UnitCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên ĐVT không được để trống")]
        [StringLength(50)]
        public string UnitName { get; set; } = string.Empty;

        [Range(0, 4, ErrorMessage = "Thang tỷ lệ phải từ 0 đến 4")]
        public byte QuantityScale { get; set; } = 0;
        public string Status { get; set; } = "ACTIVE";
    }
}
