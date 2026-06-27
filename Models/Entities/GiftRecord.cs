namespace EAPlaymateGroup.Models.Entities;

public sealed class GiftRecord : IOrganizationScoped
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();

    public DateOnly GiftDate { get; set; }
    public int BossUserId { get; set; }
    public int RecipientUserId { get; set; }
    public int? ServiceItemId { get; set; }

    public string GiftName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string CustomerPaymentStatus { get; set; } = "unpaid";
    public string Status { get; set; } = "completed";
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public long? CreatedAuditLogId { get; set; }
    public AuditLog? CreatedAuditLog { get; set; }
    public long? CancelledAuditLogId { get; set; }
    public AuditLog? CancelledAuditLog { get; set; }

    public User? BossUser { get; set; }
    public User? RecipientUser { get; set; }
    public ServiceItem? ServiceItem { get; set; }
}
