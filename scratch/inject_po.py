import sys

file_path = 'BMWMS.Business/Services/Inventory/PurchaseOrderService.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

method = '''
        public async Task<(bool Success, string Message)> CreatePurchaseOrderAsync(PurchaseOrderCreateDto request, long userId)
        {
            // 1. Validate
            if (request.ExpectedDeliveryDate.HasValue && request.ExpectedDeliveryDate.Value < request.OrderDate)
            {
                return (false, "Ngày giao dự kiến không được nhỏ hơn ngày đặt hàng.");
            }
            if (!request.OrderDetails.Any())
            {
                return (false, "Lệnh mua hàng phải có ít nhất một vật tư.");
            }

            var duplicateProducts = request.OrderDetails.GroupBy(x => x.ProductId).Where(g => g.Count() > 1).ToList();
            if (duplicateProducts.Any())
            {
                return (false, "Không được chọn trùng lặp vật tư trong cùng một lệnh mua hàng.");
            }

            // 2. Generate PO Number
            string poNumber = await _poRepository.GeneratePurchaseOrderNumberAsync();

            // 3. Map to Entity
            var po = new BMWMS.Repository.Entities.PurchaseOrder
            {
                PurchaseOrderNumber = poNumber,
                SupplierId = request.SupplierId,
                OrderDate = request.OrderDate,
                ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                Status = "DRAFT", // Hardcoded as per implementation plan
                Notes = request.Notes,
                CreatedByUserId = userId,
                CreatedAt = DateTime.Now,
                PurchaseOrderDetails = new List<BMWMS.Repository.Entities.PurchaseOrderDetail>()
            };

            foreach (var item in request.OrderDetails)
            {
                if (item.OrderedQuantity <= 0)
                {
                    return (false, "Số lượng đặt phải lớn hơn 0.");
                }

                po.PurchaseOrderDetails.Add(new BMWMS.Repository.Entities.PurchaseOrderDetail
                {
                    ProductId = item.ProductId,
                    OrderedQuantity = item.OrderedQuantity,
                    Notes = item.Notes
                });
            }

            // 4. Save
            try
            {
                await _poRepository.AddAsync(po);
                return (true, poNumber);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi hệ thống khi tạo Lệnh mua hàng: {ex.Message}");
            }
        }
'''

insert_pos = content.rfind('}')
insert_pos = content.rfind('}', 0, insert_pos)
new_content = content[:insert_pos] + method + content[insert_pos:]

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_content)
