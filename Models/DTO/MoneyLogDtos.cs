namespace EAPlaymateGroup.Models.DTO;

public sealed class MoneyLogDto
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string MemberNickname { get; set; } = string.Empty;
    public long? AuditLogId { get; set; }
    public long? ReversedMoneyLogId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? SourceType { get; set; }
    public int? SourceId { get; set; }
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
