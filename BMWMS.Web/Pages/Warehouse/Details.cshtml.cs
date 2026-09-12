using BMWMS.Web.Models.Warehouse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Warehouse
{
    public class DetailsModel : PageModel
    {
        public WarehouseResponseDto Warehouse { get; set; } = default!;

        public IActionResult OnGet(long id) => RedirectToPage("/StorageLocations/Index");
    }
}
