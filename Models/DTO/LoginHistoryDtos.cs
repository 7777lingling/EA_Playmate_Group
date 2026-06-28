namespace EAPlaymateGroup.Models.DTO;

public sealed class LoginHistoryDto
{
    public long Id { get; set; }
    public int LoginUserId { get; set; }
    public string? LoginUserDisplayName { get; set; }
    public string? LoginAccount { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public string? DeviceInfo { get; set; }
    public string? FailureReason { get; set; }
    public bool Succeeded { get; set; }
    public DateTime? LoggedOutAt { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
}
