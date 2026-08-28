using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;

namespace BMWMS.Business.Services;

public class AuditLogService : IAuditLogService
{
    private const int MaxSnapshotLength = 100_000;
    private static readonly Regex ActionPattern = new("^[A-Z][A-Z0-9_]{1,49}$", RegexOptions.Compiled);
    private static readonly TimeZoneInfo BusinessTimeZone = ResolveBusinessTimeZone();
    private static readonly string[] SensitiveTokens =
    [
        "password", "passwordhash", "token", "secret", "otp", "apikey", "api_key",
        "authorization", "cookie", "credential", "refreshtoken", "resetcode"
    ];

    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task RecordAsync(AuditEventDto auditEvent)
    {
        await _auditLogRepository.AddAsync(BuildAuditLog(auditEvent));
    }

    public async Task StageAsync(AuditEventDto auditEvent)
    {
        await _auditLogRepository.AddAsync(BuildAuditLog(auditEvent), saveChanges: false);
    }

    private static AuditLog BuildAuditLog(AuditEventDto auditEvent)
    {
        if (string.IsNullOrWhiteSpace(auditEvent.ActionType))
            throw new ArgumentException("ActionType của audit log là bắt buộc.");
        if (string.IsNullOrWhiteSpace(auditEvent.EntityName))
            throw new ArgumentException("EntityName của audit log là bắt buộc.");

        var actionType = auditEvent.ActionType.Trim().ToUpperInvariant();
        var entityName = auditEvent.EntityName.Trim();
        if (!ActionPattern.IsMatch(actionType))
            throw new ArgumentException("ActionType chỉ được dùng chữ in hoa, số và dấu gạch dưới; tối đa 50 ký tự.");
        if (entityName.Length > 100)
            throw new ArgumentException("EntityName không được vượt quá 100 ký tự.");
        if (auditEvent.EntityId?.Length > 100)
            throw new ArgumentException("EntityId không được vượt quá 100 ký tự.");

        var oldValuesJson = SerializeSafeSnapshot(auditEvent.OldValues);
        var newValuesJson = SerializeSafeSnapshot(auditEvent.NewValues);

        return new AuditLog
        {
            UserId = auditEvent.UserId,
            ActionType = actionType,
            EntityName = entityName,
            EntityId = string.IsNullOrWhiteSpace(auditEvent.EntityId) ? null : auditEvent.EntityId.Trim(),
            OldValuesJson = oldValuesJson,
            NewValuesJson = newValuesJson,
            IpAddress = NormalizeIpAddress(auditEvent.IpAddress),
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<AuditLogListResponseDto> GetAuditLogsAsync(AuditLogFilterDto filter)
    {
        NormalizeFilter(filter);
        IReadOnlyCollection<string>? moduleEntities = null;
        if (!string.IsNullOrWhiteSpace(filter.ModuleCode))
        {
            var existingEntities = await _auditLogRepository.GetEntityNamesAsync();
            moduleEntities = existingEntities
                .Where(x => AuditCatalog.ResolveModuleCode(x) == filter.ModuleCode)
                .ToArray();
        }
        var (fromUtc, toUtcExclusive) = ResolveUtcRange(filter.FromDate, filter.ToDate);

        var result = await _auditLogRepository.GetPagedAsync(
            filter.Keyword,
            moduleEntities,
            NormalizeOptional(filter.ActionType),
            NormalizeOptional(filter.EntityName),
            NormalizeOptional(filter.EntityId),
            filter.UserId,
            fromUtc,
            toUtcExclusive,
            filter.PageIndex,
            filter.PageSize);

        return new AuditLogListResponseDto
        {
            Data = new PagedResultDto<AuditLogListItemDto>
            {
                Items = result.Items.Select(MapListItem).ToList(),
                TotalCount = result.TotalCount,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize
            }
        };
    }

    public async Task<AuditLogDetailDto?> GetAuditLogAsync(long auditLogId)
    {
        var log = await _auditLogRepository.GetByIdAsync(auditLogId);
        if (log is null) return null;

        var oldValues = ParseSnapshot(log.OldValuesJson);
        var newValues = ParseSnapshot(log.NewValuesJson);
        var listItem = MapListItem(log);

        return new AuditLogDetailDto
        {
            AuditLogId = listItem.AuditLogId,
            UserId = listItem.UserId,
            ActorCode = listItem.ActorCode,
            ActorName = listItem.ActorName,
            ModuleCode = listItem.ModuleCode,
            ModuleName = listItem.ModuleName,
            ActionType = listItem.ActionType,
            ActionName = listItem.ActionName,
            EntityName = listItem.EntityName,
            EntityDisplayName = listItem.EntityDisplayName,
            EntityId = listItem.EntityId,
            IpAddress = listItem.IpAddress,
            CreatedAt = listItem.CreatedAt,
            ChangeCount = BuildChanges(oldValues, newValues).Count,
            OldValues = oldValues.Select(x => new AuditValueDto { Field = x.Key, Value = x.Value }).ToList(),
            NewValues = newValues.Select(x => new AuditValueDto { Field = x.Key, Value = x.Value }).ToList(),
            Changes = BuildChanges(oldValues, newValues)
        };
    }

    public async Task<AuditLogOptionsDto> GetOptionsAsync(string? entityName = null)
    {
        // Các repository dùng chung scoped DbContext; không chạy song song trên cùng context.
        var actions = await _auditLogRepository.GetActionTypesAsync(entityName);
        var entities = await _auditLogRepository.GetEntityNamesAsync();
        var actors = await _auditLogRepository.GetActorsAsync();

        return new AuditLogOptionsDto
        {
            Modules = AuditCatalog.GetModules().ToList(),
            Actions = actions
                .Select(x => new AuditOptionDto { Value = x, Label = AuditCatalog.GetActionName(x) })
                .OrderBy(x => x.Label)
                .ToList(),
            Entities = entities
                .Select(x => new AuditOptionDto { Value = x, Label = AuditCatalog.GetEntityName(x) })
                .OrderBy(x => x.Label)
                .ToList(),
            Actors = actors.Select(x => new AuditActorOptionDto
            {
                UserId = x.UserId,
                Code = $"USR-{x.UserId:D3}",
                Name = x.FullName,
                Username = x.Username
            }).ToList()
        };
    }

    private static AuditLogListItemDto MapListItem(AuditLog log)
    {
        var moduleCode = AuditCatalog.ResolveModuleCode(log.EntityName);
        var oldValues = ParseSnapshot(log.OldValuesJson);
        var newValues = ParseSnapshot(log.NewValuesJson);
        return new AuditLogListItemDto
        {
            AuditLogId = log.AuditLogId,
            UserId = log.UserId,
            ActorCode = log.UserId.HasValue ? $"USR-{log.UserId.Value:D3}" : "HỆ THỐNG",
            ActorName = log.User?.FullName ?? "Hệ thống",
            ModuleCode = moduleCode,
            ModuleName = AuditCatalog.GetModuleName(moduleCode),
            ActionType = log.ActionType,
            ActionName = AuditCatalog.GetActionName(log.ActionType),
            EntityName = log.EntityName,
            EntityDisplayName = AuditCatalog.GetEntityName(log.EntityName),
            EntityId = log.EntityId,
            IpAddress = log.IpAddress,
            CreatedAt = DateTime.SpecifyKind(log.CreatedAt, DateTimeKind.Utc),
            ChangeCount = BuildChanges(oldValues, newValues).Count
        };
    }

    private static void NormalizeFilter(AuditLogFilterDto filter)
    {
        filter.PageIndex = Math.Max(1, filter.PageIndex);
        filter.PageSize = Math.Clamp(filter.PageSize, 10, 100);
        filter.ModuleCode = NormalizeOptional(filter.ModuleCode)?.ToUpperInvariant();
        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate > filter.ToDate)
            throw new ArgumentException("Ngày bắt đầu không được sau ngày kết thúc.");
    }

    private static (DateTime? FromUtc, DateTime? ToUtcExclusive) ResolveUtcRange(DateOnly? from, DateOnly? to)
    {
        DateTime? fromUtc = from.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), BusinessTimeZone)
            : null;
        DateTime? toUtc = to.HasValue
            ? TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), BusinessTimeZone)
            : null;
        return (fromUtc, toUtc);
    }

    private static TimeZoneInfo ResolveBusinessTimeZone()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }

    private static string? SerializeSafeSnapshot(object? snapshot)
    {
        if (snapshot is null) return null;
        var node = JsonSerializer.SerializeToNode(snapshot);
        var safeNode = RedactSensitiveValues(node);
        var json = safeNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        if (json?.Length > MaxSnapshotLength)
            throw new ArgumentException("Dữ liệu audit vượt quá giới hạn 100.000 ký tự. Chỉ ghi các trường nghiệp vụ cần thiết.");
        return json;
    }

    private static JsonNode? RedactSensitiveValues(JsonNode? node, string? propertyName = null)
    {
        if (IsSensitive(propertyName)) return JsonValue.Create("[ĐÃ ẨN]");
        if (node is JsonObject obj)
        {
            var result = new JsonObject();
            foreach (var pair in obj)
                result[pair.Key] = RedactSensitiveValues(pair.Value, pair.Key);
            return result;
        }
        if (node is JsonArray array)
        {
            var result = new JsonArray();
            foreach (var item in array) result.Add(RedactSensitiveValues(item));
            return result;
        }
        return node?.DeepClone();
    }

    private static bool IsSensitive(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return false;
        var normalized = propertyName.Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        return SensitiveTokens.Any(token => normalized.Contains(token.Replace("_", string.Empty)));
    }

    private static SortedDictionary<string, string?> ParseSnapshot(string? json)
    {
        var result = new SortedDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var document = JsonDocument.Parse(json);
            FlattenElement(document.RootElement, string.Empty, result);
        }
        catch (JsonException)
        {
            result["Dữ liệu"] = "Không thể đọc snapshot cũ do JSON không hợp lệ.";
        }
        return result;
    }

    private static void FlattenElement(JsonElement element, string path, IDictionary<string, string?> result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var nextPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                if (IsSensitive(property.Name)) result[nextPath] = "[ĐÃ ẨN]";
                else FlattenElement(property.Value, nextPath, result);
            }
            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
                FlattenElement(item, $"{path}[{index++}]", result);
            if (index == 0) result[path] = "[]";
            return;
        }

        result[string.IsNullOrEmpty(path) ? "Giá trị" : path] = element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => "Có",
            JsonValueKind.False => "Không",
            _ => element.GetRawText()
        };
    }

    private static List<AuditChangeDto> BuildChanges(
        IReadOnlyDictionary<string, string?> oldValues,
        IReadOnlyDictionary<string, string?> newValues)
    {
        var fields = oldValues.Keys.Union(newValues.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x);
        var changes = new List<AuditChangeDto>();
        foreach (var field in fields)
        {
            oldValues.TryGetValue(field, out var oldValue);
            newValues.TryGetValue(field, out var newValue);
            if (string.Equals(oldValue, newValue, StringComparison.Ordinal)) continue;
            changes.Add(new AuditChangeDto
            {
                Field = field,
                OldValue = oldValue,
                NewValue = newValue,
                ChangeType = !oldValues.ContainsKey(field) ? "ADDED" : !newValues.ContainsKey(field) ? "REMOVED" : "CHANGED"
            });
        }
        return changes;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return null;
        var value = ipAddress.Trim();
        return value.Length <= 45 ? value : value[..45];
    }
}
