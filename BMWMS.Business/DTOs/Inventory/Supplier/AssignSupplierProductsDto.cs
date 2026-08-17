using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Inventory
{
    public class AssignSupplierProductsDto
    {
        public List<long> ProductIds { get; set; } = new();
    }
}
