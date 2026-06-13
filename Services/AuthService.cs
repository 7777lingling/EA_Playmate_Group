using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class AuthService
{
    public const string SessionUserId = "auth_user_id";
    public const string SessionOrganizationId = "auth_organization_id";
    public const string SessionSystemRole = "auth_system_role";
    public const string SessionMemberUserId = "auth_member_user_id";

    private readonly EAPlaymateGroupDbContext _db;
    private readonly PasswordHasher _passwordHasher;

    public AuthService(EAPlaymateGroupDbContext db, PasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> IsAuthRequiredAsync()
    {
        return await _db.LoginUsers.IgnoreQueryFilters().AnyAsync(x => x.IsActive);
    }

    public async Task<LoginUserDto?> GetCurrentUserAsync(HttpContext httpContext)
    {
        var userId = httpContext.Session.GetInt32(SessionUserId);
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _db.LoginUsers.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId.Value && x.IsActive);
        if (user is null)
        {
            return null;
        }

        httpContext.Session.SetInt32(SessionOrganizationId, user.OrganizationId);
        httpContext.Session.SetString(SessionSystemRole, user.SystemRole);
        if (user.UserId.HasValue)
        {
            httpContext.Session.SetInt32(SessionMemberUserId, user.UserId.Value);
        }
        else
        {
            httpContext.Session.Remove(SessionMemberUserId);
        }
        return await ToDtoWithPermissionsAsync(user);
    }

    public async Task<LoginUserDto?> LoginAsync(LoginRequestDto request)
    {
        var loginAccount = request.LoginAccount.Trim();
        if (string.IsNullOrWhiteSpace(loginAccount) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var user = await _db.LoginUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.LoginAccount == loginAccount && x.IsActive);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await ToDtoWithPermissionsAsync(user);
    }

    private async Task<LoginUserDto> ToDtoWithPermissionsAsync(Models.Entities.LoginUser user)
    {
        var dto = LoginUserMapper.ToDto(user);
        dto.Permissions = user.SystemRole == "admin"
            ? Common.PermissionCodes.All.ToList()
            : await _db.RolePermissions.AsNoTracking()
                .Where(x => x.SystemRole == user.SystemRole && x.IsAllowed)
                .Select(x => x.PermissionCode)
                .ToListAsync();
        return dto;
    }
}
