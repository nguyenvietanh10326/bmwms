namespace BMWMS.Business.DTOs.Supplier;

public class SupplierListResponseDto
{
    public long SupplierId { get; set; }
    public string SupplierCode { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string RepresentativeName { get; set; } = null!;
    public string TaxCode { get; set; } = null!;
    public int SuppliedProductCount { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Status { get; set; } = null!;
}
