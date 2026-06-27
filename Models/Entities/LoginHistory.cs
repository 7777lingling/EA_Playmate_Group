namespace EAPlaymateGroup.Models.Entities;

public sealed class LoginHistory : IOrganizationScoped
{
    public long Id { get; set; }
    public int OrganizationId { get; set; }
    public int LoginUserId { get; set; }
    public LoginUser LoginUser { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public bool Succeeded { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
