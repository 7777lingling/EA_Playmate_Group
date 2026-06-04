namespace EAPlaymateGroup.Models.DTO;

public sealed class OrderDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string? OrderNo { get; set; }
    public DateOnly OrderDate { get; set; }
    public int? OwnerUserId { get; set; }
    public string? OwnerNickname { get; set; }
    public decimal Amount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal ShareTotalAmount { get; set; }
    public string Status { get; set; } = "completed";
    public string CustomerPaymentStatus { get; set; } = "unpaid";
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderMemberDto> Members { get; set; } = [];
}

public sealed class CreateOrderRequestDto
{
    public string? OrderNo { get; set; }
    public DateOnly OrderDate { get; set; }
    public int? OwnerUserId { get; set; }
    public decimal Amount { get; set; }
    public decimal CommissionRate { get; set; } = 0.1000m;
    public decimal? CommissionAmount { get; set; }
    public string Status { get; set; } = "completed";
    public string CustomerPaymentStatus { get; set; } = "unpaid";
    public string? Remark { get; set; }
    public List<CreateOrderMemberRequestDto> Members { get; set; } = [];
}

public sealed class UpdateOrderRequestDto
{
    public string? OrderNo { get; set; }
    public DateOnly OrderDate { get; set; }
    public int? OwnerUserId { get; set; }
    public decimal Amount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Status { get; set; } = "completed";
    public string CustomerPaymentStatus { get; set; } = "unpaid";
    public string? Remark { get; set; }
    public List<CreateOrderMemberRequestDto> Members { get; set; } = [];
}

public sealed class OrderListItemDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string? OrderNo { get; set; }
    public DateOnly OrderDate { get; set; }
    public string? OwnerNickname { get; set; }
    public decimal Amount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal ShareTotalAmount { get; set; }
    public int MemberCount { get; set; }
    public string Status { get; set; } = "completed";
    public string CustomerPaymentStatus { get; set; } = "unpaid";
}
