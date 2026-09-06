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

namespace BMWMS.Web.Pages.Admin.Inbound;

[Authorize(Roles = "WAREHOUSE_STAFF")]
public class PutawayModel : PageModel
{
    private readonly InboundApiService _inboundApiService;
    private readonly HttpClient _httpClient;

    public PutawayModel(InboundApiService inboundApiService, IHttpClientFactory httpClientFactory)
    {
        _inboundApiService = inboundApiService;
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public InboundOrderDetailDto Order { get; private set; } = default!;
    public List<PutawayReceiptRow> ReceiptRows { get; private set; } = new();

    [BindProperty]
    public List<PutawayInboundItemDto> PutawayDtos { get; set; } = new();

    // Keep the former query parameters optional so links generated before this
    // page became an order-wide operation continue to work.
    public async Task<IActionResult> OnGetAsync(long id, long? itemId = null, long? lotId = null)
    {
        var result = await LoadPageAsync(id);
        return result ?? Page();
    }

    public async Task<IActionResult> OnPostAsync(long id)
    {
        if (!ModelState.IsValid)
        {
            await LoadPageAsync(id, redirectWhenInvalid: false);
            return Page();
        }

        try
        {
            await _inboundApiService.PutawayBatchAsync(id, PutawayDtos);
            TempData["SuccessMessage"] = "Đã xác nhận đầy đủ vị trí cất hàng và cập nhật tồn kho.";
            return RedirectToPage("Details", new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await LoadPageAsync(id, redirectWhenInvalid: false);
            return Page();
        }
    }

    private async Task<IActionResult?> LoadPageAsync(long id, bool redirectWhenInvalid = true)
    {
        var data = await _inboundApiService.GetInboundOrderByIdAsync(id);
        if (data == null)
            return NotFound();

        Order = data;
        if (Order.Status != "RECEIVED")
        {
            if (!redirectWhenInvalid)
            {
                ModelState.AddModelError(string.Empty, "Phiếu không còn hàng chờ xếp vị trí.");
                return null;
            }

            TempData["ErrorMessage"] = "Chỉ được xếp vị trí khi phiếu đã ghi nhận thực nhận và còn hàng chưa cất.";
            return RedirectToPage("Details", new { id });
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var userId) || Order.AssignedToUserId != userId)
        {
            if (!redirectWhenInvalid)
            {
                ModelState.AddModelError(string.Empty, "Bạn không phải nhân viên chịu trách nhiệm cho đợt nhập này.");
                return null;
            }

            TempData["ErrorMessage"] = "Bạn không phải nhân viên chịu trách nhiệm cho đợt nhập này.";
            return RedirectToPage("Index");
        }

        ReceiptRows = Order.Items
            .SelectMany(item => item.Receipts
                .Where(receipt => receipt.ConditionStatus == "GOOD" && receipt.ReceivedQuantity > receipt.PutawayQuantity)
                .Select(receipt => new PutawayReceiptRow
                {
                    InboundOrderItemId = item.InboundOrderItemId,
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    UnitName = item.UnitName,
                    QuantityScale = item.QuantityScale,
                    ProductLotId = receipt.ProductLotId,
                    LotNumber = receipt.LotNumber,
                    ExpiryDate = receipt.ExpiryDate,
                    RemainingQuantity = receipt.ReceivedQuantity - receipt.PutawayQuantity
                }))
            .ToList();

        foreach (var productGroup in ReceiptRows.GroupBy(row => row.ProductId))
        {
            var locations = await LoadLocationsAsync(Order.WarehouseId, productGroup.Key);
            foreach (var row in productGroup)
                row.Locations = locations;
        }

        return null;
    }

    private async Task<List<PutawayLocationOption>> LoadLocationsAsync(long warehouseId, long productId)
    {
        var response = await _httpClient.GetAsync(
            $"api/Inbounds/putaway-locations?warehouseId={warehouseId}&productId={productId}");
        if (!response.IsSuccessStatusCode)
            return new List<PutawayLocationOption>();

        return await response.Content.ReadFromJsonAsync<List<PutawayLocationOption>>()
            ?? new List<PutawayLocationOption>();
    }
}

public sealed class PutawayReceiptRow
{
    public long InboundOrderItemId { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public byte QuantityScale { get; set; }
    public long ProductLotId { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal RemainingQuantity { get; set; }
    public List<PutawayLocationOption> Locations { get; set; } = new();
}

public class PutawayLocationOption
{
    public long StorageLocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public long? ZoneId { get; set; }
    public string ZoneCode { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public long? RackId { get; set; }
    public string RackCode { get; set; } = string.Empty;
    public string RackName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CurrentOnHandQuantity { get; set; }
    public int StoredProductCount { get; set; }
    public bool IsRecommended { get; set; }
    public int Priority { get; set; }
    public bool IsDefault { get; set; }
    public string HierarchyPath => $"{(string.IsNullOrWhiteSpace(ZoneCode) ? "Chưa phân khu" : ZoneCode)} / {(string.IsNullOrWhiteSpace(RackCode) ? "Chưa phân kệ" : RackCode)} / {LocationCode}";
    public string DisplayName => $"{(IsDefault ? "★ " : IsRecommended ? "• " : string.Empty)}{HierarchyPath} — {LocationName} · Tồn {CurrentOnHandQuantity:0.####}";
}
