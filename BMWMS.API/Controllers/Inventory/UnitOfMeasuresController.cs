using BMWMS.Business.DTOs.Product;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.API.Controllers.Inventory
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UnitOfMeasuresController : ControllerBase
    {
        private readonly IUnitOfMeasureService _service;

        public UnitOfMeasuresController(IUnitOfMeasureService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<UnitOfMeasureDto>>> GetAll([FromQuery] string? keyword)
        {
            var result = await _service.GetAllAsync(keyword);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UnitOfMeasureDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateUnitOfMeasureDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return Ok(new { id, message = "Thêm ÐVT thành công" });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateUnitOfMeasureDto dto)
        {
            await _service.UpdateAsync(id, dto);
            return Ok(new { message = "Cập nhật ÐVT thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "Xóa ÐVT thành công" });
        }
    }
}
