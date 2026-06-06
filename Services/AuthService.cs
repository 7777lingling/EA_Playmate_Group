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
        return await _db.LoginUsers.AnyAsync(x => x.IsActive);
    }

    public async Task<LoginUserDto?> GetCurrentUserAsync(HttpContext httpContext)
    {
        var userId = httpContext.Session.GetInt32(SessionUserId);
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _db.LoginUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value && x.IsActive);
        return user is null ? null : LoginUserMapper.ToDto(user);
    }

    public async Task<LoginUserDto?> LoginAsync(LoginRequestDto request)
    {
        var loginAccount = request.LoginAccount.Trim();
        if (string.IsNullOrWhiteSpace(loginAccount) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = await _db.LoginUsers.FirstOrDefaultAsync(x => x.LoginAccount == loginAccount && x.IsActive);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return LoginUserMapper.ToDto(user);
    }
}
