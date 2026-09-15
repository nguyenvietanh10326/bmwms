using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Admin.UnitOfMeasures
{
    public class EditModel : PageModel
    {
        private readonly UnitOfMeasureApiService _apiService;

        public EditModel(UnitOfMeasureApiService apiService)
        {
            _apiService = apiService;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public UpdateUnitOfMeasureDto Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var entity = await _apiService.GetByIdAsync(Id);
            if (entity == null)
            {
                TempData["ErrorMessage"] = "Không tìm th?y ÐVT.";
                return RedirectToPage("./Index");
            }

            Input = new UpdateUnitOfMeasureDto
            {
                UnitCode = entity.UnitCode,
                UnitName = entity.UnitName,
                QuantityScale = entity.QuantityScale,
                Status = entity.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await _apiService.UpdateAsync(Id, Input);
                TempData["SuccessMessage"] = "C?p nh?t don v? tính thành công.";
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
