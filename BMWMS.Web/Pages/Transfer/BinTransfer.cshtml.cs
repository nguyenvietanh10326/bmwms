using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Transfer
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
    public class BinTransferModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToPage("/Transfer/Create");
        }
    }
}

