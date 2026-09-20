using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly ProductGroupApiService _productGroupService;
        private readonly ProductApiService _productApiService;

        public IndexModel(ProductGroupApiService productGroupService, ProductApiService productApiService)
        {
            _productGroupService = productGroupService;
            _productApiService = productApiService;
        }

        public List<SelectListItem> UomOptions { get; set; } = new();

        public string UnitName(int? unitId) => unitId.HasValue
            ? UomOptions.FirstOrDefault(option => option.Value == unitId.Value.ToString())?.Text ?? "Chưa cấu hình"
            : "Chưa cấu hình";

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

        public bool CanManage { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }

        public async Task OnGetAsync()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            CanManage = roleCode is "SYSTEM_ADMIN" or "WAREHOUSE_MANAGER";

            if (PageIndex < 1) PageIndex = 1;
            if (PageSize < 1) PageSize = 10;

            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
            }

            if (TempData["WarningMessage"] != null)
            {
                WarningMessage = TempData["WarningMessage"]?.ToString();
            }

            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"]?.ToString();
            }

            PagedGroups = await _productGroupService.GetPagedListAsync(Keyword, Status, PageIndex, PageSize);
            UomOptions = (await _productApiService.GetUnitsOfMeasureAsync())
                .Select(unit => new SelectListItem($"{unit.UnitName} ({unit.UnitCode})", unit.UnitOfMeasureId.ToString()))
                .ToList();

            // Compute statistics
            var allGroups = await _productGroupService.GetAllActiveAsync();
            TotalGroups = PagedGroups.TotalCount;
            ActiveGroups = allGroups.Count;
            TotalAttributesConfigured = allGroups.Sum(g => g.AttributeCount);
            FefoGroupsCount = allGroups.Count(g => g.GroupCode == "PG06" || g.GroupCode == "PG07" || g.GroupCode == "PG08");
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(long id)
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            if (roleCode is not ("SYSTEM_ADMIN" or "WAREHOUSE_MANAGER"))
            {
                TempData["WarningMessage"] = "Bạn không có quyền thay đổi trạng thái nhóm sản phẩm! Chỉ Quản trị hệ thống hoặc Trưởng kho mới được thao tác.";
                return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
            }

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
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            if (roleCode is not ("SYSTEM_ADMIN" or "WAREHOUSE_MANAGER"))
            {
                TempData["WarningMessage"] = "Bạn không có quyền xóa nhóm sản phẩm! Chỉ Quản trị hệ thống hoặc Trưởng kho mới được thao tác.";
                return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
            }

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
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            if (roleCode is not ("SYSTEM_ADMIN" or "WAREHOUSE_MANAGER"))
            {
                TempData["WarningMessage"] = "Bạn không có quyền thêm mới nhóm sản phẩm! Chỉ Quản trị hệ thống hoặc Trưởng kho mới được thao tác.";
                return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
            }

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
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            if (roleCode is not ("SYSTEM_ADMIN" or "WAREHOUSE_MANAGER"))
            {
                TempData["WarningMessage"] = "Bạn không có quyền cập nhật nhóm sản phẩm! Chỉ Quản trị hệ thống hoặc Trưởng kho mới được thao tác.";
                return RedirectToPage(new { Keyword, Status, PageIndex, PageSize });
            }

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
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            if (roleCode is not ("SYSTEM_ADMIN" or "WAREHOUSE_MANAGER"))
            {
                return StatusCode(403, new { success = false, message = "Bạn không có quyền cấu hình thuộc tính cho nhóm sản phẩm! Chỉ Quản trị hệ thống hoặc Trưởng kho mới được thao tác." });
            }

            var (success, message) = await _productGroupService.UpdateGroupAttributesAsync(id, attributes ?? new());
            if (success)
            {
                return new JsonResult(new { success = true, message });
            }
            return StatusCode(400, new { success = false, message });
        }
    }
}
