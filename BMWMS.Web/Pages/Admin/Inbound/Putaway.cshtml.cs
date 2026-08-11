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
    public PutawayInboundItemDto PutawayDto { get; set; } = new();

    public SelectList Locations { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(long id, long itemId, long lotId)
    {
        var data = await _inboundApiService.GetInboundOrderByIdAsync(id);
        if (data == null)
            return NotFound();

        Order = data;
        Item = Order.Items.FirstOrDefault(x => x.InboundOrderItemId == itemId)!;
        if (Item == null) return NotFound();

        Receipt = Item.Receipts.FirstOrDefault(r => r.ProductLotId == lotId)!;
        if (Receipt == null) return NotFound();

        PutawayDto.InboundOrderItemId = itemId;
        PutawayDto.ProductLotId = lotId;
        PutawayDto.PutawayQuantity = Receipt.ReceivedQuantity - Receipt.PutawayQuantity;

        await LoadLocations();
        
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
            await _inboundApiService.PutawayItemAsync(id, PutawayDto);
            TempData["SuccessMessage"] = "Xếp vị trí thành công.";
            return RedirectToPage("Details", new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await OnGetAsync(id, itemId, lotId);
            return Page();
        }
    }

    private async Task LoadLocations()
    {
        var response = await _httpClient.GetAsync($"/api/storagelocations?pageIndex=1&pageSize=100");
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
