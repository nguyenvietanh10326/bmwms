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

            try
            {
                await _apiService.CreateAsync(Input);
                TempData["SuccessMessage"] = "Thêm don v? tính thành công.";
                return RedirectToPage("./Index");
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
