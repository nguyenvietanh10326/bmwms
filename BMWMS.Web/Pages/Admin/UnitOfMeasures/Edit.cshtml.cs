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

        public bool IsInUse { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var entity = await _apiService.GetByIdAsync(Id);
                if (entity == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn vị tính.";
                    return RedirectToPage("./Index");
                }

                IsInUse = entity.IsInUse;
                Input = new UpdateUnitOfMeasureDto
                {
                    UnitCode = entity.UnitCode,
                    UnitName = entity.UnitName,
                    QuantityScale = entity.QuantityScale,
                    Status = entity.Status
                };

                return Page();
            }
            catch
            {
                TempData["ErrorMessage"] = "Không thể tải thông tin đơn vị tính. Vui lòng thử lại.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var (success, message) = await _apiService.UpdateAsync(Id, Input);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                // Reload IsInUse for the view
                var entity = await _apiService.GetByIdAsync(Id);
                IsInUse = entity?.IsInUse ?? false;
                return Page();
            }

            TempData["SuccessMessage"] = message;
            return RedirectToPage("./Index");
        }
    }
}
