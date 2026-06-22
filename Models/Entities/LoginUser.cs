namespace EAPlaymateGroup.Models.Entities;

public sealed class LoginUser : IOrganizationScoped
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? UserId { get; set; }
    public Guid Uuid { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string LoginAccount { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DiscordId { get; set; }
    public string? DiscordName { get; set; }
    public string SystemRole { get; set; } = "staff";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
