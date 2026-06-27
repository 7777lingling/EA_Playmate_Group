namespace EAPlaymateGroup.Models.DTO;

public sealed class AuditLogDto
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string? UserNickname { get; set; }
    public int? LoginUserId { get; set; }
    public string? LoginUserDisplayName { get; set; }
    public string? LoginAccount { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public int? TargetId { get; set; }
    public Guid? TargetUuid { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? IpAddress { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}
