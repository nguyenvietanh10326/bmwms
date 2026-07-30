namespace BMWMS.Business.DTOs.Supplier;

public class SupplierCreateRequestDto
{
    public string SupplierCode { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public string TaxCode { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string Address { get; set; } = null!;
    public string RepresentativeName { get; set; } = null!;
}
