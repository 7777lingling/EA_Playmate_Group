namespace EAPlaymateGroup.Models.DTO;

public sealed class OrderMemberDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string Role { get; set; } = "player";
    public decimal ShareAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateOrderMemberRequestDto
{
    public int UserId { get; set; }
    public string Role { get; set; } = "player";
    public decimal ShareAmount { get; set; }
}

public sealed class UpdateOrderMemberRequestDto
{
    public string Role { get; set; } = "player";
    public decimal ShareAmount { get; set; }
}
