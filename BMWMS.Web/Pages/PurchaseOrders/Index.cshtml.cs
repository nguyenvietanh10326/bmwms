using Microsoft.AspNetCore.Authorization;
using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Models.Warehouse;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.PurchaseOrders;

[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    public class IndexModel : PageModel
{
    private readonly PurchaseOrderApiService _apiService;

    public IndexModel(PurchaseOrderApiService apiService)
    {
        _apiService = apiService;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 20;
    
    public List<PurchaseOrderListDto> Orders { get; set; } = new();
    public int TotalPages { get; set; }

    public async Task OnGetAsync()
    {
        var filter = new PurchaseOrderFilterDto
        {
            SearchTerm = SearchTerm,
            Status = Status,
            PageIndex = PageIndex,
            PageSize = PageSize
        };

        var pagedResult = await _apiService.GetPagedPurchaseOrdersAsync(filter);
        if (pagedResult != null)
        {
            Orders = pagedResult.Items ?? new List<PurchaseOrderListDto>();
            TotalPages = pagedResult.TotalPages;
        }
    }
}

