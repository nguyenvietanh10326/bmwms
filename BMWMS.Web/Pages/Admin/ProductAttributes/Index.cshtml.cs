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

        public List<ProductAttributeDto> Attributes { get; set; } = new();

        public async Task OnGetAsync()
        {
            Attributes = await _apiService.GetAllAsync();
        }
    }
}
