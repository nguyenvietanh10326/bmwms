using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly ProductGroupApiService _productGroupService;

        public IndexModel(ProductGroupApiService productGroupService)
        {
            _productGroupService = productGroupService;
        }

        public PagedResultModel<ProductGroupViewModel> PagedGroups { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalGroups { get; set; }
        public int ActiveGroups { get; set; }
        public int TotalAttributesConfigured { get; set; }
        public int FefoGroupsCount { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            if (PageIndex < 1) PageIndex = 1;
            if (PageSize < 1) PageSize = 10;

            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }

            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"]?.ToString();
            }

            PagedGroups = await _productGroupService.GetPagedListAsync(Keyword, Status, PageIndex, PageSize);

            // Compute statistics
            var allGroups = await _productGroupService.GetAllActiveAsync();
            TotalGroups = PagedGroups.TotalCount;
            ActiveGroups = allGroups.Count;
            TotalAttributesConfigured = allGroups.Sum(g => g.AttributeCount);
            FefoGroupsCount = allGroups.Count(g => g.GroupCode == "PG06" || g.GroupCode == "PG07" || g.GroupCode == "PG08");
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(long id)
        {
            var (success, message) = await _productGroupService.ToggleStatusAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var (success, message) = await _productGroupService.DeleteAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
        }

        public async Task<IActionResult> OnPostCreateGroupAsync([FromForm] CreateProductGroupInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu nhập vào chưa hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
            }

            var (success, message, _) = await _productGroupService.CreateAsync(model);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
        }

        public async Task<IActionResult> OnPostUpdateGroupAsync(long id, [FromForm] UpdateProductGroupInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu nhập vào chưa hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
            }

            var (success, message) = await _productGroupService.UpdateAsync(id, model);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
        }
        public async Task<IActionResult> OnGetAttributesAllAsync()
        {
            var result = await _productGroupService.GetAllAttributesAsync();
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetGroupAttributesAsync(long id)
        {
            var result = await _productGroupService.GetGroupAttributesAsync(id);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostSaveGroupAttributesAsync(long id, [FromBody] List<GroupAttributeAssignmentInputModel> attributes)
        {
            var (success, message) = await _productGroupService.UpdateGroupAttributesAsync(id, attributes ?? new());
            if (success)
            {
                return new JsonResult(new { success = true, message });
            }
            return StatusCode(400, new { success = false, message });
        }
    }
}
