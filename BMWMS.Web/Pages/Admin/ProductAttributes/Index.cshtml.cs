using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Admin.ProductAttributes
{
    public class IndexModel : PageModel
    {
        private readonly ProductAttributeApiService _apiService;

        public IndexModel(ProductAttributeApiService apiService)
        {
            _apiService = apiService;
        }

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public PagedResultModel<ProductAttributeDto> PagedAttributes { get; set; } = new();
        public bool CanManage { get; set; }

        public async Task OnGetAsync()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            CanManage = roleCode is "SYSTEM_ADMIN" or "WAREHOUSE_MANAGER";

            if (PageIndex < 1) PageIndex = 1;
            if (PageSize < 1) PageSize = 10;

            PagedAttributes = await _apiService.GetPagedListAsync(Keyword, Status, PageIndex, PageSize);
        }
    }
}
