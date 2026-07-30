namespace BMWMS.Business.DTOs.Supplier;

public class SupplierFilterDto
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
