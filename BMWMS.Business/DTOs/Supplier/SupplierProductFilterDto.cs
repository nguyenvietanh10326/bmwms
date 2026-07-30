namespace BMWMS.Business.DTOs.Supplier;

public class SupplierProductFilterDto
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SupplierProductResponseDto
{
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public string? SupplierProductCode { get; set; }
    public int? LeadTimeDays { get; set; }
    public string Status { get; set; } = null!;
}
