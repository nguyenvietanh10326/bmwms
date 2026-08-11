namespace BMWMS.Web.Models;

public class WarehouseModel
{
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = null!;
    public string WarehouseName { get; set; } = null!;
}

public class StorageLocationModel
{
    public long StorageLocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
}
