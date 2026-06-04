namespace EAPlaymateGroup.Models.Entities;

public sealed class OrderMember
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Role { get; set; } = "player";
    public decimal ShareAmount { get; set; }

    public DateTime CreatedAt { get; set; }
}
