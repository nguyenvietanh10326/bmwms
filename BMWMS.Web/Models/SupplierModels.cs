namespace BMWMS.Web.Models;

public class SupplierFilterModel
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SupplierListResponseModel
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

public class SupplierProductModel
{
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string GroupName { get; set; } = null!;
    public decimal? LastPurchasePrice { get; set; }
    public int? LeadTimeDays { get; set; }
    public string Status { get; set; } = null!;
}

public class SupplierDetailResponseModel
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

    public List<SupplierProductModel> SuppliedProducts { get; set; } = new();
}

public class PagedResultModel<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
