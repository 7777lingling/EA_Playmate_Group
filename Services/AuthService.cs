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

    public async Task<LoginUserDto?> LoginWithDiscordAsync(DiscordUserProfile profile)
    {
        var discordUserId = profile.Id.Trim();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return null;
        }

        var loginUser = await _db.LoginUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.DiscordUserId == discordUserId &&
                x.DiscordLinkedAt != null &&
                x.IsActive);
        if (loginUser is null)
        {
            return null;
        }

        var discordId = profile.Username.Trim();
        var discordName = string.IsNullOrWhiteSpace(profile.GlobalName)
            ? discordId
            : profile.GlobalName.Trim();
        if (loginUser.DiscordUserId != discordUserId ||
            loginUser.DiscordId != discordId ||
            loginUser.DiscordName != discordName)
        {
            loginUser.DiscordUserId = discordUserId;
            loginUser.DiscordId = discordId;
            loginUser.DiscordName = discordName;
            loginUser.UpdatedAt = DateTime.UtcNow;
        }

        if (loginUser.UserId.HasValue)
        {
            var member = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == loginUser.UserId.Value && x.IsActive);
            if (member is not null)
            {
                member.DiscordUserId = discordUserId;
                member.DiscordId = discordId;
                member.DiscordName = discordName;
                member.UpdatedAt = DateTime.UtcNow;
            }
        }

        loginUser.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await ToDtoWithPermissionsAsync(loginUser);
    }

    public async Task<string> LinkDiscordAsync(int loginUserId, DiscordUserProfile profile)
    {
        var discordUserId = profile.Id.Trim();
        var discordId = profile.Username.Trim();
        var discordName = string.IsNullOrWhiteSpace(profile.GlobalName)
            ? discordId
            : profile.GlobalName.Trim();
        if (string.IsNullOrWhiteSpace(discordUserId) || string.IsNullOrWhiteSpace(discordId))
        {
            return "failed";
        }

        var alreadyLinked = await _db.LoginUsers.IgnoreQueryFilters()
            .AnyAsync(x => x.DiscordUserId == discordUserId && x.Id != loginUserId);
        if (alreadyLinked)
        {
            return "conflict";
        }

        var loginUser = await _db.LoginUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == loginUserId && x.IsActive);
        if (loginUser is null)
        {
            return "failed";
        }

        Models.Entities.User? member = null;
        if (!loginUser.UserId.HasValue)
        {
            member = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.OrganizationId == loginUser.OrganizationId &&
                    x.IsActive &&
                    (x.DiscordUserId == discordUserId ||
                     x.DiscordId == discordId ||
                     x.LoginAccount == loginUser.LoginAccount));
            if (member is not null)
            {
                loginUser.UserId = member.Id;
            }
        }

        if (loginUser.UserId.HasValue)
        {
            var memberAlreadyLinked = await _db.Users.IgnoreQueryFilters()
                .AnyAsync(x => x.DiscordUserId == discordUserId && x.Id != loginUser.UserId.Value);
            if (memberAlreadyLinked)
            {
                return "conflict";
            }

            member ??= await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == loginUser.UserId.Value && x.IsActive);
        }

        if (member is null)
        {
            return "member_required";
        }

        loginUser.DiscordUserId = discordUserId;
        loginUser.DiscordId = discordId;
        loginUser.DiscordName = discordName;
        loginUser.UpdatedAt = DateTime.UtcNow;
        loginUser.DiscordLinkedAt = DateTime.UtcNow;
        if (member is not null)
        {
            member.DiscordUserId = loginUser.DiscordUserId;
            member.DiscordId = loginUser.DiscordId;
            member.DiscordName = loginUser.DiscordName;
            member.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return "success";
    }

    public async Task<bool> UnlinkDiscordAsync(int loginUserId)
    {
        var loginUser = await _db.LoginUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == loginUserId && x.IsActive);
        if (loginUser is null)
        {
            return false;
        }

        loginUser.DiscordUserId = null;
        loginUser.DiscordId = null;
        loginUser.DiscordName = null;
        loginUser.DiscordLinkedAt = null;
        loginUser.UpdatedAt = DateTime.UtcNow;
        if (loginUser.UserId.HasValue)
        {
            var member = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == loginUser.UserId.Value);
            if (member is not null)
            {
                member.DiscordUserId = null;
                member.DiscordId = null;
                member.DiscordName = null;
                member.UpdatedAt = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync();
        return true;
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
