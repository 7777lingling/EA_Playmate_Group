using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class AuthService
{
    public const string SessionUserId = "auth_user_id";

    private readonly EAPlaymateGroupDbContext _db;
    private readonly PasswordHasher _passwordHasher;

    public AuthService(EAPlaymateGroupDbContext db, PasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> IsAuthRequiredAsync()
    {
        return await _db.Users.AnyAsync(x => x.IsActive && x.LoginAccount != null && x.PasswordHash != null);
    }

    public async Task<UserDto?> GetCurrentUserAsync(HttpContext httpContext)
    {
        var userId = httpContext.Session.GetInt32(SessionUserId);
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value && x.IsActive);
        return user is null ? null : UserMapper.ToDto(user);
    }

    public async Task<UserDto?> LoginAsync(LoginRequestDto request)
    {
        var loginAccount = request.LoginAccount.Trim();
        if (string.IsNullOrWhiteSpace(loginAccount) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.LoginAccount == loginAccount && x.IsActive);
        if (user?.PasswordHash is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return UserMapper.ToDto(user);
    }
}
