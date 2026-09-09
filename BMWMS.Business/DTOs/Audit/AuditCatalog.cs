namespace BMWMS.Business.DTOs.Audit;

public static class AuditModules
{
    public const string Account = "ACCOUNT";
    public const string Product = "PRODUCT";
    public const string Partner = "PARTNER";
    public const string Order = "ORDER";
    public const string Inbound = "INBOUND";
    public const string Outbound = "OUTBOUND";
    public const string Inventory = "INVENTORY";
    public const string Transfer = "TRANSFER";
    public const string Stocktake = "STOCKTAKE";
    public const string System = "SYSTEM";
}

public static class AuditActions
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
    public const string Confirm = "CONFIRM";
    public const string Cancel = "CANCEL";
    public const string Assign = "ASSIGN";
    public const string ChangeStatus = "CHANGE_STATUS";
    public const string Approve = "APPROVE";
    public const string Reject = "REJECT";
    public const string Receive = "RECEIVE";
    public const string Putaway = "PUTAWAY";
    public const string Pick = "PICK";
    public const string Issue = "ISSUE";
    public const string Export = "EXPORT";
}

public static class AuditEntities
{
    public const string User = "User";
    public const string Role = "Role";
    public const string Product = "Product";
    public const string ProductGroup = "ProductGroup";
    public const string Category = "Category";
    public const string Supplier = "Supplier";
    public const string Customer = "Customer";
    public const string PurchaseOrder = "PurchaseOrder";
    public const string SalesOrder = "SalesOrder";
    public const string InboundOrder = "InboundOrder";
    public const string OutboundOrder = "OutboundOrder";
    public const string TransferOrder = "TransferOrder";
    public const string StocktakeSession = "StocktakeSession";
    public const string Inventory = "Inventory";
    public const string StorageLocation = "StorageLocation";
    public const string WarehouseZone = "WarehouseZone";
    public const string StorageRack = "StorageRack";
}

public static class AuditCatalog
{
    private static readonly IReadOnlyDictionary<string, string[]> ModuleEntities =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [AuditModules.Account] = ["User", "Role", "UserSession", "PasswordResetToken", "UserPasswordHistory"],
            [AuditModules.Product] = ["Product", "ProductGroup", "Category"],
            [AuditModules.Partner] = ["Supplier", "Customer"],
            [AuditModules.Order] = ["PurchaseOrder", "PurchaseOrderDetail", "SalesOrder", "SalesOrderDetail"],
            [AuditModules.Inbound] = ["InboundOrder", "InboundOrderDetail", "InboundReceipt", "InboundPutaway"],
            [AuditModules.Outbound] = ["OutboundOrder", "OutboundOrderDetail", "OutboundPickPlan", "OutboundPick"],
            [AuditModules.Inventory] = ["Inventory", "InventoryTransaction", "InventoryReservation", "ProductLot", "ReceiptStockLayer", "Warehouse", "WarehouseZone", "StorageRack", "StorageLocation"],
            [AuditModules.Transfer] = ["TransferOrder", "TransferOrderDetail"],
            [AuditModules.Stocktake] = ["StocktakeSchedule", "StocktakeSession", "StocktakeLocation", "StocktakeItem"],
            [AuditModules.System] = ["Notification", "Report", "System"]
        };

    private static readonly IReadOnlyDictionary<string, string> ModuleNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AuditModules.Account] = "Tài khoản và phân quyền",
            [AuditModules.Product] = "Sản phẩm và danh mục",
            [AuditModules.Partner] = "Đối tác",
            [AuditModules.Order] = "Đơn hàng PO/SO",
            [AuditModules.Inbound] = "Nhập kho",
            [AuditModules.Outbound] = "Xuất kho",
            [AuditModules.Inventory] = "Tồn kho và vị trí",
            [AuditModules.Transfer] = "Điều chuyển nội bộ",
            [AuditModules.Stocktake] = "Kiểm kê",
            [AuditModules.System] = "Hệ thống"
        };

    public static IReadOnlyList<AuditOptionDto> GetModules() => ModuleNames
        .Select(x => new AuditOptionDto { Value = x.Key, Label = x.Value })
        .ToList();

    public static IReadOnlyCollection<string>? GetEntitiesForModule(string? moduleCode)
    {
        if (string.IsNullOrWhiteSpace(moduleCode)) return null;
        return ModuleEntities.TryGetValue(moduleCode, out var entities) ? entities : Array.Empty<string>();
    }

    public static string ResolveModuleCode(string entityName)
    {
        foreach (var pair in ModuleEntities)
        {
            if (pair.Value.Contains(entityName, StringComparer.OrdinalIgnoreCase)) return pair.Key;
        }

        return AuditModules.System;
    }

    public static string GetModuleName(string moduleCode) =>
        ModuleNames.TryGetValue(moduleCode, out var name) ? name : "Hệ thống";

    public static string GetActionName(string actionType)
    {
        var action = actionType.Trim().ToUpperInvariant();
        if (action.Contains("LOGIN_SUCCESS")) return "Đăng nhập thành công";
        if (action.Contains("LOGIN_FAILED")) return "Đăng nhập thất bại";
        if (action.Contains("LOGIN_DENIED")) return "Từ chối đăng nhập";
        if (action.Contains("LOGIN_LOCKED")) return "Tài khoản bị khóa do đăng nhập sai";
        if (action.Contains("LOGOUT")) return "Đăng xuất";
        if (action.Contains("PASSWORD")) return "Thay đổi mật khẩu";
        if (action.Contains("ASSIGN")) return "Phân công / gán";
        if (action.Contains("UNLOCK")) return "Mở khóa";
        if (action.Contains("LOCK")) return "Khóa";
        if (action.Contains("CANCEL")) return "Hủy";
        if (action.Contains("CONFIRM")) return "Xác nhận";
        if (action.Contains("APPROVE")) return "Phê duyệt";
        if (action.Contains("REJECT")) return "Từ chối";
        if (action.Contains("DELETE")) return "Xóa";
        if (action.Contains("CREATE") || action.Contains("ADD")) return "Tạo mới";
        if (action.Contains("UPDATE") || action.Contains("EDIT")) return "Cập nhật";
        if (action.Contains("RECEIVE")) return "Kiểm nhận";
        if (action.Contains("PUTAWAY")) return "Cất hàng";
        if (action.Contains("PICK")) return "Lấy hàng";
        if (action.Contains("ISSUE")) return "Xuất hàng";
        if (action.Contains("EXPORT")) return "Xuất dữ liệu";
        return actionType.Replace('_', ' ');
    }

    public static string GetEntityName(string entityName) => entityName switch
    {
        "User" => "Người dùng",
        "Role" => "Vai trò",
        "UserSession" => "Phiên đăng nhập",
        "Product" => "Sản phẩm",
        "ProductGroup" => "Nhóm sản phẩm",
        "Category" => "Danh mục",
        "Supplier" => "Nhà cung cấp",
        "Customer" => "Khách hàng",
        "PurchaseOrder" => "Phiếu mua hàng",
        "SalesOrder" => "Phiếu bán hàng",
        "InboundOrder" => "Phiếu nhập",
        "OutboundOrder" => "Phiếu xuất",
        "TransferOrder" => "Phiếu điều chuyển",
        "StocktakeSession" => "Phiên kiểm kê",
        "Inventory" => "Tồn kho",
        "StorageLocation" => "Vị trí kho",
        "WarehouseZone" => "Khu vực kho",
        "StorageRack" => "Kệ kho",
        _ => entityName
    };
}
