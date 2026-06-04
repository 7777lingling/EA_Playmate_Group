namespace EAPlaymateGroup.Models.Entities;

public sealed class Payment
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

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
