namespace EAPlaymateGroup.Models.Entities;

public sealed class Order : IOrganizationScoped
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Guid Uuid { get; set; }

    public string? OrderNo { get; set; }
    public string OrderType { get; set; } = "boosting";
    public string? PricingCategory { get; set; }
    public DateOnly OrderDate { get; set; }

    public int? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    public decimal Amount { get; set; }
    public decimal ServiceQuantity { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal DesignatedFee { get; set; }
    public decimal FriendFee { get; set; }
    public decimal ReplacementFee { get; set; }
    public decimal NightFee { get; set; }
    public decimal OtherFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public int? ActivityId { get; set; }
    public Activity? Activity { get; set; }
    public string? ActivityNameSnapshot { get; set; }
    public string? ActivityDiscountType { get; set; }
    public decimal? ActivityDiscountValue { get; set; }
    public bool ActivityIncludeFees { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }

    public string Status { get; set; } = "completed";
    public string CustomerPaymentStatus { get; set; } = "unpaid";

    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? CreatedAuditLogId { get; set; }
    public AuditLog? CreatedAuditLog { get; set; }
    public long? CancelledAuditLogId { get; set; }
    public AuditLog? CancelledAuditLog { get; set; }

    public ICollection<OrderMember> Members { get; set; } = [];
}
