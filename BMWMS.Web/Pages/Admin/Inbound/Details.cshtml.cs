using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Inbound;

public class DetailsModel : PageModel
{
    private readonly InboundApiService _inboundApiService;

    public DetailsModel(InboundApiService inboundApiService)
    {
        _inboundApiService = inboundApiService;
    }

    public InboundOrderDetailDto Order { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var data = await _inboundApiService.GetInboundOrderByIdAsync(id);
        if (data == null)
            return NotFound();

        Order = data;
        return Page();
    }
    public async Task<IActionResult> OnPostConfirmAsync(long id)
    {
        try
        {
            await _inboundApiService.ConfirmInboundOrderAsync(id);
            TempData["SuccessMessage"] = "Lệnh nhập kho đã được xác nhận thành công.";
        }
        catch (System.Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(long id, string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy lệnh.";
                return RedirectToPage(new { id });
            }

            await _inboundApiService.CancelInboundOrderAsync(id, reason);
            TempData["SuccessMessage"] = "Lệnh nhập kho đã được hủy thành công.";
        }
        catch (System.Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToPage(new { id });
    }
}
