namespace EAPlaymateGroup.Models.Entities;

public sealed class Order
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }

    public string? OrderNo { get; set; }
    public DateOnly OrderDate { get; set; }

    public int? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    public decimal Amount { get; set; }
    public decimal CommissionRate { get; set; } = 0.1000m;
    public decimal CommissionAmount { get; set; }

    public string Status { get; set; } = "completed";
    public string CustomerPaymentStatus { get; set; } = "unpaid";

    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderMember> Members { get; set; } = [];
}
