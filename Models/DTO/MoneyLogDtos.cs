namespace EAPlaymateGroup.Models.DTO;

public sealed class MoneyLogDto
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string MemberNickname { get; set; } = string.Empty;
    public int? LoginUserId { get; set; }
    public string? OperatorDisplayName { get; set; }
    public string? OperatorLoginAccount { get; set; }
    public long? AuditLogId { get; set; }
    public long? ReversedMoneyLogId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Status { get; set; } = "completed";
    public string? SourceType { get; set; }
    public int? SourceId { get; set; }
    public Guid? SourceUuid { get; set; }
    public string? Note { get; set; }
    public bool IsReversal { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateMoneyLogRequestDto
{
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Source { get; set; }
    public string? Note { get; set; }
}

public sealed class ReverseMoneyLogRequestDto
{
    public string? Note { get; set; }
}
