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
}
