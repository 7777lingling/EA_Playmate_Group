namespace EAPlaymateGroup.Models.DTO;

public sealed class LoginRequestDto
{
    public string LoginAccount { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthMeDto
{
    public bool AuthRequired { get; set; }
    public bool IsAuthenticated { get; set; }
    public UserDto? User { get; set; }
}
