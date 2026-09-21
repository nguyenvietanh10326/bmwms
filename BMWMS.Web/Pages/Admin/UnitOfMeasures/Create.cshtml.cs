using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Admin.UnitOfMeasures
{
    public class CreateModel : PageModel
    {
        private readonly UnitOfMeasureApiService _apiService;

        public CreateModel(UnitOfMeasureApiService apiService)
        {
            _apiService = apiService;
        }

        [BindProperty]
        public CreateUnitOfMeasureDto Input { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var (success, message) = await _apiService.CreateAsync(Input);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return Page();
            }

            TempData["SuccessMessage"] = message;
            return RedirectToPage("./Index");
        }
    }
}
