using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

public class SupplierCreateRequestModel
{
    [Required(ErrorMessage = "Vui lòng nhập Mã nhà cung cấp")]
    [RegularExpression(@"^[A-Z0-9_\-]{2,30}$", ErrorMessage = "Mã nhà cung cấp từ 2-30 ký tự, chỉ gồm chữ in hoa, số, dấu gạch ngang hoặc gạch dưới")]
    public string SupplierCode { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Tên nhà cung cấp")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên nhà cung cấp từ 2 đến 200 ký tự")]
    public string SupplierName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Mã số thuế")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Mã số thuế từ 10 đến 20 ký tự")]
    public string TaxCode { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Số điện thoại")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Địa chỉ")]
    [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự")]
    public string Address { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Người liên hệ")]
    [StringLength(150, ErrorMessage = "Người liên hệ tối đa 150 ký tự")]
    public string RepresentativeName { get; set; } = null!;
}

public class SupplierUpdateRequestModel
{
    [Required(ErrorMessage = "Vui lòng nhập Mã nhà cung cấp")]
    [RegularExpression(@"^[A-Z0-9_\-]{2,30}$", ErrorMessage = "Mã nhà cung cấp từ 2-30 ký tự, chỉ gồm chữ in hoa, số, dấu gạch ngang hoặc gạch dưới")]
    public string SupplierCode { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Tên nhà cung cấp")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên nhà cung cấp từ 2 đến 200 ký tự")]
    public string SupplierName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Mã số thuế")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Mã số thuế từ 10 đến 20 ký tự")]
    public string TaxCode { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Số điện thoại")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Địa chỉ")]
    [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự")]
    public string Address { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Người liên hệ")]
    [StringLength(150, ErrorMessage = "Người liên hệ tối đa 150 ký tự")]
    public string RepresentativeName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    public string Status { get; set; } = null!;
}

public class PagedResultModel<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
