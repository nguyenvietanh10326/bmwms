using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages
{
    public class IndexModel : PageModel
    {
        public bool IsLoggedIn { get; set; }
        public string? FullName { get; set; }
        public string? RoleName { get; set; }

        public void OnGet()
        {
            FullName = HttpContext.Session.GetString("FullName");
            RoleName = HttpContext.Session.GetString("RoleName");
            IsLoggedIn = !string.IsNullOrEmpty(FullName);
        }
    }
}
