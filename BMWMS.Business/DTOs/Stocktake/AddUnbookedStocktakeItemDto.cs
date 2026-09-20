using System;
using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.Stocktake;

public class AddUnbookedStocktakeItemDto
{
    [Required(ErrorMessage = "Vui lòng chọn vị trí phát hiện hàng.")]
    [Range(1, long.MaxValue, ErrorMessage = "Vị trí phát hiện không hợp lệ.")]
    public long StorageLocationId { get; set; }

    public long? TargetStorageLocationId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn sản phẩm.")]
    [Range(1, long.MaxValue, ErrorMessage = "Sản phẩm không hợp lệ.")]
    public long ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng đếm thực tế.")]
    [Range(0.0001, 1000000, ErrorMessage = "Số lượng đếm phải lớn hơn 0.")]
    public decimal CountedQuantity { get; set; }

    public DateOnly? FirstReceivedDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    public string? Notes { get; set; }
}

public class CompatibleLocationDto
{
    public long StorageLocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string LocationPath { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public decimal? MaxCapacity { get; set; }
    public decimal CurrentOccupancy { get; set; }
    public decimal RemainingCapacity { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public bool IsCompatibleZone { get; set; } = true;
}

public class SetTargetLocationDto
{
    public long? TargetStorageLocationId { get; set; }
}
