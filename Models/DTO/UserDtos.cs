namespace EAPlaymateGroup.Models.DTO;

public sealed class UserDto
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Guid Uuid { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string? DiscordId { get; set; }
    public string? DiscordName { get; set; }
    public string? DiscordUserId { get; set; }
    public string? BankAccount { get; set; }
    public bool IsPlayer { get; set; }
    public bool IsBoss { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LeftAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CreateUserRequestDto
{
    public string Nickname { get; set; } = string.Empty;
    public string? BankAccount { get; set; }
    public bool IsPlayer { get; set; } = true;
    public bool IsBoss { get; set; }
}

public sealed class UpdateUserRequestDto
{
    public string Nickname { get; set; } = string.Empty;
    public string? BankAccount { get; set; }
    public bool IsPlayer { get; set; }
    public bool IsBoss { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LeftAt { get; set; }
}

public sealed class LeaveUserRequestDto
{
    public DateTime? LeftAt { get; set; }
}
