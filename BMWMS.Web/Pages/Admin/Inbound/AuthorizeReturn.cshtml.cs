using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace BMWMS.Web.Pages.Admin.Inbound;
// Compatibility for bookmarks: use the approved customer request workflow.
[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF")]
public class AuthorizeReturnModel : PageModel
{
    public IActionResult OnGet(long? salesOrderId) => User.IsInRole("SALES_STAFF") || User.IsInRole("SYSTEM_ADMIN")
        ? RedirectToPage("/CustomerReturns/Create", new { salesOrderId }) : RedirectToPage("/CustomerReturns/Index");
    public IActionResult OnPost() => RedirectToPage("/CustomerReturns/Index");
}
