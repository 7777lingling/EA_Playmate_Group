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

    public LoginUserService(EAPlaymateGroupDbContext db, PasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult<LoginUserDto>> CreateAsync(CreateLoginUserRequestDto request)
    {
        var validationResult = await ValidateAsync(
            request.DisplayName,
            request.LoginAccount,
            request.Password,
            request.SystemRole,
            requirePassword: true);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<LoginUserDto>(validationResult);
        }

        var loginUser = new LoginUser
        {
            DisplayName = request.DisplayName.Trim(),
            LoginAccount = request.LoginAccount.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            SystemRole = request.SystemRole,
            IsActive = true
        };

        _db.LoginUsers.Add(loginUser);
        await _db.SaveChangesAsync();

        return ServiceResult<LoginUserDto>.Success(LoginUserMapper.ToDto(loginUser));
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
            requirePassword: false,
            excludeLoginUserId: id);
        if (!validationResult.Succeeded)
        {
            return ToGenericResult<LoginUserDto>(validationResult);
        }

        loginUser.DisplayName = request.DisplayName.Trim();
        loginUser.LoginAccount = request.LoginAccount.Trim();
        loginUser.SystemRole = request.SystemRole;
        loginUser.IsActive = request.IsActive;
        loginUser.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            loginUser.PasswordHash = _passwordHasher.Hash(request.Password);
        }

        await _db.SaveChangesAsync();

        return ServiceResult<LoginUserDto>.Success(LoginUserMapper.ToDto(loginUser));
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive)
    {
        var loginUser = await _db.LoginUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (loginUser is null)
        {
            return ServiceResult.Missing();
        }

        loginUser.IsActive = isActive;
        loginUser.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    private async Task<ServiceResult> ValidateAsync(
        string displayName,
        string loginAccount,
        string? password,
        string systemRole,
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

        if (!string.IsNullOrWhiteSpace(loginAccount))
        {
            var normalizedLoginAccount = loginAccount.Trim();
            var exists = await _db.LoginUsers.AnyAsync(x =>
                x.LoginAccount == normalizedLoginAccount &&
                (!excludeLoginUserId.HasValue || x.Id != excludeLoginUserId.Value));
            if (exists)
            {
                errors["loginAccount"] = ["此登入帳號已存在，請換一個。"];
            }
        }

        return errors.Count > 0 ? ServiceResult.Validation(errors) : ServiceResult.Success();
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
