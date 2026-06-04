namespace EAPlaymateGroup.Models.Entities;

public sealed class User
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }

    public string Nickname { get; set; } = string.Empty;
    public string? DiscordId { get; set; }
    public string? DiscordName { get; set; }
    public string? BankAccount { get; set; }

    public string SystemRole { get; set; } = "staff";
    public bool IsPlayer { get; set; } = true;
    public bool IsBoss { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LeftAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Order> OwnedOrders { get; set; } = [];
    public ICollection<OrderMember> OrderMembers { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
