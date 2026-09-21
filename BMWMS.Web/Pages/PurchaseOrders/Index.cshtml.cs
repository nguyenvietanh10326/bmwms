using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Models.Warehouse;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
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

    [BindProperty(SupportsGet = true)] public string? SortOrder { get; set; }
    public int PageSize { get; set; } = 20;
    
    public List<PurchaseOrderListDto> Orders { get; set; } = new();
    public int TotalPages { get; set; }

    public async Task OnGetAsync()
    {
        SortOrder = NormalizeSort(SortOrder, User.IsInRole("WAREHOUSE_MANAGER") ? "approval" : "priority");
        ModelState.Remove(nameof(SortOrder));
        var filter = new PurchaseOrderFilterDto
        {
            SearchTerm = SearchTerm,
            Status = Status,
            SortOrder = SortOrder,
            PageIndex = PageIndex,
            PageSize = PageSize
        };

        PageIndex = Math.Max(1, PageIndex);
        filter.PageIndex = PageIndex;
        try {
        var pagedResult = await _apiService.GetPagedPurchaseOrdersAsync(filter);
        if (pagedResult != null)
        {
            Orders = pagedResult.Items ?? new List<PurchaseOrderListDto>();
            TotalPages = pagedResult.TotalPages;
        }
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        { ModelState.AddModelError(string.Empty, ex is InvalidOperationException ? ex.Message : "Không tải được danh sách PO. Kiểm tra kết nối và schema database trước khi thử lại."); }
    }
    private static string NormalizeSort(string? value, string fallback)
        => value?.Trim().ToLowerInvariant() is "approval" or "priority" or "expected" or "newest" or "oldest" ? value.Trim().ToLowerInvariant() : fallback;
}
