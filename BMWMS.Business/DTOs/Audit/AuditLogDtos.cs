using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Audit;

public class AuditLogFilterDto
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

public class AuditLogListResponseDto
{
    public PagedResultDto<AuditLogListItemDto> Data { get; set; } = new();
}

public class AuditLogListItemDto
{
    public long AuditLogId { get; set; }
    public long? UserId { get; set; }
    public string ActorCode { get; set; } = "HỆ THỐNG";
    public string ActorName { get; set; } = "Hệ thống";
    public string ModuleCode { get; set; } = AuditModules.System;
    public string ModuleName { get; set; } = "Hệ thống";
    public string ActionType { get; set; } = null!;
    public string ActionName { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public string EntityDisplayName { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ChangeCount { get; set; }
}

public class AuditLogDetailDto : AuditLogListItemDto
{
    public List<AuditValueDto> OldValues { get; set; } = new();
    public List<AuditValueDto> NewValues { get; set; } = new();
    public List<AuditChangeDto> Changes { get; set; } = new();
}

public class AuditValueDto
{
    public string Field { get; set; } = null!;
    public string? Value { get; set; }
}

public class AuditChangeDto
{
    public string Field { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangeType { get; set; } = null!;
}

public class AuditLogOptionsDto
{
    public List<AuditOptionDto> Modules { get; set; } = new();
    public List<AuditOptionDto> Actions { get; set; } = new();
    public List<AuditOptionDto> Entities { get; set; } = new();
    public List<AuditActorOptionDto> Actors { get; set; } = new();
}

public class AuditOptionDto
{
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
}

public class AuditActorOptionDto
{
    public long UserId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Username { get; set; } = null!;
}

/// <summary>
/// Contract duy nhất để các module ghi audit. Chỉ truyền snapshot nghiệp vụ cần thiết;
/// AuditLogService sẽ che trường nhạy cảm trước khi serialize.
/// </summary>
public class AuditEventDto
{
    public long? UserId { get; set; }
    public string ActionType { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public string? EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
    public string? IpAddress { get; set; }
}

