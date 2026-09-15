using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Admin.UnitOfMeasures
{
    public class IndexModel : PageModel
    {
        private readonly UnitOfMeasureApiService _apiService;

        public IndexModel(UnitOfMeasureApiService apiService)
        {
            _apiService = apiService;
        }

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        public List<UnitOfMeasureDto> UnitOfMeasures { get; set; } = new();

        public async Task OnGetAsync()
        {
            UnitOfMeasures = await _apiService.GetAllAsync(Keyword);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                await _apiService.DeleteAsync(id);
                
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToPage("./Index");
        }
    }
}
