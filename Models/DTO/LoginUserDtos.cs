namespace EAPlaymateGroup.Models.DTO;

public sealed class LoginUserDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string LoginAccount { get; set; } = string.Empty;
    public string SystemRole { get; set; } = "staff";
    public bool IsActive { get; set; }
    public bool HasPassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public sealed class CreateLoginUserRequestDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string LoginAccount { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SystemRole { get; set; } = "staff";
}

public sealed class UpdateLoginUserRequestDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string LoginAccount { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string SystemRole { get; set; } = "staff";
    public bool IsActive { get; set; } = true;
}
