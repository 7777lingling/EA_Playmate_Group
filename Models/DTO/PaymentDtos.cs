namespace EAPlaymateGroup.Models.DTO;

public sealed class PaymentDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public int UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string PayMonth { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public string PaymentStatus { get; set; } = "pending";
    public string? SnapshotJson { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CreatePaymentRequestDto
{
    public int UserId { get; set; }
    public string PayMonth { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public string PaymentStatus { get; set; } = "pending";
    public string? SnapshotJson { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
}

public sealed class UpdatePaymentRequestDto
{
    public decimal ExpectedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public string PaymentStatus { get; set; } = "pending";
    public string? SnapshotJson { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
}

public sealed class GenerateMonthlyPaymentsRequestDto
{
    public string PayMonth { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; }
}

public sealed class MarkPaymentPaidRequestDto
{
    public decimal? ActualAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
}
