namespace EAPlaymateGroup.Models.DTO;

public sealed class GiftRecordDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public DateOnly GiftDate { get; set; }
    public int BossUserId { get; set; }
    public string BossNickname { get; set; } = string.Empty;
    public int RecipientUserId { get; set; }
    public string RecipientNickname { get; set; } = string.Empty;
    public int? ServiceItemId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; }
    public string CustomerPaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateGiftRecordRequestDto
{
    public DateOnly GiftDate { get; set; }
    public int BossUserId { get; set; }
    public int RecipientUserId { get; set; }
    public int? ServiceItemId { get; set; }
    public string? GiftName { get; set; }
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string CustomerPaymentStatus { get; set; } = "unpaid";
    public string Status { get; set; } = "completed";
    public string? Remark { get; set; }
}

public sealed class UpdateGiftRecordRequestDto
{
    public DateOnly GiftDate { get; set; }
    public int BossUserId { get; set; }
    public int RecipientUserId { get; set; }
    public int? ServiceItemId { get; set; }
    public string? GiftName { get; set; }
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string CustomerPaymentStatus { get; set; } = "unpaid";
    public string Status { get; set; } = "completed";
    public string? Remark { get; set; }
}
