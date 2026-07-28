using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BMWMS.Business.Interfaces;

namespace BMWMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/categories")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }
    }
}
