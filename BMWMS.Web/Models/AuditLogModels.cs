namespace BMWMS.Web.Models;

public class AuditLogFilterModel
{
    public string? Keyword { get; set; }
    public string? ModuleCode { get; set; }
    public string? ActionType { get; set; }
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public long? UserId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AuditLogListResponseModel
{
    public PagedResult<AuditLogListItemModel> Data { get; set; } = new();
}

public class AuditLogListItemModel
{
    public long AuditLogId { get; set; }
    public long? UserId { get; set; }
    public string ActorCode { get; set; } = null!;
    public string ActorName { get; set; } = null!;
    public string ModuleCode { get; set; } = null!;
    public string ModuleName { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string ActionName { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public string EntityDisplayName { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ChangeCount { get; set; }
}

public class AuditLogDetailModel : AuditLogListItemModel
{
    public List<AuditValueModel> OldValues { get; set; } = new();
    public List<AuditValueModel> NewValues { get; set; } = new();
    public List<AuditChangeModel> Changes { get; set; } = new();
}

public class AuditValueModel
{
    public string Field { get; set; } = null!;
    public string? Value { get; set; }
}

public class AuditChangeModel
{
    public string Field { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangeType { get; set; } = null!;
}

public class AuditLogOptionsModel
{
    public List<AuditOptionModel> Modules { get; set; } = new();
    public List<AuditOptionModel> Actions { get; set; } = new();
    public List<AuditOptionModel> Entities { get; set; } = new();
    public List<AuditActorOptionModel> Actors { get; set; } = new();
}

public class AuditOptionModel
{
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
}

public class AuditActorOptionModel
{
    public long UserId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Username { get; set; } = null!;
}
