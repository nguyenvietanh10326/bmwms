using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Auth;

public class AccessDeniedModel : PageModel
{
    public void OnGet() => Response.StatusCode = StatusCodes.Status403Forbidden;
}
