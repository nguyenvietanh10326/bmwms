namespace BMWMS.Business.DTOs.Supplier;

public class SupplierProductDto
{
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string GroupName { get; set; } = null!;
    public decimal? LastPurchasePrice { get; set; }
    public int? LeadTimeDays { get; set; }
    public string Status { get; set; } = null!;
}

public class SupplierDetailResponseDto
{
    public long SupplierId { get; set; }
    public string SupplierCode { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public string TaxCode { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string Address { get; set; } = null!;
    public string RepresentativeName { get; set; } = null!;
    public string Status { get; set; } = null!;
    
    public decimal InboundYtdValue { get; set; }
    public int TotalProducts { get; set; }
    public int PreferredProducts { get; set; }

    public List<SupplierProductDto> SuppliedProducts { get; set; } = new();
}
