namespace BMWMS.Web.Models.Inventory
{
    public class InventoryFilterDto
    {
        public string? Keyword { get; set; }        
        public long? WarehouseId { get; set; }         
        public long? StorageLocationId { get; set; }   
        public string? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class InventoryListItemDto
    {
        public long InventoryId { get; set; }

        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;

        public string WarehouseName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string WarehouseAndBin => $"{WarehouseName} - {LocationCode}";

        public decimal OnHandQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }

        public string LotNumber { get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }
        public string LotAndExpiryDisplay => ExpiryDate.HasValue
            ? $"{LotNumber} - {ExpiryDate.Value:dd/MM/yyyy}"
            : $"{LotNumber} - —";

        public string Status { get; set; } = string.Empty;
        public string StatusCssClass { get; set; } = string.Empty; 
    }

    public class InventoryDashboardPageDto
    {
        public decimal TotalOnHand { get; set; }
        public decimal TotalAvailable { get; set; }
        public decimal TotalReserved { get; set; }
        public decimal TotalInTransit { get; set; }

        public List<InventoryListItemDto> Items { get; set; } = new();

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
