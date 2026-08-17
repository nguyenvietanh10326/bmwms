import sys

file_path = 'BMWMS.API/Controllers/Inventory/PurchaseOrdersController.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

method = '''
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BMWMS.Business.DTOs.Inventory.PurchaseOrderCreateDto request)
        {
            try
            {
                // In a real app, we would get the UserId from claims.
                // For now, we hardcode 1 or get from headers if provided.
                long userId = 1;
                if (Request.Headers.TryGetValue("X-User-Id", out var userIdStr) && long.TryParse(userIdStr, out var uid))
                {
                    userId = uid;
                }

                var result = await _poService.CreatePurchaseOrderAsync(request, userId);
                if (result.Success)
                {
                    return Ok(new { message = "Tạo lệnh mua hàng thành công.", data = result.Message });
                }
                else
                {
                    return BadRequest(new { message = result.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo Lệnh mua hàng.", detail = ex.Message });
            }
        }
'''

insert_pos = content.rfind('}')
insert_pos = content.rfind('}', 0, insert_pos)
new_content = content[:insert_pos] + method + content[insert_pos:]

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_content)
