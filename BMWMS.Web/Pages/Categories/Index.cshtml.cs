using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly CategoryApiService _categoryService;

        // Dữ liệu được render server-side (lần đầu load trang)
        public List<CategoryViewModel> Categories { get; set; } = [];
        public string? ErrorMessage { get; set; }

        public IndexModel(CategoryApiService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Server-side: Load categories khi trang được mở lần đầu.
        /// jQuery AJAX sẽ tự động refresh sau đó nếu cần.
        /// </summary>
        public async Task OnGetAsync()
        {
            var (data, error) = await _categoryService.GetAllAsync();
            Categories = data;
            ErrorMessage = error;
        }
    }
}
