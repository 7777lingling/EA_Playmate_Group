namespace EAPlaymateGroup.Models.Entities;

public sealed class AuditLog : IOrganizationScoped
{
    public long Id { get; set; }
    public int OrganizationId { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public int? LoginUserId { get; set; }
    public LoginUser? LoginUser { get; set; }

    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public int? TargetId { get; set; }
    public Guid? TargetUuid { get; set; }

    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public string? DeviceInfo { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? BatchUuid { get; set; }

    public DateTime CreatedAt { get; set; }
}
