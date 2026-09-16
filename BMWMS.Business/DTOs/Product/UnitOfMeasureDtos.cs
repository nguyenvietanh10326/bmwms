using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.Product
{
    public class CreateUnitOfMeasureDto
    {
        [Required(ErrorMessage = "Mã đơn vị tính không được để trống.")]
        [StringLength(20)]
        public string UnitCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên đơn vị tính không được để trống.")]
        [StringLength(50)]
        public string UnitName { get; set; } = string.Empty;

        [Range(0, 4, ErrorMessage = "Độ chính xác số lượng chỉ từ 0 đến 4 chữ số thập phân.")]
        public byte QuantityScale { get; set; } = 0;
        [RegularExpression("^(ACTIVE|INACTIVE)$", ErrorMessage = "Trạng thái đơn vị tính không hợp lệ.")]
        public string Status { get; set; } = "ACTIVE";
    }

    public class UpdateUnitOfMeasureDto
    {
        [Required(ErrorMessage = "Mã đơn vị tính không được để trống.")]
        [StringLength(20)]
        public string UnitCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên đơn vị tính không được để trống.")]
        [StringLength(50)]
        public string UnitName { get; set; } = string.Empty;

        [Range(0, 4, ErrorMessage = "Độ chính xác số lượng chỉ từ 0 đến 4 chữ số thập phân.")]
        public byte QuantityScale { get; set; } = 0;
        [RegularExpression("^(ACTIVE|INACTIVE)$", ErrorMessage = "Trạng thái đơn vị tính không hợp lệ.")]
        public string Status { get; set; } = "ACTIVE";
    }
}
