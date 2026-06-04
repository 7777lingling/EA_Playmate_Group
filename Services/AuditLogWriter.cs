using System.Text.Json;
using EAPlaymateGroup.Models.Entities;

namespace EAPlaymateGroup.Services;

public static class AuditLogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AuditLog Create(
        string action,
        string targetType,
        int? targetId = null,
        Guid? targetUuid = null,
        object? before = null,
        object? after = null,
        int? userId = null)
    {
        return new AuditLog
        {
            UserId = userId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            TargetUuid = targetUuid,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, JsonOptions)
        };
    }
}
