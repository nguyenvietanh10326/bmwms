using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.Admin.Inbound;

[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
public class PutawayModel : PageModel
{
    private readonly InboundApiService _inboundApiService;
    private readonly HttpClient _httpClient;

    public PutawayModel(InboundApiService inboundApiService, IHttpClientFactory httpClientFactory)
    {
        _inboundApiService = inboundApiService;
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public InboundOrderDetailDto Order { get; set; } = default!;
    public InboundOrderItemDto Item { get; set; } = default!;
    public InboundReceiptDto Receipt { get; set; } = default!;

    [BindProperty]
    public List<PutawayInboundItemDto> PutawayDtos { get; set; } = new();

    public SelectList Locations { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(long id, long itemId, long lotId)
    {
        var data = await _inboundApiService.GetInboundOrderByIdAsync(id);
        if (data == null)
            return NotFound();

        Order = data;

        var userIdClaimForCheck = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole("WAREHOUSE_STAFF") && long.TryParse(userIdClaimForCheck, out var userId) && Order.AssignedToUserId != userId)
        {
            TempData["ErrorMessage"] = "Bạn không có quyền xếp vị trí cho lệnh này vì nó không được phân công cho bạn.";
            return RedirectToPage("Index");
        }
        Item = Order.Items.FirstOrDefault(x => x.InboundOrderItemId == itemId)!;
        if (Item == null) return NotFound();

        Receipt = Item.Receipts.FirstOrDefault(r => r.ProductLotId == lotId)!;
        if (Receipt == null) return NotFound();

        PutawayDtos.Add(new PutawayInboundItemDto 
        {
            InboundOrderItemId = itemId,
            ProductLotId = lotId,
            PutawayQuantity = Receipt.ReceivedQuantity - Receipt.PutawayQuantity
        });

        await LoadLocations(Order.WarehouseId);
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, long itemId, long lotId)
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync(id, itemId, lotId);
            return Page();
        }

        try
        {
            await _inboundApiService.PutawayBatchAsync(id, PutawayDtos);
            TempData["SuccessMessage"] = "Xếp vị trí thành công.";
            
            // Fetch order again to find if there are more items needing putaway
            var order = await _inboundApiService.GetInboundOrderByIdAsync(id);
            var nextItemNeedingPutaway = order?.Items.FirstOrDefault(i => i.Receipts.Any(r => r.ReceivedQuantity > r.PutawayQuantity));
            
            if (nextItemNeedingPutaway != null)
            {
                var nextReceipt = nextItemNeedingPutaway.Receipts.First(r => r.ReceivedQuantity > r.PutawayQuantity);
                return RedirectToPage("Putaway", new { id = id, itemId = nextItemNeedingPutaway.InboundOrderItemId, lotId = nextReceipt.ProductLotId });
            }

            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await OnGetAsync(id, itemId, lotId);
            return Page();
        }
    }

    private async Task LoadLocations(long warehouseId)
    {
        var response = await _httpClient.GetAsync($"/api/storagelocations?warehouseId={warehouseId}&locationType=BIN&status=AVAILABLE&pageIndex=1&pageSize=100");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<PagedResultModel<StorageLocationModel>>();
            if (result != null)
            {
                Locations = new SelectList(result.Items, "StorageLocationId", "LocationName");
            }
        }
        
        if (Locations == null)
        {
            Locations = new SelectList(new List<StorageLocationModel>(), "StorageLocationId", "LocationName");
        }
    }
}
