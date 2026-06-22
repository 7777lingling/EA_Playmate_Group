namespace EAPlaymateGroup.Models.DTO;

public sealed class LoginUserDto
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? UserId { get; set; }
    public Guid Uuid { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string LoginAccount { get; set; } = string.Empty;
    public string? DiscordId { get; set; }
    public string? DiscordName { get; set; }
    public string? DiscordUserId { get; set; }
    public DateTime? DiscordLinkedAt { get; set; }
    public string SystemRole { get; set; } = "staff";
    public bool IsActive { get; set; }
    public bool HasPassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Permissions { get; set; } = [];
}

public sealed class CreateLoginUserRequestDto
{
    public int? OrganizationId { get; set; }
    public int? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string LoginAccount { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string SystemRole { get; set; } = "staff";
}

public sealed class UpdateLoginUserRequestDto
{
    public int? OrganizationId { get; set; }
    public int? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string LoginAccount { get; set; } = string.Empty;
    public string SystemRole { get; set; } = "staff";
    public bool IsActive { get; set; } = true;
}

public sealed class ChangeMyPasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
