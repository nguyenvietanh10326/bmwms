using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.Product
{
    public class CreateUnitOfMeasureDto
    {
        [Required(ErrorMessage = "Mã ÐVT không du?c d? tr?ng")]
        [StringLength(20)]
        public string UnitCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên ÐVT không du?c d? tr?ng")]
        [StringLength(50)]
        public string UnitName { get; set; } = string.Empty;

        public byte QuantityScale { get; set; } = 0;
        public string Status { get; set; } = "ACTIVE";
    }

    public class UpdateUnitOfMeasureDto
    {
        [Required(ErrorMessage = "Mã ÐVT không du?c d? tr?ng")]
        [StringLength(20)]
        public string UnitCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên ÐVT không du?c d? tr?ng")]
        [StringLength(50)]
        public string UnitName { get; set; } = string.Empty;

        public byte QuantityScale { get; set; } = 0;
        public string Status { get; set; } = "ACTIVE";
    }
}
