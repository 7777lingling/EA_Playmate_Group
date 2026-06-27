namespace EAPlaymateGroup.Models.Entities;

public sealed class MoneyLog : IOrganizationScoped
{
    public long Id { get; set; }
    public int OrganizationId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? LoginUserId { get; set; }
    public LoginUser? LoginUser { get; set; }
    public long? AuditLogId { get; set; }
    public AuditLog? AuditLog { get; set; }
    public long? ReversedMoneyLogId { get; set; }
    public MoneyLog? ReversedMoneyLog { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? SourceType { get; set; }
    public int? SourceId { get; set; }
    public Guid? SourceUuid { get; set; }
    public string? Note { get; set; }
    public bool IsReversal { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}
