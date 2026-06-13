using EAPlaymateGroup.Common;
using EAPlaymateGroup.Data;
using EAPlaymateGroup.Models.DTO;
using EAPlaymateGroup.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EAPlaymateGroup.Services;

public sealed class LoginUserService
{
    private readonly EAPlaymateGroupDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginUserService(
        EAPlaymateGroupDbContext db,
        PasswordHasher passwordHasher,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResult<LoginUserDto>> CreateAsync(CreateLoginUserRequestDto request)
    {
        var validationResult = await ValidateAsync(
            request.DisplayName,
            request.LoginAccount,
            request.Password,
            request.SystemRole,
            request.OrganizationId,
            request.UserId,
            requirePassword: true);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<LoginUserDto>(validationResult);
        }

        var loginUser = new LoginUser
        {
            OrganizationId = ResolveOrganizationId(request.OrganizationId),
            UserId = request.UserId,
            DisplayName = request.DisplayName.Trim(),
            LoginAccount = request.LoginAccount.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            SystemRole = request.SystemRole,
            IsActive = true
        };

        _db.LoginUsers.Add(loginUser);
        await _db.SaveChangesAsync();

        var dto = LoginUserMapper.ToDto(loginUser);
        _db.AuditLogs.Add(AuditLogWriter.Create("create", "login_users", loginUser.Id, loginUser.Uuid, after: dto));
        await _db.SaveChangesAsync();

        return ServiceResult<LoginUserDto>.Success(dto);
    }

    public async Task<ServiceResult<LoginUserDto>> UpdateAsync(int id, UpdateLoginUserRequestDto request)
    {
        var loginUser = await _db.LoginUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (loginUser is null)
        {
            return ServiceResult<LoginUserDto>.Missing();
        }

        var validationResult = await ValidateAsync(
            request.DisplayName,
            request.LoginAccount,
            request.Password,
            request.SystemRole,
            request.OrganizationId,
            request.UserId,
            requirePassword: false,
            excludeLoginUserId: id);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<LoginUserDto>(validationResult);
        }

        var before = LoginUserMapper.ToDto(loginUser);

        loginUser.DisplayName = request.DisplayName.Trim();
        loginUser.OrganizationId = ResolveOrganizationId(request.OrganizationId);
        loginUser.UserId = request.UserId;
        loginUser.LoginAccount = request.LoginAccount.Trim();
        loginUser.SystemRole = request.SystemRole;
        loginUser.IsActive = request.IsActive;
        loginUser.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            loginUser.PasswordHash = _passwordHasher.Hash(request.Password);
        }

        await _db.SaveChangesAsync();

        var dto = LoginUserMapper.ToDto(loginUser);
        _db.AuditLogs.Add(AuditLogWriter.Create("update", "login_users", loginUser.Id, loginUser.Uuid, before, dto));
        await _db.SaveChangesAsync();

        return ServiceResult<LoginUserDto>.Success(dto);
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive)
    {
        var loginUser = await _db.LoginUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (loginUser is null)
        {
            return ServiceResult.Missing();
        }

        var before = LoginUserMapper.ToDto(loginUser);
        loginUser.IsActive = isActive;
        loginUser.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(AuditLogWriter.Create(
            isActive ? "activate" : "deactivate",
            "login_users",
            loginUser.Id,
            loginUser.Uuid,
            before,
            LoginUserMapper.ToDto(loginUser)));
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    private async Task<ServiceResult> ValidateAsync(
        string displayName,
        string loginAccount,
        string? password,
        string systemRole,
        int? organizationId,
        int? userId,
        bool requirePassword,
        int? excludeLoginUserId = null)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errors["displayName"] = ["請輸入顯示名稱。"];
        }

        if (string.IsNullOrWhiteSpace(loginAccount))
        {
            errors["loginAccount"] = ["請輸入登入帳號。"];
        }

        if (requirePassword && string.IsNullOrWhiteSpace(password))
        {
            errors["password"] = ["請輸入登入密碼。"];
        }

        if (!DomainValues.IsSystemRole(systemRole))
        {
            errors["systemRole"] = ["系統權限必須是 admin、staff 或 viewer。"];
        }

        var resolvedOrganizationId = ResolveOrganizationId(organizationId);
        if (resolvedOrganizationId <= 0 ||
            !await _db.Organizations.AnyAsync(x => x.Id == resolvedOrganizationId && x.IsActive))
        {
            errors["organizationId"] = ["請選擇有效的組織。"];
        }

        if (systemRole == "viewer" && !userId.HasValue)
        {
            errors["userId"] = ["一般會員必須綁定成員資料。"];
        }
        else if (userId.HasValue && !await _db.Users.AnyAsync(x =>
                     x.Id == userId.Value &&
                     x.OrganizationId == resolvedOrganizationId &&
                     x.IsActive))
        {
            errors["userId"] = ["綁定的成員不屬於此組織或已停用。"];
        }

        if (!string.IsNullOrWhiteSpace(loginAccount))
        {
            var normalizedLoginAccount = loginAccount.Trim();
            var excludedLoginUserId = excludeLoginUserId ?? 0;
            var exists = await _db.LoginUsers.AnyAsync(x =>
                x.LoginAccount == normalizedLoginAccount &&
                (!excludeLoginUserId.HasValue || x.Id != excludedLoginUserId));
            if (exists)
            {
                errors["loginAccount"] = ["此登入帳號已存在，請換一個。"];
            }
        }

        return errors.Count > 0 ? ServiceResult.Validation(errors) : ServiceResult.Success();
    }

    private int ResolveOrganizationId(int? requestedOrganizationId)
    {
        var role = _httpContextAccessor.HttpContext?.Session.GetString(AuthService.SessionSystemRole);
        if (role == "admin" && requestedOrganizationId.HasValue)
        {
            return requestedOrganizationId.Value;
        }

        return _httpContextAccessor.HttpContext?.Session.GetInt32(AuthService.SessionOrganizationId) ?? 0;
    }

    private static ServiceResult<T> ToGenericResult<T>(ServiceResult result)
    {
        if (result.ValidationErrors is not null)
        {
            return ServiceResult<T>.Validation(result.ValidationErrors);
        }

        if (result.NotFound)
        {
            return ServiceResult<T>.Missing();
        }

        return ServiceResult<T>.Failure(
            result.ErrorCode ?? "operation_failed",
            result.ErrorMessage ?? "Operation failed.");
    }
}
