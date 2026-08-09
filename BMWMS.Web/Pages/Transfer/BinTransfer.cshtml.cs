using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Transfer
{
    public class BinTransferModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToPage("/Transfer/Create");
        }
    }
}
